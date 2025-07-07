using ControlzEx.Standard;
using MahApps.Metro.Controls.Dialogs;
using ScottPlot;
using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TestCore;
using TestCore.Data;
using TestCore.Services;
using Toucan_WPF.ViewModels;
using ToucanCore.Engine;
using ToucanCore.Misc;

namespace Toucan_WPF.Ctrls
{
    /// <summary>
    /// Interaction logic for Ctrl_Slot.xaml
    /// </summary>
    public partial class Ctrl_Slot : UserControl
    {
        WpfPlot wpfplot { get; }

        public Ctrl_Slot()
        {
            InitializeComponent();
            DataContextChanged += Ctrl_Slot_DataContextChanged;

            wpfplot = new WpfPlot() { Name = "wpfplot", Margin = new Thickness(1)};
            ug_Plot.Children.Add(wpfplot);

            wpfplot_Failure.RightClicked -= wpfplot_Failure.DefaultRightClickEvent;
        }

        private void Ctrl_Slot_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is VM_Slot vm)
            {
                vm.ActivePlot = wpfplot;
                vm.FailurePlot = wpfplot_Failure;
                vm.Result.TestEnd += Rs_TestEnd;

                if(vm.ItemPlotMap.Count > 0)
                {
                    foreach (var item in vm.ItemPlotMap)
                    {
                        ug_Plot.Children.Add(item.Value);
                    }

                    if (ug_Plot.Children.Count < 3)
                    {
                        ug_Plot.Rows = 1;
                    }
                    else if (ug_Plot.Children.Count < 8)
                    {
                        ug_Plot.Rows = 2;
                    }
                    else
                    {
                        ug_Plot.Rows = 0;
                    }
                }
            }
        }

        public Ctrl_Slot(TF_Result rs) : this()
        {
            rs.TestEnd += Rs_TestEnd;
        }

        bool forbidstepdatachange;
        private void Rs_TestEnd(object sender, EventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                forbidstepdatachange = true;
                tv_Result.Items.Refresh();  // this will trig ActiveStepDataChanged
                forbidstepdatachange = false;
            });
        }

        private void ActiveStepDataChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (forbidstepdatachange) return;
            if (sender is TreeView tv && DataContext is VM_Slot vm)
            {
                if (tv.SelectedItem is Nest<TF_StepData> data)
                {
                    vm.StepData = data;
                }
            }
        }

        private void SlotName_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is VM_Slot vm)
            {
                if (vm.IsEnable)
                {
                    switch (vm.Result.Status)
                    {
                        case TF_TestStatus.PASSED:
                        case TF_TestStatus.FAILED:
                        case TF_TestStatus.IDLE:
                        case TF_TestStatus.NULL:
                        case TF_TestStatus.TERMINATED:
                        case TF_TestStatus.ABORT:
                        case TF_TestStatus.ERROR:
                            if(DialogCoordinator.Instance.ShowModalMessageExternal(DataContext, "Warning", "You are Trying to disable/enable the slot, Are you sure", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                            {
                                vm.IsEnable = !vm.IsEnable;
                                vm.Result.Status = TF_TestStatus.DISABLED;
                                //vm.TestStatus = "Disabled";
                            }
                            break;
                        default:
                            DialogCoordinator.Instance.ShowModalMessageExternal(this.DataContext, "Error", $"Current Status {vm.Result.Status}, Could not disable Slot", MessageDialogStyle.Affirmative);
                            break;
                    }
                }
                else
                {
                    vm.IsEnable = !vm.IsEnable;
                    //vm.TestStatus = vm.Result.Status.ToString();
                    vm.Result.Status = TF_TestStatus.IDLE;
                }

                if (sender is Control ctrl)
                {
                    ctrl.Background = vm.IsEnable ? Brushes.Green : Brushes.Gray;
                }
            }
        }

        private void SlotTestCount_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is VM_Slot vm)
            {
                try
                {
                    if (DialogCoordinator.Instance.ShowModalInputExternal(this.DataContext, "Warning", "You are trying to RESET the test count, Please Input the PASSWORD?") == "tympte")
                    {
                        EngineUtilities.ClearTestCount(vm.Result.StationConfig, vm.Result.SocketIndex);
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void SwitchMesStatus_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is VM_Slot vm)
            {
                vm.SwitchMesStatus.Execute(null);
            }
        }

        private void mi_PinTestItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is VM_Slot vm && sender is Control ctrl)
            {
                if (ctrl.DataContext is Nest<TF_StepData> item)
                {
                    if(vm.ItemPlotMap.ContainsKey(item))
                    {
                        vm.UnregisterItemPlot(item);
                        ug_Plot.Children.Clear();

                        ug_Plot.Children.Add(wpfplot);
                        foreach (var sub in vm.ItemPlotMap)
                        {
                            ug_Plot.Children.Add(sub.Value);
                        }
                    }
                    else
                    {
                        WpfPlot plot = new WpfPlot() { Margin= new Thickness(1) };

                        vm.RegisterItemPlot(item, plot);
                        ug_Plot.Children.Add(plot);
                    }

                    if (ug_Plot.Children.Count < 3)
                    {
                        ug_Plot.Rows = 1;
                    }
                    else if (ug_Plot.Children.Count < 8)
                    {
                        ug_Plot.Rows = 2;
                    }
                    else
                    {
                        ug_Plot.Rows = 0;
                    }
                }
            }
        }

        private void mi_ResetPinList_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is VM_Slot vm && sender is Control ctrl)
            {
                //if (ctrl.DataContext is Nest<TF_StepData> item)
                //{
                //    if (vm.ItemPlotMap.ContainsKey(item))
                //    {
                //        vm.UnregisterItemPlot(item);

                //        ug_Plot.Children.Clear();

                //        ug_Plot.Children.Add(wpfplot);
                //        foreach(var sub in vm.ItemPlotMap)
                //        {
                //            ug_Plot.Children.Add(sub.Value);
                //        }
                //    }
                //}

                ug_Plot.Children.Clear();

                ug_Plot.Children.Add(wpfplot);
                vm.ItemPlotMap.Clear();
                ug_Plot.Rows = 0;
                ug_Plot.Columns = 0;
            }
        }

        private void mi_SavePinSetItem_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is VM_Slot vm)
            {
                if(vm.ItemPlotMap.Count <= 0) return;

                vm.SaveUiConfig.Execute(null);

                //var filename = $"{vm.Result.StationConfig.CustomerName}_{vm.Result.StationConfig.ProductName}_{vm.Result.StationConfig.StationName}.suc";
                //var path = System.IO.Path.Combine(ServiceStatic.RootDataDir, filename);
                //try
                //{
                //    SlotUiConfig slotuiconfig = new SlotUiConfig();
                //    slotuiconfig.Pins = new ChartItemConfig[vm.ItemPlotMap.Count];
                //    for (int i = 0; i < vm.ItemPlotMap.Count; i++)
                //    {
                //        slotuiconfig.Pins[i] = new ChartItemConfig() { Path = string.Join(".", vm.ItemPlotMap.Keys.ElementAt(i).GetPath().Select(x => x.Name).Reverse()) };
                //    }

                //    XmlSerializer xml = new XmlSerializer(typeof(SlotUiConfig));

                //    using (TextWriter tw = new StreamWriter(path))
                //    {
                //        xml.Serialize(tw, slotuiconfig);
                //    }

                //    using (TextReader tw = new StreamReader(path))
                //    {
                //        slotuiconfig = xml.Deserialize(tw) as SlotUiConfig;
                //    }
                //}
                //catch
                //{ }
            }
        }
    }

    public class ComparibleDataTemplateSelector : System.Windows.Controls.DataTemplateSelector
    {
        public DataTemplate TemplateDefault { get; set; }
        public DataTemplate TemplateNumber { get; set; }
        public DataTemplate TemplateString { get; set; }
        public DataTemplate TemplateCurve { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is TF_ItemData itemdata)
            {
                var limit = itemdata.Limit;
                if (limit.LimitType == typeof(string))
                {
                    return TemplateString;
                }
                else if (limit.LimitType == typeof(TF_Curve))
                {
                    return TemplateCurve;
                }
                else
                {
                    return TemplateNumber;
                }
            }

            return TemplateDefault;
        }
    }
}
