using System.Configuration;
using System.Data;
using System.Security.Permissions;
using System.Windows;

namespace ScreenRecordTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Views.MainView mainView = new Views.MainView();
            mainView.Show();
        }
    }

}
