using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace Spiderpad.Updater
{
    internal class Program
    {
        static void Log(string message)
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "spiderpadupdater.log");
            File.AppendAllText(logPath,
                $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        }
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 3)
            {
                Log("Insufficient arguments. Usage: Spiderpad.Updater.exe <PackageUri> <channel>");
                return 1;
            }

            string packageUriText = args[0];
            string channel = args[1];
            string packageFamilyName = args[2];
            Log($"Update started for channel: {channel}, URI: {packageUriText}, App Pfn: {packageFamilyName}");

            // 1. Add process existence check
            var processName = $"Spiderpad-{CultureInfo.InvariantCulture.TextInfo.ToTitleCase(channel)}";
            var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));

            if (processes.Any())
            {
                Log($"Found {processes.Length} running instances. Waiting for shutdown...");
                foreach (var process in processes)
                {
                    process.Kill();
                    process.WaitForExit(5000); // Wait up to 5 seconds per process
                }
            }

            // Give the main app time to exit
            await Task.Delay(3000);

            try
            {
                var uri = new Uri(packageUriText);
                var packageManager = new PackageManager();

                // 2. Add progress tracking
                var progress = new Progress<DeploymentProgress>(p =>
                    Log($"Update progress: {p.state} ({p.percentage}%)"));

                Log("Starting package installation");
                var result = await packageManager.AddPackageAsync(
                    uri,
                    null,
                    DeploymentOptions.ForceApplicationShutdown | DeploymentOptions.ForceTargetApplicationShutdown
                ).AsTask();

                // 3. Verify installation result
                if (result.IsRegistered)
                {
                    Log("Update successful");
                    var pkg = packageManager.FindPackagesForUser(null, packageFamilyName)
             .OrderByDescending(p => p.Id.Version)
             .FirstOrDefault();

                    if (pkg == null)
                    {
                        Log($"❌ No package found for {packageFamilyName}");
                        return 1;
                    }


                    var appUserModelId = $"{packageFamilyName}!App";
                    var shellCmd = $"shell:appsFolder\\{appUserModelId}";

                    Log($"🔄 Relaunching via explorer.exe {shellCmd}");
                    Process.Start(new ProcessStartInfo("explorer.exe", shellCmd)
                    {
                        UseShellExecute = true
                    });
                    return 0;

                    Log($"ERROR: could not locate package at {packageFamilyName}");
                    return 1;
                }

                Log($"Update failed: {result.ErrorText}");
                return 1;
            }
            catch (Exception ex)
            {
                Log($"Critical error: {ex.Message}");
                File.WriteAllText("updater-error.log", ex.ToString());
                return 1;
            }
        }
    }
}

