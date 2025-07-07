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
using ToucanCore.Engine;

namespace ToucanCore.UIs
{
    /// <summary>
    /// MouseSimHelper.xaml 的交互逻辑
    /// </summary>
    public partial class MouseSimHelper : Window
    {


        public string WindowTitle
        {
            get { return (string)GetValue(WindowTitleProperty); }
            set { SetValue(WindowTitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WindowTitle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowTitleProperty =
            DependencyProperty.Register("WindowTitle", typeof(string), typeof(MouseSimHelper), new PropertyMetadata(null));

        public string WindowClass
        {
            get { return (string)GetValue(WindowClassProperty); }
            set { SetValue(WindowClassProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WindowClass.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WindowClassProperty =
            DependencyProperty.Register("WindowClass", typeof(string), typeof(MouseSimHelper), new PropertyMetadata(null));





        public MouseSimHelper()
        {
            InitializeComponent();

            MouseUp += MouseSimHelper_MouseUp;
        }

        private void MouseSimHelper_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var p = e.MouseDevice.GetPosition(null);
            var d = WindowsApi.WindowFromPoint((int)p.X, (int)p.Y);
        }
    }
}
