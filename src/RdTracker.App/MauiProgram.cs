using Microsoft.Extensions.Logging;
using RdTracker.UI;

namespace RdTracker.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        // All screens and services live in RdTracker.UI / RdTracker.Core and are shared with the web host.
        builder.Services.AddRdTracker();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
