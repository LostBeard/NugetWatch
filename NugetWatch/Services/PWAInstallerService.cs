using SpawnDev.BlazorJS;
using SpawnDev.BlazorJS.JSObjects;

namespace NugetWatch
{
    public class PWAInstallerService
    {
        public class DialogResult
        {
            public bool Cancelled { get; set; }
        }

        public enum InstallableMessageType
        {
            NONE,       // None. Window_OnBeforeInstallPrompt has not fired.
            ACTIVE,     // beforeinstallprompt has fired and prompt has not been dismissed
            PASSIVE,    // beforeinstallprompt has fired and prompt has been dismissed at least once
            ACCEPTED,    // prompt has return outcome == 'accepted'
        }

        public InstallableMessageType InstallableMessage { get; private set; } = InstallableMessageType.NONE;
        BlazorJSRuntime JS;
        public bool InstallPromptDismissed { get; private set; }
        public bool Accepted { get; private set; }
        public event Action InstallPromotionVisibleChanged;
        private BeforeInstallPromptEvent? DeferredPrompt;
        Storage SessionStorage;
        bool ShowPWAInstallNotification = false;
        public PWAInstallerService(BlazorJSRuntime js)
        {
            JS = js;
            if (JS.IsWindow)
            {
                using var window = JS.Get<Window>("window");
                window.OnBeforeInstallPrompt += Window_OnBeforeInstallPrompt;
                SessionStorage = window.SessionStorage;
                InstallPromptDismissed = !ShowPWAInstallNotification || SessionStorage.GetJSON<bool>(SessionStorageKey);
                // the beforeinstallprompt event can fire before Blazor loads causing a missed event.
                // the event can be caught in Javascript and cached so Blazor can pick it up when it is ready
                DeferredPrompt = JS.Call<BeforeInstallPromptEvent?>("GetDeferredBeforeInstallPromptEvent");
                UpdateInstallableMessage();
            }
        }

        void UpdateInstallableMessage()
        {
            var installableMessage = InstallableMessageType.NONE;
            if (Accepted) installableMessage = InstallableMessageType.ACCEPTED;
            else if (DeferredPrompt == null) installableMessage = InstallableMessageType.NONE;
            else if (!InstallPromptDismissed) installableMessage = InstallableMessageType.ACTIVE;
            else installableMessage = InstallableMessageType.PASSIVE;
            if (InstallableMessage != installableMessage)
            {
                InstallableMessage = installableMessage;
                InstallPromotionVisibleChanged?.Invoke();
            }
        }

        void Window_OnBeforeInstallPrompt(BeforeInstallPromptEvent e)
        {
            JS.Log("Window_OnBeforeInstallPrompt", e);
            e.PreventDefault(); //
            DeferredPrompt = e;
            UpdateInstallableMessage();
        }

        string SessionStorageKey = "PWADismissedInstall";
        void InstallDismissed()
        {
            InstallPromptDismissed = true;
            SessionStorage.SetJSON(SessionStorageKey, InstallPromptDismissed);
            UpdateInstallableMessage();
        }

        public async Task HandleInstallDialog(DialogResult dialogResult)
        {
            try
            {
                if (dialogResult.Cancelled)
                {
                    JS.Log("User cancelled install app promo dialog");
                    InstallDismissed();
                }
                else if (DeferredPrompt != null)
                {
                    var promptResult = await DeferredPrompt.Prompt();
                    var accepted = promptResult != null && promptResult.Outcome == "accepted";
                    Accepted = accepted;
                    JS.Set("_promptResult", promptResult);
                    if (!accepted)
                    {
                        InstallDismissed();
                    }
                    UpdateInstallableMessage();
                }
            }
            catch (Exception ex)
            {
                JS.Log("Error installing app", ex);
            }
        }

    }
}
