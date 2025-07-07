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

using MahApps;
using MahApps.Metro.Controls;

using ToucanCore.Engine;
using ApEngine;
using TestCore;
using Microsoft.Win32;
using System.IO;
using TestCore.Data;
using Robin;
using System.ComponentModel;
using ScottPlot;
using System.Data;
using ScottPlot.Plottable;
using System.Windows.Controls.Primitives;
using ControlzEx.Theming;
using Xceed.Wpf.AvalonDock.Themes;
using System.Reflection;
using MahApps.Metro.Controls.Dialogs;
using System.Web.UI;
using System.Runtime.Remoting.Channels;

namespace Robin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public bool EnableShowChannels
        {
            get { return (bool)GetValue(EnableShowChannelsProperty); }
            set { SetValue(EnableShowChannelsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableShowChannels.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableShowChannelsProperty =
            DependencyProperty.Register("EnableShowChannels", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        private VM_Robin robin = new VM_Robin(DialogCoordinator.Instance);

        public MainWindow()
        {
            InitializeComponent();

            DataContext = robin;
            robin.ActivePlot = wpfplot;
            robin.ActivePlot_Attach = wpfplot_attach;

            robin.TestResultUpdated += Robin_TestResultUpdated;

            robin.AuxInRefreshed += AuxInRefreshed;

            Application.Current.Exit += Current_Exit;
            
            foreach(var input in App.HardwareDefinition.InputNames)
            {
                StatusBarItem sbi = new StatusBarItem();
                sbi.BorderThickness = new Thickness(1);
                sbi.Content = input.Key;
                sbi.DataContext = input.Value;
                sbi.ToolTip = $"Aux {input.Value}. Index Start From 0";
                sp_AuxInIndicators.Children.Add(sbi);
            }

            //InitChartContextMenu(); 
            if(!IsActive) this.Activate();
        }

        private void InitChartContextMenu()
        { 
            if(wpfplot.ContextMenu != null)   // this is null
            {
                wpfplot.ContextMenu.Items.Add(new Separator());

                var mi = new MenuItem() { Header = "Export Data As CSV", IsEnabled = false };
                mi.Click += (sender, e) => { };  // TODO
                wpfplot.ContextMenu.Items.Add(mi);
            }

            if (wpfplot_attach.ContextMenu != null)
            {
                wpfplot_attach.ContextMenu.Items.Add(new Separator());

                var mi = new MenuItem() { Header = "Export Data As CSV", IsEnabled = false };
                mi.Click += (sender, e) => { };  // TODO
                wpfplot_attach.ContextMenu.Items.Add(mi);
            }
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            robin.Exit.Execute(null);
        }

        private void Robin_TestResultUpdated(object sender, EventArgs e)
        {
            tv_spec.Items?.Refresh();
            if(sender is VM_Robin vm)
            {
                if (vm.Result is null) return;
                tb_Conclusion.Text = $"{vm.Result.Status}";

                int i = 0;
                for (; i < vm.Sequences.Length; i++)
                {
                    if(vm.ActiveSequence == vm.Sequences[i])
                    {
                        break;
                    }
                }

                switch(vm.Result.Status)
                {
                    case TF_TestStatus.PASSED:
                        tb_Conclusion.Background = Brushes.LimeGreen;
                        break;
                    case TF_TestStatus.FAILED:
                        tb_Conclusion.Background = Brushes.Red;
                        break;
                    case TF_TestStatus.ERROR:
                        tb_Conclusion.Background = Brushes.Yellow;
                        break;
                    case TF_TestStatus.TESTING:
                        tb_Conclusion.Background = Brushes.Gray;
                        break;
                }
            }

        }

        private void AuxInRefreshed(object sender, byte e)
        {
            foreach(var ctrl in sp_AuxInIndicators.Children)
            {
                if(ctrl is StatusBarItem sbi)
                {
                    if(sbi.DataContext is byte byteval)
                    {
                        var bit = (e & (2 << byteval)) > 0;
                        if(bit)
                        {
                            sbi.Background = Brushes.Green;
                        }
                        else
                        {
                            sbi.Background = Brushes.Gray;
                        }
                    }
                }
            }
        }

        private void MI_Exit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void ActiveStepDataChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView tv)
            {
                if (tv.SelectedItem is Nest<TF_StepData> data)
                {
                    if(robin.Result.StepDatas.FirstOrDefault() != data)
                    {
                        robin.StepData = data;
                    }
                }
            }
        }

        private void MI_ApVisibleClick(object sender, RoutedEventArgs e)
        {
            ApxEngine.ApVisible = !ApxEngine.ApVisible;
        }

        private void OpenTemplate_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender is TreeViewItem tvi)
            {
                if (tvi.Header is ObservedNest<SampleFileName> sfn)
                {
                    if (File.Exists(sfn.Element.Path))
                    {
                        robin.OpenScript.Execute(sfn.Element.Path);
                    }
                }
            }
        }

        private void AuxOut_SetTrue(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn)
            {
                if(btn.DataContext is AuxOut ao)
                {
                    byte mask = (byte)(1 << ao.Index);
                    var d = ApxEngine.ApRef.AuxControlMonitor.AuxControlOutputValue;

                    byte newval = (byte)(mask | d);
                    if (newval != d)
                    {
                        ApxEngine.ApRef.AuxControlMonitor.AuxControlOutputValue = newval;
                    }
                }
            }
        }

        private void AuxOut_SetFalse(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn.DataContext is AuxOut ao)
                {
                    byte mask =(byte)~(1 << ao.Index);
                    var d = ApxEngine.ApRef.AuxControlMonitor.AuxControlOutputValue;

                    byte newval = (byte)(mask & d);
                    if (newval != d)
                    {
                        ApxEngine.ApRef.AuxControlMonitor.AuxControlOutputValue = newval;
                    }
                }
            }
        }

        private void MI_DockWorkbase_Click(object sender, RoutedEventArgs e)
        {
            la_Workbase.Show();
        }

        private void MI_DockTemplate_Click(object sender, RoutedEventArgs e)
        {
            la_Template.Show();
        }

        private void MI_DockVariable_Click(object sender, RoutedEventArgs e)
        {
            la_Variables.Show();
        }

        private void MI_DockConclusion_Click(object sender, RoutedEventArgs e)
        {
            la_Conclusion.Show();
        }
        private void MI_LockClick(object sender, RoutedEventArgs e)
        {
            robin.IsEditMode = !robin.IsEditMode;
        }

        private void cm_tv_spec_UpdateExpender(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi)
            {
                if (mi.Tag is string idx)
                {
                    var d = tv_spec.FindChildren<TreeViewItem>();

                    switch (idx)
                    {
                        case "1":
                            
                            foreach (TreeViewItem item in tv_spec.Items)
                            {
                                item.IsExpanded = true;
                                foreach(TreeViewItem sub in item.Items)
                                {
                                    item.IsExpanded = false;
                                }
                            }

                            break;

                        case "2":
                            foreach (TreeViewItem item in tv_spec.Items)
                            {
                                item.IsExpanded = true;
                                foreach (TreeViewItem sub in item.Items)
                                {
                                    item.IsExpanded = true;
                                    foreach (TreeViewItem sub1 in sub.Items)
                                    {
                                        sub1.IsExpanded = false;
                                    }
                                }
                            }

                            break;
                        case "3":
                            foreach (TreeViewItem item in tv_spec.Items)
                            {
                                item.IsExpanded = true;
                                foreach (TreeViewItem sub in item.Items)
                                {
                                    item.IsExpanded = true;
                                    foreach (TreeViewItem sub1 in sub.Items)
                                    {
                                        sub1.IsExpanded = true;
                                        foreach (TreeViewItem sub2 in sub.Items)
                                        {
                                            sub2.IsExpanded = false;
                                        }
                                    }
                                }
                            }

                            break;
                    }
                }
            }
        }

        private void ClearAttachResult_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Are you sure to clear all appended results.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                foreach (var dict in robin.AttachResults)
                {
                    dict.Value.Clear();
                }

                robin.ActivePlot_Attach.Plot.Clear();
                robin.ActivePlot_Attach.Refresh();
            }
        }

        private void ShowReportDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.GroupSetting.Variables.ContainsKey("DataFolder"))
                {
                    if (!Directory.Exists(App.GroupSetting.Variables["DataFolder"]))
                    {
                        Directory.CreateDirectory(App.GroupSetting.Variables["DataFolder"]);
                    }

                    using (System.Diagnostics.Process ps = new System.Diagnostics.Process())
                    {
                        ps.StartInfo.FileName = "explorer.exe";
                        ps.StartInfo.Arguments = $"\"{App.GroupSetting.Variables["DataFolder"]}\"";

                        ps.Start();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Show Report Dir Failed. Err: {ex}", "Error");
            }
        }

        private void ShowTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToucanCore.UIs.FileBrowser fb = new ToucanCore.UIs.FileBrowser(new ToucanCore.UIs.ProjectDirectory(App.TemplateDir));
                fb.Topmost = true;
                fb.Title = "Template";
                fb.ShowDialog();

                robin.Templates.Clear();
                robin.ListFiles(robin.Templates, App.TemplateDir, "*.approjx");   // ObservableNest does not work
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Show Template Dir Failed. Err: {ex}", "Error");
            }
        }
    }
}
