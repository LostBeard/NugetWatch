using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using Timer = System.Timers.Timer;

namespace NugetWatch.Services
{
    public class NugetMonitorService(BlazorJSRuntime JS, NugetService NugetService) : IAsyncBackgroundService
    {
        public delegate void PackageChangeDelegate(List<NugetPackageChangeEventArg> packages);
        public event PackageChangeDelegate OnPackageChange = default!;
        public event Action UpdateBegin = default!;
        public event Action UpdateEnd = default!;
        Task? _Ready;
        /// <inheritdoc/>
        public Task Ready => _Ready ??= InitAsync();
        Timer _tmr = new Timer(10000);
        public bool Updating { get; set; }
        public Dictionary<string, Dictionary<string, NugetPackageData>> OwnerWatch { get; } = new Dictionary<string, Dictionary<string, NugetPackageData>>();
        public List<string> Owners => OwnerWatch.Keys.OrderBy(x => x).ToList();
        public Dictionary<string, NugetPackageData> OwnerPackages(string owner) => owner != null && OwnerWatch.TryGetValue(owner, out var p) ? p : new Dictionary<string, NugetPackageData>();
        public List<NugetPackageData> OwnerPackagesByTitle(string owner) => OwnerPackages(owner).Values.OrderBy(x => x.Title).ToList();
        public List<NugetPackageData> OwnerPackagesByTotalDownloads(string owner) => OwnerPackages(owner).Values.OrderByDescending(x => x.TotalDownloads).ToList();

        public long GetOwnerTotalDownloads(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) return 0;
            var packages = OwnerPackagesByTitle(owner);
            var ret = packages.Sum(x => x.TotalDownloads);
            return ret;
        }
        FileSystemDirectoryHandle? FS;

        async Task<IDBDatabase> GetDB()
        {
            var idb = await IDBDatabase.OpenAsync(keyStoreDBName, 1, (evt) =>
            {
                // upgrade needed
                using var request = evt.Target;
                using var db = request.Result;
                var stores = db.ObjectStoreNames;
                if (!stores.Contains(keyStoreName))
                {
                    using var myKeysStore = db.CreateObjectStore<string, NugetPackageData>(keyStoreName, new IDBObjectStoreCreateOptions { KeyPath = "dataKey" });
                    myKeysStore.CreateIndex<string>("authorIndex", "author");
                    myKeysStore.CreateIndex<string>("ownerIndex", "owner");
                    myKeysStore.CreateIndex<string>("titleIndex", "title");
                    myKeysStore.CreateIndex<long>("dataTimeStampLongIndex", "dataTimeStampLong");
                }
            });
            return idb;
        }

