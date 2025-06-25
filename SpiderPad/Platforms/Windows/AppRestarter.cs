using System.Diagnostics;
using System.Reflection;
#if WINDOWS
using Microsoft.UI.Xaml;
using Application = Microsoft.UI.Xaml.Application;
#endif

namespace SpiderPad.Platforms.Windows;

public static class AppRestarter
{
    public static void RestartApp()
    {
#if WINDOWS
        string exePath = Process.GetCurrentProcess().MainModule?.FileName;

        if (!string.IsNullOrEmpty(exePath))
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
            };

            // Start new instance
            Process.Start(psi);

            // Close the current app
            Application.Current?.Exit();
        }
#endif
    }
}
