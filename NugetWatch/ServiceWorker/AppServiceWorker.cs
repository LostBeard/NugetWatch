using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NugetWatch.Services;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.Toolbox;
using SpawnDev.BlazorJS.WebWorkers;
using System.Text.Json;

namespace NugetWatch.ServiceWorker
{
    public class AppServiceWorker : ServiceWorkerEventHandler
    {
        Window? window = null;
        AssetManifest? assetsManifest = null;
        NugetMonitorService NugetMonitorService;
        bool isProduction;
        string cacheNamePrefix = "offline-cache-";
        string cacheName = "";
        CacheStorage? caches = null;
        List<string>? manifestUrlList = null;
        Uri baseUri;
        public AppServiceWorker(BlazorJSRuntime js, NavigationManager navigationManager, NugetMonitorService nugetMonitorService, IWebAssemblyHostEnvironment hostEnvironment) : base(js)
        {
            baseUri = new Uri(navigationManager.BaseUri);
            NugetMonitorService = nugetMonitorService;
            isProduction = hostEnvironment.IsProduction();
        }
        // called before any ServiceWorker events are handled
        protected override async Task OnInitializedAsync()
        {
            // This service may start in any scope. This will be called before the app runs.
            // If JS.IsWindow == true be careful not stall here.
            // you can do initialization based on the scope that is running
            //Log("GlobalThisTypeName", JS.GlobalThisTypeName);
            window = JS.WindowThis;
            if (ServiceWorkerThis != null)
            {
                caches = ServiceWorkerThis.Caches;
                // get the assets manifest data generated on release build and imported in the service-worker.js
                assetsManifest = JS.Get<AssetManifest?>("assetsManifest");
                if (assetsManifest != null)
                {
                    cacheName = $"{cacheNamePrefix}{assetsManifest.Version}";
                    Log("Offline cache name:", cacheName);
                    manifestUrlList = assetsManifest!.Assets.Select(asset => new Uri(baseUri, asset.Url).ToString()).ToList();
                }
            }
        }
        public class CachedApp
        {
            public AssetManifest AssetManifest { get; set; }
            public DateTimeOffset Installed { get; set; } = DateTimeOffset.Now;
        }
        protected override async Task ServiceWorker_OnInstallAsync(ExtendableEvent e)
        {
            // cache assets (if needed)
            if (assetsManifest != null)
            {
                try
                {
                    var cacheKeys = await caches!.Keys();
                    var offlineCaches = cacheKeys.Where(key => key.StartsWith(cacheNamePrefix) && key != cacheName).ToList();
                    // Fetch and cache all matching items from the assets manifest
                    var assets = assetsManifest.Assets.ToList();
                    long reusedByteLength = 0;
                    long reusedAssetsCount = 0;
                    using var cache = await caches!.Open(cacheName);
                    // check existing cache for assets with unchanged an hash so they can be re-used to save on re-downloading
                    var oldCacheName = offlineCaches.FirstOrDefault();
                    if (!string.IsNullOrEmpty(oldCacheName))
                    {
                        // Trying to re-use unchanged assets
                        using var oldCache = await caches!.Open(oldCacheName);
                        var cachedAppOld = await oldCache.ReadJSON<CachedApp>("cachedApp.json");
                        if (cachedAppOld != null)
                        {
                            var assetsRequestsAlt = assets.ToList();
                            foreach (var asset in assetsRequestsAlt)
                            {
                                var oldAssetIndo = cachedAppOld.AssetManifest.Assets.FirstOrDefault(o => o.Url == asset.Url);
                                var assetFnd = oldAssetIndo != null;
                                var assetUnchanged = assetFnd && oldAssetIndo!.Hash == asset.Hash;
                                if (assetUnchanged)
                                {
                                    using var request = new Request(asset.Url, new RequestOptions { Integrity = asset.Hash, Cache = "no-cache" });
                                    using var resp = await oldCache.Match(request);
                                    if (resp != null)
                                    {
                                        using var clone = resp.Clone();
                                        using var blob = await clone.Blob();
                                        reusedByteLength += blob.Size;
                                        // use the existing item in the new cache to prevent redownloading the same data we already have
                                        await cache.Put(request, resp);
                                        assets.Remove(asset);
                                        reusedAssetsCount++;
                                    }
                                }
                            }
                        }
                    }
                    var assetsRequests = assets.Select(asset => new Request(asset.Url, new RequestOptions { Integrity = asset.Hash, Cache = "no-cache" })).ToList();
                    long downloadedBytes = 0;
                    if (assetsRequests.Any())
                    {
                        await cache.AddAll(assetsRequests);
                        foreach(var asset in assetsRequests)
                        {
                            using var resp = await cache.Match(asset);
                            if (resp != null)
                            {
                                using var blob = await resp.Blob();
                                downloadedBytes += blob.Size;
                            }
                        }
                    }
                    // write the current cache info so we can use it next update
                    var cachedApp = new CachedApp { AssetManifest = assetsManifest };
                    await cache.WriteJSON("service-worker-assets.js", $"var assetsManifest = {JsonSerializer.Serialize(assetsManifest, toJavascriptOptions)};");
                    await cache.WriteJSON("cachedApp.json", cachedApp);
                    Log("Cached:", cacheName, $"Downloaded: {assetsRequests.Count} assets ({downloadedBytes} bytes)", $"Reused: {reusedAssetsCount} assets ({reusedByteLength} bytes)");
                }
                catch (Exception ex)
                {
                    Log("Failed to cache:", cacheName, ex.ToString());
                }
            }
            _ = ServiceWorkerThis!.SkipWaiting();   // returned task can be ignored
        }
        JsonSerializerOptions toJavascriptOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        protected override async Task ServiceWorker_OnActivateAsync(ExtendableEvent e)
        {
            Log($"ServiceWorker_OnActivateAsync");
            // delete old caches
            // Delete unused caches that start with offline prefix
            var cacheKeys = await caches!.Keys();
            var expiredCaches = cacheKeys.Where(key => key.StartsWith(cacheNamePrefix) && key != cacheName).ToList();
            await Task.WhenAll(expiredCaches.Select(key => caches.Delete(key)));
            await ServiceWorkerThis!.Clients.Claim();
        }
        protected override async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
        {
            //Log($"ServiceWorker_OnFetchAsync", e.Request.Method, e.Request.Url);
            Response? response = null;
            using var request = e.Request;
            if (request.Method == "GET" && assetsManifest != null)
            {
                // For all navigation requests, try to serve index.html from cache,
                // unless that request is for an offline resource.
                // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
                var shouldServeIndexHtml = request.Mode == "navigate" && !manifestUrlList!.Any(url => url == request.Url);
                var request1 = shouldServeIndexHtml ? new Request("index.html") : request;
                var cache = await caches!.Open(cacheName);
                response = await cache.Match(request1);
                if (response != null)
                {
                    // cached response used
                    //Log("Cache used", request.Url);
                }
            }
            if (response == null)
            {
                try
                {
                    response = await JS.Fetch(request);
                    //Log("Live used", request.Url);
                    // live response used
                }
                catch (Exception ex)
                {
                    //Log("Failed used", request.Url);
                    // failed response used
                    response = new Response(ex.Message, new ResponseOptions { Status = 500, StatusText = ex.Message, Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } } });
                }
            }
            return response;
        }
        protected override async Task ServiceWorker_OnPeriodicSyncAsync(PeriodicSyncEvent e)
        {
            JS.Log("ServiceWorker_OnPeriodicSyncAsync", e.Tag);
            switch (e.Tag)
            {
                case BackgroundUpdateTag:
                    // refresh stats data
                    JS.Log(">> ServiceWorker_OnPeriodicSyncAsync NugetMonitorService.Update");
                    await NugetMonitorService.Update();
                    JS.Log("<< ServiceWorker_OnPeriodicSyncAsync NugetMonitorService.Update");
                    break;
            }
        }
        public const string BackgroundUpdateTag = "stats-update";
        public async Task<bool> CheckBackgroundPeriodicSync()
        {
            using var navigator = JS.Get<Navigator>("navigator");
            try
            {
                var registration = await navigator.ServiceWorker.Ready.WaitAsync(TimeSpan.FromSeconds(10));
                var tags = await registration.PeriodicSync!.GetTags();
                return tags.Contains(BackgroundUpdateTag);
            }
            catch { }
            return false;
        }
        public async Task UnregisterBackgroundPeriodicSync()
        {
            using var navigator = JS.Get<Navigator>("navigator");
            try
            {
                var registration = await navigator.ServiceWorker.Ready.WaitAsync(TimeSpan.FromSeconds(10));
                await registration.PeriodicSync!.Unregister(BackgroundUpdateTag);
            }
            catch { }
        }
        public async Task<bool> RegisterBackgroundPeriodicSync()
        {
            using var navigator = JS.Get<Navigator>("navigator");
            try
            {
                var registration = await navigator.ServiceWorker.Ready.WaitAsync(TimeSpan.FromSeconds(10));
                await registration.PeriodicSync!.Register(BackgroundUpdateTag, new PeriodicSyncOptions
                {
                    MinInterval = 24 * 60 * 60 * 1000,
                });
                return true;
            }
            catch (Exception ex)
            {
                JS.Log("Periodic Sync could not be registered!", ex.ToString());
            }
            return false;
        }
        void Log(params object[] msg)
        {
            JS.Log(msg);
        }
    }
}
