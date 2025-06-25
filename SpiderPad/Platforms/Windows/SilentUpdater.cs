#if WINDOWS
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using Windows.ApplicationModel.Core;

namespace SpiderPad.Platforms.Windows
{
    /// <summary>
    /// SilentUpdater checks the AppInstaller feed, shows an overlay, installs the MSIX update,
    /// logs progress to LocalState/updater.log, and then relaunches the updated app via protocol.
    /// Call KickOffUpdateCheck() on normal startups (args empty), and handle "updated" in OnLaunched
    /// to dismiss the UpdatingPage modal. Ensure <uap:Protocol Name="spiderpad"/> is in your manifest.
    /// </summary>
    public static class SilentUpdater
    {
        private static readonly HttpClient _http = new();
        private static readonly string LogPath = Path.Combine(FileSystem.Current.AppDataDirectory, "updater.log");
        private static bool _updateInProgress;

        public static void KickOffUpdateCheck()
        {
            if (_updateInProgress)
                return;
            _updateInProgress = true;
            _ = Task.Run(CheckAndApplyUpdatesAsync);
        }

        private static async Task Log(string message)
        {
            try
            {
                var line = $"[{DateTime.Now:O}] {message}{Environment.NewLine}";
                await File.AppendAllTextAsync(LogPath, line);
            }
            catch { }
        }

        private static async Task CheckAndApplyUpdatesAsync()
        {
            try
            {
                await Log("Updater started");

                // 1) Current version
                var pkg = Package.Current;
                var currentVer = new Version(pkg.Id.Version.Major, pkg.Id.Version.Minor, pkg.Id.Version.Build, pkg.Id.Version.Revision);
                await Log($"Current version: {currentVer}");

                // 2) Fetch feed
                var feedUri = new Uri($"https://spiderpad-{ChannelConfig.Channel}.s3.eu-west-3.amazonaws.com/{ChannelConfig.Channel}/Spiderpad-latest.appinstaller");
                await Log($"Fetching feed: {feedUri}");
                var xml = await _http.GetStringAsync(feedUri);
                var doc = XDocument.Parse(xml);
                var ns = doc.Root.Name.Namespace;
                var main = doc.Root.Element(ns + "MainPackage");
                var feedVer = Version.Parse(main.Attribute("Version").Value);
                var msixUri = new Uri(main.Attribute("Uri").Value);
                await Log($"Feed version: {feedVer}, MSIX URI: {msixUri}");

                if (feedVer <= currentVer)
                {
                    await Log("No update needed");
                    return;
                }

                // 3) Show overlay
                await Log("Displaying UpdatingPage overlay");
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var nav = Application.Current?.MainPage?.Navigation;
                    if (nav != null)
                    {
                        while (nav.ModalStack.Count > 0)
                            await nav.PopModalAsync(false);
                        await nav.PushModalAsync(new UpdatingPage(), false);
                    }
                });
                await Task.Delay(300);


                var flagPath = Path.Combine(FileSystem.Current.AppDataDirectory, "update.flag");
                await File.WriteAllTextAsync(flagPath, "in_progress");


                await Log("Applying MSIX with ForceApplicationShutdown");
                var manager = new PackageManager();
                try
                {
                    await manager.AddPackageAsync(
                        msixUri,
                        null,
                        DeploymentOptions.ForceApplicationShutdown,
                        null, null, null, null);
                }
                catch (Exception ex)
                {
                    await Log($"Installation failed: {ex}");
                    throw; // Re-throw to trigger final error handling
                }

            }
            catch (Exception ex)
            {
                await Log($"Updater error: {ex}");
                MainThread.BeginInvokeOnMainThread(async () =>
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
                _updateInProgress = false;
                await Log("Updater finished");
            }
        }
    }
}
#endif