        string keyStoreDBName = "storageDB";
        string keyStoreName = "NugetPackageDataStorage";
        async Task InitAsync()
        {
            using var idb = await GetDB();
            using var navigator = JS.Get<Navigator>("navigator");
            FS = await navigator.Storage.GetDirectory();
#if DEBUG
            _tmr.Interval = 60 * 1 * 1000;
#else
            _tmr.Interval = 60 * 2 * 1000;
#endif
            _tmr.Elapsed += _tmr_Elapsed;
            _tmr.Enabled = true;
            // load owner(s) to watch and the latest package data
            if (await FS.FilePathExists("owners.json"))
            {
                try
                {
                    var owners = await FS.ReadJSON<List<string>>("owners.json");
                    if (owners != null)
                    {
                        foreach (var owner in owners)
                        {
                            var packages = await GetFromDBByOwner(owner);
                            var packagesD = packages.GroupBy(o => o.Title).ToDictionary(o => o.First().Title, o => o.OrderByDescending(o => o.DataTimeStampLong).First());
                            OwnerWatch[owner] = packagesD;
                        }
                    }
                }
                catch (Exception ex)
                {
                    var nmt = true;
                }
            }
            // don't auto-update if not running in a window
            if (JS.IsWindow)
            {
                // add defaults if none
                if (!OwnerWatch.Any())
                {
                    await AddOwnerWatch("LostBeard");
                }
                else
                {
                    _ = Update();
                }
            }
        }
        async Task SaveToDB(NugetPackageData data)
        {
            using var idb = await GetDB();
            // start the IndexedDB transaction in read and write mode
            using var tx = idb.Transaction(keyStoreName, true);
            // get the store
            using var objectStore = tx.ObjectStore<string, NugetPackageData>(keyStoreName);
            // put the data into the store
            await objectStore.PutAsync(data);
        }
        public Task<List<NugetPackageData>> GetFromDBByTitle(string title) => GetFromDBByIndex(title, "titleIndex");
        public Task<List<NugetPackageData>> GetFromDBByOwner(string owner) => GetFromDBByIndex(owner, "ownerIndex");
        public Task<List<NugetPackageData>> GetFromDBByAuthor(string author) => GetFromDBByIndex(author, "authorIndex");
        async Task<List<NugetPackageData>> GetFromDBByIndex(string query, string indexName)
        {
            using var idb = await GetDB();
            // start the IndexedDB transaction in read only mode
            using var tx = idb.Transaction(keyStoreName);
            // get the key store
            using var objectStore = tx.ObjectStore<string, NugetPackageData>(keyStoreName);
            // get the previously created index 
            using var index = objectStore.Index<string>(indexName);
            var results = (await index.GetAllAsync(query)).Using(o => o.ToList());
            // query
            return results;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="min">specifies the lower bound of the new key range.</param>
        /// <param name="max">specifies the upper bound of the new key range.</param>
        /// <param name="lowerOpen">indicates whether the lower bound excludes the endpoint value. The default is false.</param>
        /// <param name="upperOpen">Indicates whether the upper bound excludes the endpoint value. The default is false.</param>
        /// <returns></returns>
        public async Task<List<NugetPackageData>> GetFromDBByDataTimeStamp(DateTimeOffset min, DateTimeOffset max, bool lowerOpen = false, bool upperOpen = false)
        {
            using var idbKeyRange = IDBKeyRange<long>.Bound(min.ToUnixTimeMilliseconds(), max.ToUnixTimeMilliseconds(), lowerOpen, upperOpen);
            using var idb = await GetDB();
            // start the IndexedDB transaction in read only mode
            using var tx = idb.Transaction(keyStoreName);
            // get the key store
            using var objectStore = tx.ObjectStore<string, NugetPackageData>(keyStoreName);
            // get the previously created index 
            using var index = objectStore.Index<long>("dataTimeStampLongIndex");
            // query
            var results = (await index.GetAllAsync(idbKeyRange)).Using(o => o.ToList());
            return results;
        }
        //public async Task<List<NugetPackageData>> GetFromDBByDataTimeStamp(DateTimeOffset min, DateTimeOffset max, bool lowerOpen = false, bool upperOpen = false)
        //{
        //    using var idbKeyRange = IDBKeyRange<long>.Bound(min.ToUnixTimeMilliseconds(), max.ToUnixTimeMilliseconds(), lowerOpen, upperOpen);
        //    using var idb = await GetDB();
        //    // start the IndexedDB transaction in read only mode
        //    using var tx = idb.Transaction(keyStoreName);
        //    // get the key store
        //    using var objectStore = tx.ObjectStore<string, NugetPackageData>(keyStoreName);
        //    // get the previously created index 
        //    using var index = objectStore.Index<long>("dataTimeStampLongIndex");
        //    // query
        //    var results = (await index.GetAllAsync(idbKeyRange)).Using(o => o.ToList());
        //    return results;
        //}
        public async Task<NugetPackageData?> GetFromDBByDataTimeStamp(DateTimeOffset asOf, bool excludeEndpoint = false)
        {
            using var idb = await GetDB();
            using var idbKeyRange = IDBKeyRange<long>.UpperBound(asOf.ToUnixTimeMilliseconds(), excludeEndpoint);
            // start the IndexedDB transaction in read only mode
            using var tx = idb.Transaction(keyStoreName);
            // get the key store
            using var objectStore = tx.ObjectStore<string, NugetPackageData>(keyStoreName);
            // get the previously created index 
            using var index = objectStore.Index<long>("dataTimeStampLongIndex");
            // query
            var result = (await index.GetAllAsync(idbKeyRange)).Using(o => o.ToList().OrderByDescending(o => o.DataTimeStampLong).FirstOrDefault());
            return result;
        }
        public async Task AddOwnerWatch(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) return;
            if (OwnerWatch.ContainsKey(owner)) return;
            var packages = await GetFromDBByOwner(owner);
            var packagesD = packages.GroupBy(o => o.Title).ToDictionary(o => o.First().Title, o => o.OrderByDescending(o => o.DataTimeStampLong).First());
            OwnerWatch[owner] = packagesD;
            var owners = OwnerWatch.Keys.ToList();
            await FS!.WriteJSON("owners.json", owners);
            await Update();
        }
        public async Task RemoveOwnerWatch(string owner)
        {
            if (!OwnerWatch.ContainsKey(owner)) return;
            OwnerWatch.Remove(owner);
            var owners = OwnerWatch.Keys.ToList();
            await FS!.WriteJSON("owners.json", owners);
            await Update();
        }
        private void _tmr_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            _ = Update();
        }
        Task? _Updating;
        public Task Update()
        {
            _Updating ??= _Update();
            return _Updating;
        }
        public Dictionary<string, Dictionary<string, long>> DownloadsToday { get; private set; } = new Dictionary<string, Dictionary<string, long>>();

        public long GetDownloadsToday(NugetPackageData nugetPackage)
        {
            if (nugetPackage == null || nugetPackage.Owner == null || nugetPackage.Title == null) return 0;
            return GetDownloadsToday(nugetPackage.Owner, nugetPackage.Title);
        }
        public long GetDownloadsToday(string owner, string packageTitle)
        {
            if (DownloadsToday.TryGetValue(owner,  out var packageDownloadCounts))
            {
                if (packageDownloadCounts.TryGetValue(packageTitle, out var packageDownloadCount))
                {
                    return packageDownloadCount;
                }
            }
            return 0;
        }

        async Task UpdateDownloadsToday()
        {
            var now = DateTimeOffset.Now;
            var startOfDay = now.StartOfDay();
            var endOfDay = now.EndOfDay();
            var downloadsToday = new Dictionary<string, Dictionary<string, long>>();
            foreach (var ownerWatch in OwnerWatch)
            {
                var owner = ownerWatch.Key;
                var ownerPackages = ownerWatch.Value;
                downloadsToday[owner] = new Dictionary<string, long>();
                var ownerDownloadsToday = downloadsToday[owner];
                // owner's packages
                // iterate
                foreach (var package in ownerPackages.Values)
                {
                    // get the count at the start of the day, and compare to the count now
                    var startOfDayEntry = await GetFromDBByDataTimeStamp(startOfDay);
                    var startOfDayCount = startOfDayEntry?.TotalDownloads ?? 0;
                    var currentCount = package.TotalDownloads;
                    ownerDownloadsToday[package.Title] = currentCount - startOfDayCount;
                }
            }
            DownloadsToday = downloadsToday;
        }
        async Task _Update()
        {
            try
            {
                UpdateBegin?.Invoke();
                var changedPackages = new List<NugetPackageChangeEventArg>();
                foreach (var ownerWatch in OwnerWatch)
                {
                    var owner = ownerWatch.Key;
                    var ownerPackages = ownerWatch.Value;
                    var packages = await NugetService.GetOwnedPackages(owner);
                    // we don't check for removed packages... it is extremely rare for packages to be removed
                    var now = DateTimeOffset.Now;
                    foreach (var package in packages)
                    {
                        var changed = true;
                        if (ownerPackages.TryGetValue(package.Title, out NugetPackageData? packageOld))
                        {
                            package.FirstSeen = packageOld.FirstSeen;
                            changed = packageOld.Changed(package) && package.TotalDownloads >= packageOld.TotalDownloads;
                            if (changed)
                            {
                                var totalDownloadsSinceLast = package.TotalDownloads - packageOld.TotalDownloads;
                                if (totalDownloadsSinceLast > 0)
                                {
                                    JS.Log($"Package downloaded: {owner} {package.Title} {package.TotalDownloads} Count: {totalDownloadsSinceLast}");
                                    // log the downloads so downloads over time can be tracked
                                }
                                else
                                {
                                    JS.Log($"Package changed: {owner} {package.Title} {package.TotalDownloads}");
                                }
                                changedPackages.Add(new NugetPackageChangeEventArg { PackageDataOld = packageOld, PackageDataNew = package });
                            }
                        }
                        else
                        {
                            package.FirstSeen = now;
                            JS.Log($"Package found: {owner} {package.Title} {package.TotalDownloads}");
                            changedPackages.Add(new NugetPackageChangeEventArg { PackageDataNew = package });
                        }
                        if (changed)
                        {
                            package.DataTimeStamp = now;
                            ownerPackages[package.Title] = package;
                            var commitTimeStamp = await NugetService.GetNugetPackageLastModified(package);
                            package.CommitTimeStamp = commitTimeStamp ?? packageOld?.CommitTimeStamp;
                        }
                    }
                }
                if (changedPackages.Any())
                {
                    foreach (var p in changedPackages)
                    {
                        await SaveToDB(p.PackageDataNew);
                    }
                    await UpdateDownloadsToday();
                    OnPackageChange?.Invoke(changedPackages);
                }
            }
            catch (Exception ex)
            {
                JS.Log("Update error", ex.ToString());
            }
            _Updating = null;
            UpdateEnd?.Invoke();
        }
    }
}
