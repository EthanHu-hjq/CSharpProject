using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DailyApp.Views
{
    /// <summary>
    /// LoginUC.xaml 的交互逻辑
    /// </summary>
    public partial class LoginUC : UserControl
    {
        public LoginUC()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            LoginGrid.Visibility = Visibility.Hidden; // 隐藏登录页面
            RegisterGrid.Visibility = Visibility.Visible; // 显示注册页面
        }

        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterGrid.Visibility = Visibility.Hidden; // 隐藏注册页面
            LoginGrid.Visibility = Visibility.Visible; // 显示登录页面
        }
    }
}
