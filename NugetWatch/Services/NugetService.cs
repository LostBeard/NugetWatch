using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NugetWatch.Services
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/nuget/api/search-query-service-resource
    /// </summary>
    public class NugetService(HttpClient HttpClient, BlazorJSRuntime JS)
    {
        Storage LocalStorage = JS.Get<Storage>("localStorage");
        JsonSerializerOptions JsonSerializerOptionsDefault = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        public async Task<NugetQueryResult?> QueryAPI(string queryString, bool preRelease = true, int? skip = null, int? take = null, string? semVerLevel = "2.0.0")
        {
            var apiUrl = $"https://azuresearch-usnc.nuget.org/query?q={Uri.EscapeDataString(queryString)}&prerelease={preRelease.ToString().ToLowerInvariant()}";
            if (skip != null) apiUrl += $"&skip={skip.Value}";
            if (take != null) apiUrl += $"&take={take.Value}";
            if (!string.IsNullOrEmpty(semVerLevel)) apiUrl += $"&semVerLevel={semVerLevel}";
            try
            {
                var data = await HttpClient.GetFromJsonAsync<NugetQueryResult>(apiUrl, JsonSerializerOptionsDefault);
                return data;
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public async Task<List<NugetPackageData>> QueryAll(string queryString, bool preRelease = true)
        {
            var ret = new List<NugetPackageData>();
            await foreach (var d in Query(queryString, preRelease))
            {
                ret.Add(d);
            }
            return ret;
        }
        public async IAsyncEnumerable<NugetPackageData> Query(string queryString, bool preRelease = true)
        {
            var skip = 0;
            long? pageSize = null;
            long? totalHits = null;
            do
            {
                var result = await QueryAPI(queryString, preRelease, skip);
                if (result == null || result.Data == null || result.Data.Length == 0) break;
                totalHits ??= result.TotalHits;
                pageSize ??= result.Data.Length;
                foreach (var d in result.Data)
                {
                    yield return d;
                }
                skip += result.Data.Length;
                if (skip >= totalHits) break;
            } while (true);
        }
        /// <summary>
        /// Package version publish date.<br/>
        /// This will be cached
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public async Task<DateTimeOffset?> GetNugetPackageVersionPublished(NugetPackageVersion package)
        {
            if (!packageVersionPublishedCache.TryGetValue(package._Id, out var releaseDate))
            {
                var localValue = LocalStorage?.GetJSON<DateTimeOffset?>(package._Id);
                if (localValue != null)
                {
                    releaseDate = localValue.Value;
                    packageVersionPublishedCache[package._Id] = releaseDate;
                }
                else
                {
                    var releaseDateTmp = await GetNugetPackageVersionData(package);
                    if (releaseDateTmp != null)
                    {
                        releaseDate = releaseDateTmp.Published;
                        packageVersionPublishedCache[package._Id] = releaseDate;
                        // this can be cached forever as it will not change 
                        LocalStorage?.SetJSON(package._Id, releaseDate);
                    }
                }
            }
            return releaseDate;
        }
        public async Task<Dictionary<string, DateTimeOffset>> GetNugetPackageVersionsPublished(NugetPackageData package, DateTimeOffset minDate)
        {
            var ret = new Dictionary<string, DateTimeOffset>();
            // reverse the list so most recent is first and quit when the publish date is older than minDate
            var versions = package.Versions.Reverse().ToList();
            foreach (var v in versions)
            {
                var published = await GetNugetPackageVersionPublished(v);
                if (published != null)
                {
                    ret[v._Id] = published.Value;
                    if (minDate > published.Value) break;
                }
            }
            return ret;
        }

        Dictionary<string, DateTimeOffset> packageVersionPublishedCache = new Dictionary<string, DateTimeOffset>();
        public async Task<NugetPackageVersionData?> GetNugetPackageVersionData(NugetPackageVersion version)
        {
            // https://api.nuget.org/v3/registration5-semver1/spawndev.blazorjs/index.json
            try
            {
                var data = await HttpClient.GetFromJsonAsync<NugetPackageVersionData>(version._Id, JsonSerializerOptionsDefault);
                return data;
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public async Task<NugetPackageRegistration?> GetNugetPackageRegistration(NugetPackageData package)
        {
            // https://api.nuget.org/v3/registration5-semver1/spawndev.blazorjs/index.json
            try
            {
                var data = await HttpClient.GetFromJsonAsync<NugetPackageRegistration>(package.Registration, JsonSerializerOptionsDefault);
                return data;
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public async Task<DateTimeOffset?> GetNugetPackageLastModified(NugetPackageData package)
        {
            // https://api.nuget.org/v3/registration5-semver1/spawndev.blazorjs/index.json
            try
            {
                var data = await HttpClient.GetFromJsonAsync<NugetPackageRegistration>(package.Registration, JsonSerializerOptionsDefault);
                return data?.CommitTimeStamp;
            }
            catch (Exception ex)
            {
                var nmt = true;
            }
            return null;
        }
        public async Task<NugetPackageData?> GetNugetPackageData(string packageName, bool preRelease = true)
        {
            await foreach (var d in Query(packageName, preRelease))
            {
                if (d.Title.Equals(packageName, StringComparison.OrdinalIgnoreCase)) return d;
            }
            return null;
        }
        public async Task<List<NugetPackageData>> GetOwnedPackages(string ownerName, bool preRelease = true)
        {
            var packages = await QueryAll(ownerName, preRelease);
            return packages.Where(o => o.Owners.Contains(ownerName, StringComparer.OrdinalIgnoreCase)).ToList();
        }
    }
}
