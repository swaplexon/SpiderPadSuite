#if WINDOWS
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;

namespace SpiderPad.Platforms.Windows;

public static class SilentUpdater
{
    private static readonly HttpClient _http = new();
    private static readonly string LogPath =
        Path.Combine(FileSystem.Current.AppDataDirectory, "updater.log");
    private static bool _running;
    private static bool _exiting;
    private static IDispatcherTimer _updateTimer;

    public static void StartPeriodicUpdateCheck(TimeSpan interval)
    {
        _updateTimer = Application.Current.Dispatcher.CreateTimer();
        _updateTimer.Interval = interval;
        _updateTimer.Tick += (s, e) => KickOffUpdateCheck();
        _updateTimer.Start();
    }

    public static void StopPeriodicUpdateCheck()
    {
        _updateTimer?.Stop();
    }

    public static void KickOffUpdateCheck()
    {
        if (_running) return;
        _running = true;
        _ = Task.Run(CheckAndLaunchUpdaterAsync);
    }

    // ─────────────────────────────────────────────────────────────────────
    // PRIVATE IMPLEMENTATION
    // ─────────────────────────────────────────────────────────────────────
    private static async Task CheckAndLaunchUpdaterAsync()
    {
        try
        {
            await Log("Updater check started");

            // 1. Current package version
            var current = Package.Current.Id.Version;
            var currentVer = new Version(current.Major, current.Minor, current.Build, current.Revision);
            await Log($"Current version: {currentVer}");

            // 2. Fetch .appinstaller feed
            string channel = ChannelConfig.Channel;
            var feedUri = new Uri(
                $"https://spiderpad-alpha.s3.eu-west-3.amazonaws.com/{channel}/Spiderpad-latest.appinstaller");

            await Log($"Fetching feed: {feedUri}");
            var feedXml = await _http.GetStringAsync(feedUri);

            var doc = XDocument.Parse(feedXml);
            var ns = doc.Root!.Name.Namespace;
            var main = doc.Root!.Element(ns + "MainPackage")!;
            var feedVer = Version.Parse(main.Attribute("Version")!.Value);
            var msixUri = new Uri(main.Attribute("Uri")!.Value);

            await Log($"Feed version: {feedVer}, MSIX: {msixUri}");

            if (feedVer == currentVer)
            {
                await Log("No update required");
                return;
            }

            // 3. Show update UI
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var nav = Application.Current?.MainPage?.Navigation;
                if (nav != null)
                {
                    while (nav.ModalStack.Count > 0)
                        await nav.PopModalAsync(false);
                    await nav.PushModalAsync(new UpdatingPage(), false);
                }
            });

            // 4. Prepare updater launch
            var updaterExe = Path.Combine(
                AppContext.BaseDirectory, "SpiderpadUpdater.exe");
            var dst = Path.Combine(Path.GetTempPath(), "SpiderpadUpdater.exe");
            File.Copy(updaterExe, dst, true);            

            if (!File.Exists(updaterExe))
            {
                await Log($"‼️ Updater missing at {updaterExe}");
                return;
            }

            string pfn = Package.Current.Id.FamilyName;

            var args = $"\"{msixUri}\" {channel} \"{pfn}\"";
            await Log($"Launching helper: {dst} {args}");

            // 5. Start helper process
            var psi = new ProcessStartInfo(dst)
            {
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
               
            };

            var updaterProcess = Process.Start(psi);
            if (updaterProcess == null || updaterProcess.HasExited)
            {
                await Log("‼️ Helper failed to launch");
                return;
            }

            // 6. Ensure clean exit
            _exiting = true;
            await Log("Initiating application shutdown");

            // Give helper time to initialize
            await Task.Delay(500);

            // Terminate gracefully
            Application.Current?.Quit();

            // Force exit if still running after delay
            await Task.Delay(1000);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            await Log($"🚨 Updater error: {ex}");
            await CleanupUI();
        }
        finally
        {
            if (!_exiting)
            {
                _running = false;
                await Log("Updater check completed");
            }
        }
    }

    private static async Task CleanupUI()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var nav = Application.Current?.MainPage?.Navigation;
            if (nav != null)
            {
                while (nav.ModalStack.Count > 0)
                    await nav.PopModalAsync(false);
            }
        });
    }

    private static async Task Log(string msg)
    {
        try
        {
            var line = $"[{DateTime.Now:O}] {msg}{Environment.NewLine}";
            await File.AppendAllTextAsync(LogPath, line);
        }
        catch { /* Ignore log failures */ }
    }
}
#endif
