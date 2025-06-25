using SpiderPad.interfaces;
using SpiderPad.Platforms.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
[assembly: Dependency(typeof(WindowsVersionProvider))]
namespace SpiderPad.Platforms.Windows
{
    public class WindowsVersionProvider : IVersionProvider
    {
        public string GetAppVersion()
        {
            try
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}";
            }
            catch
            {
                return "0.0.0.0";
            }
        }
    }
}
