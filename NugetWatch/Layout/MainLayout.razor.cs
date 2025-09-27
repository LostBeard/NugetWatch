using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using NugetWatch.Layout.AppTray;
using NugetWatch.Services;
using NugetWatch.ServiceWorker;
using Radzen;
using SpawnDev.BlazorJS;
using System.Xml.Linq;

namespace NugetWatch.Layout
{
    public partial class MainLayout
    {
        
        [Inject]
        PWAInstallerTrayIconService CustomPWAInstallerService { get; set; } = default!;
        [Inject]
        BlazorJSRuntime JS { get; set; } = default!;
        [Inject]
        NotificationService NotificationService { get; set; } = default!;
        [Inject]
        DialogService DialogService { get; set; } = default!;
        [Inject]
        ContextMenuService ContextMenuService { get; set; } = default!;
        [Inject]
        AppTrayService TrayIconService { get; set; } = default!;
        [Inject]
        protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject]
        MainLayoutService MainLayoutService { get; set; } = default!;
        [Inject]
        ThemeService ThemeService { get; set; } = default!;
        [Inject]
        NugetMonitorService NugetMonitorService { get; set; } = default!;
        [Inject]
        NugetService NugetService { get; set; } = default!;


        [Inject]
        AppServiceWorker AppServiceWorker { get; set; } = default!;

        string Title => MainLayoutService.Title;
        bool leftSidebarExpanded = false;
        bool rightSidebarExpanded = false;
        bool rightSidebarEnabled = false;
        public Type? PageType { get; private set; }
        public string PageTypeName => PageType?.Name ?? "";
        public string Location { get; private set; } = "";
        public string? HistoryEntryState { get; private set; }
        public DateTime LocationUpdated { get; private set; } = DateTime.MinValue;
        protected override void OnInitialized()
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            MainLayoutService.OnTitleChanged += MainLayoutService_OnTitleChanged;
        }
        async Task EnableBackgroundSync(bool enable)
        {
            if (enable)
            {
                var succ = await AppServiceWorker.RegisterBackgroundPeriodicSync();
                if (succ)
                {
                    NotificationService.Notify(NotificationSeverity.Info, "Background sync enabled");
                }
            }
            else
            {
                await AppServiceWorker.UnregisterBackgroundPeriodicSync();
            }
        }
        private void MainLayoutService_OnTitleChanged()
        {
            StateHasChanged();
        }
        private void NavigationManager_LocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
        {
            AfterLocationChanged(e.HistoryEntryState);
        }
        protected override void OnAfterRender(bool firstRender)
        {
            MainLayoutService.TriggerOnAfterRender(this, firstRender);
            if (firstRender)
            {
                AfterLocationChanged();
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) await CustomPWAInstallerService.AfterFirstRenderAsync();
        }
        void OwnerContextMenu(MouseEventArgs args, string owner)
        {
            var options = new List<ContextMenuItem>();
            options.Add(new ContextMenuItem
            {
                Text = "Remove",
                Icon = "delete",
                Value = async () =>
                {
                    var confirm = await DialogService.Confirm($"Remove '{owner}' watch?", "Are you sure?");
                    if (confirm == true)
                    {
                        await NugetMonitorService.RemoveOwnerWatch(owner);
                        StateHasChanged();
                    }
                }
            });
            ContextMenuService.Open(args, options, (MenuItemEventArgs args) =>
            {
                ContextMenuService.Close();
                try
                {
                    if (args.Value is Action action) action();
                    else if (args.Value is Func<Task> func) _ = func();
                }
                catch (Exception ex)
                {
                    var nmt = true;
                }
            });
        }
        async Task AddWatch()
        {
            var owner = await DialogService.ShowInputBox("Add Nuget Owner", new InputBoxOptions { });
            if (owner == null) return;
            owner = owner.Trim();
            if (string.IsNullOrWhiteSpace(owner)) return;
            if (NugetMonitorService.Owners.Contains(owner))
            {
                NotificationService.Notify(NotificationSeverity.Info, $"Already watching '{owner}'");
                return;
            }
            var packages = await NugetService.GetOwnedPackages(owner);
            var confirm = await DialogService.Confirm($"{packages.Count} packages found for '{owner}'. Confirm add?");
            if (confirm == true)
            {
                NotificationService.Notify(NotificationSeverity.Info, $"Now watching '{owner}'");
                await NugetMonitorService.AddOwnerWatch(owner);
                NavigationManager.NavigateTo(owner);
            }
        }
        void AfterLocationChanged(string? historyEntryState = null)
        {
            var pageType = Body != null && Body.Target != null && Body.Target is RouteView routeView ? routeView.RouteData.PageType : null;
            var location = NavigationManager.Uri;
            if (PageType == pageType && Location == location)
            {
#if DEBUG && false
                Console.WriteLine($"SendLocationChanged: false");
#endif
                return;
            }
            LocationUpdated = DateTime.Now;
            PageType = pageType;
            Location = location;
            HistoryEntryState = historyEntryState;
#if DEBUG
            Console.WriteLine($"LocationChanged: {PageTypeName} [{HistoryEntryState ?? ""}] {Location}");
#endif
        }
    }
}
