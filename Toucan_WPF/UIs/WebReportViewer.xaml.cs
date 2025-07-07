using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// TsXmlViewer.xaml 的交互逻辑
    /// </summary>
    public partial class WebReportViewer : Window
    {
        public string URL
        {
            get { return (string)GetValue(URLProperty); }
            set { SetValue(URLProperty, value); }
        }

        // Using a DependencyProperty as the backing store for URL.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty URLProperty =
            DependencyProperty.Register("URL", typeof(string), typeof(WebReportViewer), new PropertyMetadata(null, UrlChanged));

        private static void UrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is WebReportViewer txv)
            {
                if(e.NewValue is string str)
                {
                    if(string.IsNullOrEmpty(str))
                    {
                        return;
                    }

                    Uri uri= new Uri(str);

                    txv.wb.Navigate(uri);
                }
            }
        }

        public WebReportViewer()
        {
            InitializeComponent();
        }
    }
}
