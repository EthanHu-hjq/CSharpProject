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
using TestCore.Data;
using TestCore;
using Toucan_WPF.ViewModels;

namespace Toucan_WPF.Ctrls
{
    /// <summary>
    /// Ctrl_ChartView.xaml 的交互逻辑
    /// </summary>
    public partial class Ctrl_ChartView : DockPanel
    {
        public Ctrl_ChartView()
        {
            InitializeComponent();
        }

        private void ActiveStepDataChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView tv && DataContext is VM_Slot vm)
            {
                if (tv.SelectedItem is Nest<TF_StepData> data)
                {
                    vm.StepData = data;
                }
            }
        }
    }
}
