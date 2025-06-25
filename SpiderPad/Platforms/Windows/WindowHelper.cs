using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT.Interop;

namespace SpiderPad.Platforms.Windows
{
    public static class WindowHelper
    {
        public static void SetWindowTitle(string title)
        {
            var window = Application.Current.Windows.FirstOrDefault();
            if (window?.Handler?.PlatformView is Window nativeWindow)
            {
                var hwnd = WindowNative.GetWindowHandle(nativeWindow);
                var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = AppWindow.GetFromWindowId(windowId);
                appWindow.Title = title;
            }
        }
    }
}
