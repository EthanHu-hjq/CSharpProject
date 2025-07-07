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
using System.Windows.Shapes;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// ChartView.xaml 的交互逻辑
    /// </summary>
    public partial class ChartView : Window
    {
        public ChartView()
        {
            InitializeComponent();
        }

        private void ActiveStepDataChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }
    }
}
