using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Toucan_WPF.ViewModels;
using ToucanCore.Abstraction.Engine;

namespace Toucan_WPF.Ctrls
{
    /// <summary>
    /// Interaction logic for Ctrl_Execution.xaml
    /// </summary>
    public partial class Ctrl_Execution : UserControl
    {
        VM_Execution vm;
        public Ctrl_Execution()
        {
            InitializeComponent();
            DataContextChanged += Ctrl_Execution_DataContextChanged;
        }

        private void Ctrl_Execution_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is VM_Execution exec)
            {
                ug_Slots.Children.Clear();
                vm = exec;

                vm.Initialized += Vm_Initialized;
                
            }
        }

        private void Vm_Initialized(object sender, EventArgs e)
        {
            if (vm is VM_Execution exec)
            {
                ug_Slots.Children.Clear();
                foreach (var slot in exec.Slots)
                {
                    ug_Slots.Children.Add(new Ctrl_Slot() { DataContext = slot });
                }

                int row = 0;
                int col = 0;
                if (exec.Script is IExecutionUISetting uisetting)
                {
                    row = uisetting.SlotRow;
                    col = uisetting.SlotColumn;
                }

                if (row > 0)
                {
                    vm.SlotRows = row;

                    if (col > 0)
                    {
                        vm.SlotColumns = col;
                    }
                    else
                    {
                        col = vm.Execution.SocketCount / row;
                        if (vm.Execution.SocketCount % row != 0)
                        {
                            col++;
                        }
                        vm.SlotColumns = col;
                    }
                }
                else if (col > 0)
                {
                    vm.SlotColumns = col;
                    row = vm.Execution.SocketCount / col;
                    if (vm.Execution.SocketCount % col != 0)
                    {
                        row++;
                    }
                    vm.SlotRows = row;
                }
                else
                {
                    var cnt = exec.Slots.Length;

                    if (ug_Slots.ActualWidth <= 0)
                    {
                        vm.SlotColumns = cnt;
                        vm.SlotRows = 1;
                    }
                    else
                    {
                        var wid_est = Math.Max(160, ug_Slots.ActualWidth / 6);  // this actualwidth might be 0 for init UI when call script directly

                        col = (int)(ug_Slots.ActualWidth / wid_est);

                        row = 1;
                        if (col < cnt)
                        {
                            row = cnt / col;

                            if (cnt % col != 0)
                            {
                                row++;
                            }
                        }
                        else
                        {
                            col = cnt;
                        }

                        vm.SlotColumns = col;
                        vm.SlotRows = row;
                    }
                }
            }
        }
    }
}
