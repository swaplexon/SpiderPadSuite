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

    public static void KickOffUpdateCheck()
    {
        if (_running) return;
        _running = true;
        _ = Task.Run(CheckAndLaunchUpdaterAsync);
    }

    // ─────────────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────────────
    private static async Task CheckAndLaunchUpdaterAsync()
    {
        try
        {
            await Log("Updater check started");

            // 1️⃣  Current package version
            var current = Package.Current.Id.Version;
            var currentVer = new Version(current.Major, current.Minor, current.Build, current.Revision);
            await Log($"Current version {currentVer}");

            // 2️⃣  Grab .appinstaller feed for this channel
            string channel = ChannelConfig.Channel;                 // "alpha", "beta", …
            var feedUri = new Uri(
                $"https://spiderpad-{channel}.s3.eu-west-3.amazonaws.com/{channel}/Spiderpad-latest.appinstaller");

            await Log($"Fetching feed {feedUri}");
            var feedXml = await _http.GetStringAsync(feedUri);

            var doc = XDocument.Parse(feedXml);
            var ns = doc.Root!.Name.Namespace;
            var main = doc.Root!.Element(ns + "MainPackage")!;
            var feedVer = Version.Parse(main.Attribute("Version")!.Value);
            var msixUri = new Uri(main.Attribute("Uri")!.Value);

            await Log($"Feed version {feedVer}; MSIX {msixUri}");

            if (feedVer == currentVer)
            {
                await Log("No update available");
                return;
            }

            // 3️⃣  Show modal overlay
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

            // 4️⃣  Launch helper EXE and quit
            var updaterExe = Path.Combine(
                AppContext.BaseDirectory, "Updater", "Spiderpad.Updater.exe");
            

            if (!File.Exists(updaterExe))
            {
                await Log($"Updater missing at {updaterExe}");
                return;
            }

            var args = $"\"{msixUri}\" {channel}";
            await Log($"Starting helper: {updaterExe} {args}");

            var psi = new ProcessStartInfo(updaterExe)
            {
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(updaterExe)!
            };
            Process.Start(psi);

            // tiny grace so the helper definitely spawns
            await Task.Delay(200);
            await Log("Exiting MAUI to allow update");
            Application.Current.Quit();
        }
        catch (Exception ex)
        {
            await Log($"Updater error {ex}");
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
        finally
        {
            _running = false;
            await Log("Updater check finished");
        }
    }

    private static Task Log(string msg)
    {
        var line = $"[{DateTime.Now:O}] {msg}{Environment.NewLine}";
        return File.AppendAllTextAsync(LogPath, line);
    }
}
#endif
