using DailyApp.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace DailyApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            // Create the main window of the application
            return new MainWin
            {
                Title = "Daily App",
                Width = 800,
                Height = 600
            };
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
        }
    }

}
