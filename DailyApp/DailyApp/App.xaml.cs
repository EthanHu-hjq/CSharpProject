using ControlzEx.Theming;
using DailyApp.ViewModels;
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
            containerRegistry.RegisterDialog<LoginUC, LoginUCViewModel>();
        }

        //重写初始化方法，判断是否登录，若未登录则跳转到登录页面
        protected override void OnInitialized()
        {
            var dialog = Container.Resolve<IDialogService>();
            dialog.ShowDialog("LoginUC",callback =>
            {
                if(callback.Result != ButtonResult.OK)
                {
                    Environment.Exit(0);
                    return;
                }
            });
            base.OnInitialized();
        }

    }

}
