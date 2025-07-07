using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using TestCore.Services;

namespace Toucan_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private VM_Main vm;
        public MainWindow()
        {
            DataContext = vm = new VM_Main();

            InitializeComponent();

            tb_InputBox.Focus();
            Title = $"Toucan_{Assembly.GetExecutingAssembly().GetName().Version}";
            Closing += MainWindow_Closing;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (vm.ToolboxService is null)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(vm, "Warning", "Function Limited. Require Toolbox V1.1.8567 or later");
                var msg = "Function Limited. Require Toolbox V1.1.8567 or later";
                btn_OpenToolbox.Background = Brushes.Red;
                btn_OpenToolbox.ToolTip = msg;
            }

            vm?.DecodeArgs(Environment.GetCommandLineArgs());
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(DataContext is VM_Main vm)
            {
                if(!vm.IsClosed)
                {
                    vm.Exit.Execute(null);
                }
            }
        }

        private void tb_InputText_GetFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void btn_InputText_Click(object sender, RoutedEventArgs e)
        {
            tb_InputBox.Focus();
        }

        private void btn_EngineUiVisible_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is VM_Main vm)   //TODO, this should not be called twice
            {
                vm.EngineUiVisible = !vm.EngineUiVisible;
            }
        }

        private void sbi_ScriptName_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (vm.CurrentAuthType < TestCore.AuthType.Engineer) return;

            vm.Link.Execute("script");
        }

        private void sbi_ConstLineNo_Click(object sender, MouseButtonEventArgs e)
        {
            if(vm.IsMaintainer)
            {
                vm.SetLineNo.Execute(null);
            }
        }

        private void sbi_ConstStationId_Click(object sender, MouseButtonEventArgs e)
        {
            if(vm.IsMaintainer)
            {
                vm.SetPhysicalStationID.Execute(null);
            }
        }
    }
}
