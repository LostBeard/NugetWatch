using NugetWatch.Services;
using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;
using SpawnDev.BlazorJS.WebWorkers;
using System;

namespace NugetWatch.ServiceWorker
{
    public class AppServiceWorker : ServiceWorkerEventHandler
    {
        NugetService NugetService;
        NugetMonitorService NugetMonitorService;
        public AppServiceWorker(BlazorJSRuntime js, NugetService nugetService, NugetMonitorService nugetMonitorService) : base(js)
        {
            NugetService = nugetService;
            NugetMonitorService = nugetMonitorService;
        }
        // called before any ServiceWorker events are handled
        protected override async Task OnInitializedAsync()
        {
            // This service may start in any scope. This will be called before the app runs.
            // If JS.IsWindow == true be careful not stall here.
            // you can do initialization based on the scope that is running
            Log("GlobalThisTypeName", JS.GlobalThisTypeName);
        }
        protected override async Task ServiceWorker_OnInstallAsync(ExtendableEvent e)
        {
            Log($"ServiceWorker_OnInstallAsync", JS.GlobalThisTypeName);
            _ = ServiceWorkerThis!.SkipWaiting();   // returned task can be ignored
        }
        protected override async Task ServiceWorker_OnActivateAsync(ExtendableEvent e)
        {
            Log($"ServiceWorker_OnActivateAsync");
            await ServiceWorkerThis!.Clients.Claim();
        }
        protected override async Task<Response> ServiceWorker_OnFetchAsync(FetchEvent e)
        {
            Log($"ServiceWorker_OnFetchAsync", e.Request.Method, e.Request.Url);
            Response ret;
            try
            {
                ret = await JS.Fetch(e.Request);
            }
            catch (Exception ex)
            {
                ret = new Response(ex.Message, new ResponseOptions { Status = 500, StatusText = ex.Message, Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } } });
                Log($"ServiceWorker_OnFetchAsync failed: {ex.Message}");
            }
            return ret;
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
            catch(Exception ex) 
            {
                JS.Log("Periodic Sync could not be registered!", ex.ToString());
            }
            return false;
        }
        void Log(params object[] msg) => JS.Log(msg);
    }
}
