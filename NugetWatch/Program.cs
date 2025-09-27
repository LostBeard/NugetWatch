using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using NugetWatch;
using NugetWatch.Layout;
using NugetWatch.Layout.AppTray;
using NugetWatch.Services;
using Radzen;
using SpawnDev.BlazorJS;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddBlazorJSRuntime(out var JS);
builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<NugetService>();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddSingleton<DialogService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<TooltipService>();
builder.Services.AddSingleton<ContextMenuService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<AppTrayService>();
builder.Services.AddSingleton<MainLayoutService>();
builder.Services.AddSingleton<ThemeTrayIconService>();
builder.Services.AddSingleton<AudioService>();

builder.Services.AddSingleton<NugetMonitorService>();
builder.Services.AddSingleton<KeepAliveService>();

if (JS.IsWindow)
{
    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
}
var host = await builder.Build().StartBackgroundServices();

#if DEBUG && false
var NugetService = host.Services.GetRequiredService<NugetService>();
var stats = await NugetService.GetNugetPackageData("SpawnDev.BlazorJS");
var owned = await NugetService.GetOwnedPackages("LostBeard");
var nmt = true;
#endif

await host.BlazorJSRunAsync();
