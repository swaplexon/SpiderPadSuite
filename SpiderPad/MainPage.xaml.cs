using SpiderPad.interfaces;
using SpiderPad.Platforms.Windows;

namespace SpiderPad
{
    public partial class MainPage : ContentPage
    {
        int count = 0;
        string version = string.Empty;

        public MainPage()
        {
            InitializeComponent();

#if WINDOWS
            GetVersionNumber();
#endif
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            if (count == 1)
                CounterBtn.Text = $"Clicked {count} time";
            else
                CounterBtn.Text = $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
            //AppRestarter.RestartApp();
        }

        private void GetVersionNumber()
        {

            var versionProvider = MauiProgram.Services.GetService<IVersionProvider>();
            string version = versionProvider?.GetAppVersion() ?? "dev";

            WelcomeLabel.Text = $"Overnight Deploy v{version}!";

        }

    }

}
