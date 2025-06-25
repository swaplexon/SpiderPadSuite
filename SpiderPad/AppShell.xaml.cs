using SpiderPad.interfaces;
using SpiderPad.Platforms.Windows;

namespace SpiderPad
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
#if WINDOWS
            var versionProvider = MauiProgram.Services.GetService<IVersionProvider>();
            string version = versionProvider?.GetAppVersion() ?? "dev";
            string channel = ChannelConfig.Channel;
            if (string.IsNullOrEmpty(channel))
                this.Title = $"SPIDERPAD v{version}";
            channel = char.ToUpper(channel[0]) + channel.Substring(1).ToLowerInvariant();
            this.Title = $"SPIDERPAD {channel} v{version}";

#endif
        }
    }
}
