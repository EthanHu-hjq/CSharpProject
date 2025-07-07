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
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using Toucan_WPF.ViewModels;
using ToucanCore.Abstraction.Engine;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// SpecEditor.xaml 的交互逻辑
    /// </summary>
    public partial class SpecEditor : Window
    {
        VM_SpecEditor vm;
        
        public SpecEditor()
        {
            DataContext = vm = new ViewModels.VM_SpecEditor();
            InitializeComponent();
        }

        public SpecEditor(TF_Spec spec, IAuthService auth, ITimeService time, IScript script, SpecType type = SpecType.Normal) : this()
        {
            DataContext = vm = new ViewModels.VM_SpecEditor(auth, time, script, type);
            InitializeComponent();
            vm.OpenSpec.Execute(spec);
        }

        private void mi_GoldenSampleSpec_Click(object sender, RoutedEventArgs e)
        {
            List<VM_Limit> limits = new List<VM_Limit>();
            foreach (var item in dg_TestItems.SelectedItems)
            {
                if (item is VM_Limit nl)
                {
                    limits.Add(nl);
                }

            }
            vm.OpenGoldenSampleSpec.Execute(limits);
        }

        private void mi_SecondarySpec_Click(object sender, RoutedEventArgs e)
        {
            List<VM_Limit> limits = new List<VM_Limit>();
            foreach(var item in dg_TestItems.SelectedItems)
            {
                if(item is VM_Limit nl)
                {
                    limits.Add(nl);
                }
                    
            }
            vm.OpenSecondarySampleSpec.Execute(limits);
        }

        private void mi_EditLimit_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem mi)
            {
                if(dg_TestItems.SelectedItem is VM_Limit nl)
                {
                    LimitEditor le = new LimitEditor(nl.NestLimit);
                    le.ShowDialog();

                    dg_TestItems.Items.Refresh();
                }
                else
                {
                }
            }
        }

        private void mi_ClearGoldenSampleSpec_Click(object sender, RoutedEventArgs e)
        {
            if(vm.Script != null)
            {
                if (vm.Script.SystemConfig.General.GolderSampleSpec != null)
                {
                    if(MessageBox.Show("Are you sure to Clear Golden Sample Spec", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        vm.Script.SystemConfig.General.GolderSampleSpec = null;
                        vm.Script.SystemConfig.Save();
                    }
                }
            }
        }

        private void mi_ClearSecondarySpec_Click(object sender, RoutedEventArgs e)
        {
            if (vm.Script?.SystemConfig?.General?.SecondarySpecs != null)
            {
                if (vm.Script.SystemConfig.General.SecondarySpecs.Count > 0)
                {
                    if (MessageBox.Show("Are you sure to Clear Secondary Spec", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        vm.Spec.Secondary = null;
                        vm.Script.SystemConfig.General.SecondarySpecs.Clear();
                        vm.Script.SystemConfig.Save();

                    }
                }
            }
                
        }
    }
}
