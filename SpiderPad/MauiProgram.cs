using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using SpiderPad.interfaces;
using SpiderPad.Platforms.Windows;
using System.Threading.Tasks;

namespace SpiderPad;

public static class MauiProgram
{
    public static IServiceProvider Services { get; private set; }
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows => windows
                    .OnLaunched(async (window, args) =>
                    {
                        var flagPath = Path.Combine(FileSystem.Current.AppDataDirectory, "update.flag");

                        if (File.Exists(flagPath)) // Match restart argument
                        {
                            File.Delete(flagPath);
                            var nav = Application.Current?.MainPage?.Navigation;
                            if (nav?.ModalStack.Count > 0) await nav.PopModalAsync();
                            await Toast.Make("Update installed").Show();
                        }
                        else if (string.IsNullOrEmpty(args.Arguments))
                        {
                            SilentUpdater.KickOffUpdateCheck(); // Normal launch
                        }
                    }));
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if WINDOWS
        builder.Services.AddSingleton<IVersionProvider, WindowsVersionProvider>();

#endif



#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        Services = app.Services;
        return app;
    }
}
