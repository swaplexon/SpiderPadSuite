using SpiderPad.interfaces;
using SpiderPad.Platforms.Windows;
using System.Diagnostics;

namespace SpiderPad
{
    public partial class App : Application
    {
        
        public App()
        {
            
            InitializeComponent();

            MainPage = new AppShell();

           
        }

        
    }
}
