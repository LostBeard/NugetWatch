using Microsoft.AspNetCore.Components.Web;
using NugetWatch.Layout.AppTray;
using Radzen;
using SpawnDev.BlazorJS;
using static NugetWatch.PWAInstallerService;


namespace NugetWatch
{
    public class PWAInstallerTrayIconService
    {
        PWAInstallerService PWAInstallerService;
        DialogService DialogService;
        AppTrayIcon TrayIcon;
        AppTrayService TrayIconService;
        bool IsShown = false;
        bool FirstRenderFired = false;
        NotificationService NotificationService;
        public PWAInstallerTrayIconService(PWAInstallerService pwaInstallerService, DialogService dialogService, AppTrayService trayIconService, NotificationService notificationService)
        {
            DialogService = dialogService;
            PWAInstallerService = pwaInstallerService;
            TrayIconService = trayIconService;
            NotificationService = notificationService;
            TrayIcon = new AppTrayIcon
            {
                Title = "Install PWA",
                ClickCallback = TrayIcon_ClickCallback,
                Icon = "install_mobile",
                Style = "cursor: pointer;",
                Visible = true,
            };
        }

        // called from inside MainLayout.razor OnAfterRenderAsync to enable DialogService
        public Task AfterFirstRenderAsync()
        {
            if (FirstRenderFired) return Task.CompletedTask;
            FirstRenderFired = true;
            PWAInstallerService.InstallPromotionVisibleChanged += PWAInstallerService_InstallPromotionVisibleChanged;
            PWAInstallerService_InstallPromotionVisibleChanged();
            return Task.CompletedTask;
        }

        void ShowIcon()
        {
            if (IsShown) return;
            IsShown = true;
            TrayIconService.Add(TrayIcon);
        }

        void HideIcon()
        {
            if (!IsShown) return;
            IsShown = false;
            TrayIconService.Remove(TrayIcon);
        }

        void TrayIcon_ClickCallback(MouseEventArgs mouseEventArgs)
        {
            _ = ShowInstallModalDialog();
        }

        bool useModal = false;
        async Task ShowInstallModalDialog()
        {
            try
            {
                if (useModal)
                {
                    var result = await DialogService.Confirm("Enable opening this site from your desktop or home screen?", "Install PWA?");
                    var cancelled = result != true;
                    await PWAInstallerService.HandleInstallDialog(new DialogResult { Cancelled = cancelled });
                }
                else
                {
                    await PWAInstallerService.HandleInstallDialog(new DialogResult { Cancelled = false });
                }
            }
            catch { }
        }

        private void PWAInstallerService_InstallPromotionVisibleChanged()
        {
            // if installable show install button
            var state = PWAInstallerService.InstallableMessage;
            if (state == InstallableMessageType.NONE || state == InstallableMessageType.ACCEPTED)
            {
                HideIcon();
            }
            else
            {
                ShowIcon();
            }
            // if installable and in active mode, show install modal dialog
            if (PWAInstallerService.InstallableMessage == InstallableMessageType.ACTIVE)
            {
                if (useModal)
                {
                    _ = ShowInstallModalDialog();
                }
                else
                {
                    TrayIcon.IconStyle = IconStyle.Info;
                    NotificationService.Notify(NotificationSeverity.Info, "Click to install PWA", "Enable opening this site from your desktop or home screen?", click: async (e) =>
                    {
                        await PWAInstallerService.HandleInstallDialog(new DialogResult { Cancelled = false });
                    }, closeOnClick: true, duration: 5000);
                }
            }
            else
            {
                TrayIcon.IconStyle = null;
            }
            TrayIconService.StateHasChanged();
        }
    }
}
