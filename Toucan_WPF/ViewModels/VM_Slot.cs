using MahApps.Metro.Controls.Dialogs;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using Toucan_WPF.Ctrls;
using ToucanCore.Abstraction.Engine;
using ToucanCore.Engine;
using ToucanCore.Abstraction.HAL;
using ToucanCore.Misc;

namespace Toucan_WPF.ViewModels
{
    public class VM_Slot : DependencyObject
    {
        public ScottPlot.WpfPlot ActivePlot { get; set; }
        public ScottPlot.WpfPlot FailurePlot { get; set; }

        public static bool ShowRecordList { get; set; }
        public static bool ShowRecordChart { get; set; }
        public static bool ShowFailureChart { get; set; }

        public int SlotIndex
        {
            get { return (int)GetValue(SlotIndexProperty); }
            set { SetValue(SlotIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlotIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotIndexProperty =
            DependencyProperty.Register("SlotIndex", typeof(int), typeof(VM_Slot), new PropertyMetadata(0));

        public string SlotName
        {
            get { return (string)GetValue(SlotNameProperty); }
            set { SetValue(SlotNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlotName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotNameProperty =
            DependencyProperty.Register("SlotName", typeof(string), typeof(VM_Slot), new PropertyMetadata($"Slot 0"));

        public string SerialNumber
        {
            get { return (string)GetValue(SerialNumberProperty); }
            set { SetValue(SerialNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNumberProperty =
            DependencyProperty.Register("SerialNumber", typeof(string), typeof(VM_Slot), new PropertyMetadata(null, SerialNumberChanged));

        private static void SerialNumberChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Slot vm)
            {
                vm.SnBorder = 1;
            }
        }

        public string SerialNumberNext
        {
            get { return (string)GetValue(SerialNumberNextProperty); }
            set { SetValue(SerialNumberNextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNumberNext.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNumberNextProperty =
            DependencyProperty.Register("SerialNumberNext", typeof(string), typeof(VM_Slot), new PropertyMetadata(null));

        public int SnBorder
        {
            get { return (int)GetValue(SnBorderProperty); }
            set { SetValue(SnBorderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SnBorder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SnBorderProperty =
            DependencyProperty.Register("SnBorder", typeof(int), typeof(VM_Slot), new PropertyMetadata(0));

        public MesStatus MesStatus
        {
            get { return (MesStatus)GetValue(MesStatusProperty); }
            set { SetValue(MesStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MesStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MesStatusProperty =
            DependencyProperty.Register("MesStatus", typeof(MesStatus), typeof(VM_Slot), new PropertyMetadata(MesStatus.Disable));

        public string TestStatus
        {
            get { return (string)GetValue(TestStatusProperty); }
            set { SetValue(TestStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TestStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TestStatusProperty =
            DependencyProperty.Register("TestStatus", typeof(string), typeof(VM_Slot), new PropertyMetadata(TF_TestStatus.NULL.ToString()));

        public string TestProgress
        {
            get { return (string)GetValue(TestProgressProperty); }
            set { SetValue(TestProgressProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TestProgress.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TestProgressProperty =
            DependencyProperty.Register("TestProgress", typeof(string), typeof(VM_Slot), new PropertyMetadata(string.Empty));

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Background.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundProperty =
            DependencyProperty.Register("Background", typeof(Brush), typeof(VM_Slot), new PropertyMetadata(SystemColors.ControlBrush));


        public int STS_Pass
        {
            get { return (int)GetValue(STS_PassProperty); }
            set { SetValue(STS_PassProperty, value); }
        }

        // Using a DependencyProperty as the backing store for STS_Pass.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty STS_PassProperty =
            DependencyProperty.Register("STS_Pass", typeof(int), typeof(VM_Slot), new PropertyMetadata(0));

        public int STS_Fail
        {
            get { return (int)GetValue(STS_FailProperty); }
            set { SetValue(STS_FailProperty, value); }
        }

        // Using a DependencyProperty as the backing store for STS_Fail.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty STS_FailProperty =
            DependencyProperty.Register("STS_Fail", typeof(int), typeof(VM_Slot), new PropertyMetadata(0));

        public int STS_All
        {
            get { return (int)GetValue(STS_AllProperty); }
            set { SetValue(STS_AllProperty, value); }
        }

        // Using a DependencyProperty as the backing store for STS_All.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty STS_AllProperty =
            DependencyProperty.Register("STS_All", typeof(int), typeof(VM_Slot), new PropertyMetadata(0));

        public string Yield
        {
            get { return (string)GetValue(YieldProperty); }
            set { SetValue(YieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Yield.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YieldProperty =
            DependencyProperty.Register("Yield", typeof(string), typeof(VM_Slot), new PropertyMetadata("0.00"));

        public int TRACE_SlotTestCount
        {
            get { return (int)GetValue(TRACE_SlotTestCountProperty); }
            set { SetValue(TRACE_SlotTestCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TRACE_SlotTestCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TRACE_SlotTestCountProperty =
            DependencyProperty.Register("TRACE_SlotTestCount", typeof(int), typeof(VM_Slot), new PropertyMetadata(0));

        public string ElapsedTime
        {
            get { return (string)GetValue(ElapsedTimeProperty); }
            set { SetValue(ElapsedTimeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ElapsedTime.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ElapsedTimeProperty =
            DependencyProperty.Register("ElapsedTime", typeof(string), typeof(VM_Slot), new PropertyMetadata("0.00"));

        public bool IsEnable
        {
            get { return (bool)GetValue(IsEnableProperty); }
            set { SetValue(IsEnableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEnable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEnableProperty =
            DependencyProperty.Register("IsEnable", typeof(bool), typeof(VM_Slot), new PropertyMetadata(true, IsEnableChanged));

        public Visibility VisibilityRecordList
        {
            get { return (Visibility)GetValue(VisibilityRecordListProperty); }
            set { SetValue(VisibilityRecordListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VisibilityRecordList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VisibilityRecordListProperty =
            DependencyProperty.Register("VisibilityRecordList", typeof(Visibility), typeof(VM_Slot), new PropertyMetadata(Visibility.Collapsed));

        public Visibility VisibilityRecordChart
        {
            get { return (Visibility)GetValue(VisibilityRecordChartProperty); }
            set { SetValue(VisibilityRecordChartProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VisibilityRecordChart.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VisibilityRecordChartProperty =
            DependencyProperty.Register("VisibilityRecordChart", typeof(Visibility), typeof(VM_Slot), new PropertyMetadata(Visibility.Collapsed));


        private static void IsEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Slot vm)
            {
                if (vm.Execution is IExecution)
                {
                    var b = (bool)e.NewValue;
                    try
                    {
                        vm.Execution.EnableSlot(vm.SlotIndex, b);
                    }
                    catch (NotImplementedException nie)
                    {
                        vm.Dispatcher.Invoke(() =>
                        {
                            DialogCoordinator.Instance.ShowModalMessageExternal(vm, "Warn", nie.Message);
                        }
                            );
                    }
                }
            }
        }

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsActive.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(VM_Slot), new PropertyMetadata(false));

        /// <summary>
        /// Parent Execution, which host the slot
        /// </summary>
        public IExecution Execution { get; set; }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(VM_Slot), new PropertyMetadata(null));

        private List<TF_Result> RegisterResults = new List<TF_Result>();

        public TF_Result Result
        {
            get { return (TF_Result)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Result.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(TF_Result), typeof(VM_Slot), new PropertyMetadata(null, ResultChanged));

        private static void ResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Slot self && e.NewValue is TF_Result rs)
            {
                self.AttachResults.Clear();
                if (!self.RegisterResults.Contains(rs))
                {
                    rs.TestStart += self.Result_TestStart;
                    rs.TestEnd += self.Result_TestEnd;
                    rs.TestStatusChanged += self.Result_StatusChanged;

                    for (int i = 0; i < rs.StepDatas.Count; i++)
                    {
                        RefreshStepDataIndex(rs.StepDatas[i], (i + 1).ToString());
                    }

                    self.RegisterResults.Add(rs);
                }

                if (!rs.SFCsConfig.EnableSfc)
                {
                    self.MesStatus = MesStatus.Disable;
                }
                else if (string.IsNullOrEmpty(StationConfig.IpAddress))
                {
                    rs.IsSFC = false;
                    self.MesStatus = MesStatus.Offline;
                }
                else if (rs.IsSFC)
                {
                    self.MesStatus = MesStatus.SFCsOn;
                }
                else
                {
                    self.MesStatus = MesStatus.SFCsOff;
                }

                self.TestStatus = rs.Status.ToString();
            }
        }

        public Nest<TF_StepData> StepData
        {
            get { return (Nest<TF_StepData>)GetValue(StepDataProperty); }
            set { SetValue(StepDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StepData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StepDataProperty =
            DependencyProperty.Register("StepData", typeof(Nest<TF_StepData>), typeof(VM_Slot), new PropertyMetadata(null, StepDataChanged));

        private static void StepDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Slot self)
            {
                if (e.NewValue is Nest<TF_StepData> data)
                {
                    if (ShowRecordChart)
                    {
                        if(VM_Slot.AttachResultInChart)
                        {
                            RefreshAttachData(self, self.ActivePlot, data);
                        }
                        else
                        {
                            RefreshData(self, self.ActivePlot, data);
                        }
                    }
                }
            }
        }

        public ObservableCollection<TF_StepData> FlatStepData
        {
            get { return (ObservableCollection<TF_StepData>)GetValue(FlatStepDataProperty); }
            set { SetValue(FlatStepDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FlatStepData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FlatStepDataProperty =
            DependencyProperty.Register("FlatStepData", typeof(ObservableCollection<TF_StepData>), typeof(VM_Slot), new PropertyMetadata(null));


        public Dictionary<Nest<TF_StepData>, WpfPlot> ItemPlotMap { get; } = new Dictionary<Nest<TF_StepData>, WpfPlot>();

        public DelegateCommand SwitchMesStatus { get; }
        public DelegateCommand Reset { get; }
        public DelegateCommand Start { get; }
        public DelegateCommand Stop { get; }

        public DelegateCommand ShowChartInNewWindow { get; }
        public DelegateCommand ShowRecordInNewWindow { get; }

        public DelegateCommand SaveUiConfig { get; }

        public DelegateCommand ExportResultAsList { get; }
        //public event EventHandler<DutMessage> SlotReset;

        private System.Timers.Timer TimeoutTimer { get; }
        private System.Timers.Timer StatusTimer { get; }

        public static bool AttachResultInChart { get; set; }

        private VM_Slot()
        {
            SwitchMesStatus = new DelegateCommand(cmd_SwitchMesStatus);
            Reset = new DelegateCommand(cmd_Reset);
            Start = new DelegateCommand(cmd_Start);
            Stop = new DelegateCommand(cmd_Stop);

            ShowChartInNewWindow = new DelegateCommand(cmd_ShowChartInNewWindow);
            ShowRecordInNewWindow = new DelegateCommand(cmd_ShowRecordInNewWindow);

            ExportResultAsList = new DelegateCommand(cmd_ExportResultAsList);

            FlatStepData = new ObservableCollection<TF_StepData>();

            VisibilityRecordList = ShowRecordList ? Visibility.Visible : Visibility.Collapsed;
            VisibilityRecordChart = ShowRecordChart ? Visibility.Visible : Visibility.Collapsed;

            StatusTimer = new System.Timers.Timer(200);
            StatusTimer.AutoReset = true;
            StatusTimer.Elapsed += (sender, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var elapsed = DateTime.Now.Subtract(Result.StartTime).TotalSeconds;
                    ElapsedTime = elapsed.ToString("F2");

                    if (Result.Status == TF_TestStatus.TESTING)
                    {
                        TestProgress = ProgressString[ProgressCount % ProgressString.Length];
                        ProgressCount++;
                    }
                });
                
            };

            SaveUiConfig = new DelegateCommand(cmd_SaveUiConfig);
        }

        private void cmd_SaveUiConfig(object obj)
        {
            var filename = $"{Result.StationConfig.CustomerName}_{Result.StationConfig.ProductName}_{Result.StationConfig.StationName}.suc";
            var path = System.IO.Path.Combine(ServiceStatic.RootDataDir, filename);

            SlotUiConfig slotuiconfig = new SlotUiConfig();
            slotuiconfig.Pins = new ChartItemConfig[ItemPlotMap.Count];
            for (int i = 0; i < ItemPlotMap.Count; i++)
            {
                slotuiconfig.Pins[i] = new ChartItemConfig() { Path = string.Join(".", ItemPlotMap.Keys.ElementAt(i).GetPath().Select(x => x.Name).ToArray()) };
            }

            XmlSerializer xml = new XmlSerializer(typeof(SlotUiConfig));

            using (TextWriter tw = new StreamWriter(path))
            {
                xml.Serialize(tw, slotuiconfig);
            }
        }

        int ProgressCount;

        public VM_Slot(TF_Result rs, SlotUiConfig uiconfig = null) : this()
        {
            Result = rs;

            if (rs.GeneralConfig.TestTimeout > 0)
            {
                TimeoutTimer = new System.Timers.Timer(rs.GeneralConfig.TestTimeout * 1e3);
                TimeoutTimer.AutoReset = true;
                TimeoutTimer.Elapsed += (sender, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        TestStatus = "Timeout";
                    });
                };
            }

            if(uiconfig != null)
            {
                foreach(var pin in uiconfig.Pins) 
                {
                    var path = pin.Path?.Split('.')?.Reverse()?.Skip(1)?.ToArray();
                    if (path is null) continue;

                    if(Result.StepDatas.Fetch(path) is Nest<TF_StepData> item)
                    {
                        RegisterItemPlot(item, new WpfPlot() { BorderThickness = new Thickness() });
                    }
                }
            }

            TRACE_SlotTestCount = EngineUtilities.GetTestCount(Result.StationConfig, Result.SocketIndex);
        }

        private static void RefreshStepDataIndex(Nest<TF_StepData> datas, string index)
        {
            datas.Element.TempIndex = index;

            for (int i = 0; i < datas.Count; i++)
            {
                RefreshStepDataIndex(datas[i], $"{index}.{i+1}");
            }
        }

        private void cmd_SwitchMesStatus(object obj)
        {
            if(Result.SFCsConfig.Lock)
            {
                Message = "SFCs Locked";
                return;
            }

            if (Result.Status == TF_TestStatus.IDLE || Result.Status == TF_TestStatus.PASSED || Result.Status == TF_TestStatus.FAILED || Result.Status == TF_TestStatus.ERROR || Result.Status == TF_TestStatus.WAIVE)
            {
                if (MesStatus == MesStatus.Offline)
                {
                    StationConfig.CheckNetwork();
                    if (string.IsNullOrEmpty(StationConfig.IpAddress))
                    {
                        var mes = Mes.MesManager.GetMesInstance(Result.StationConfig.Location, Result.StationConfig.Vendor);
                        Result.IsSFC = true;
                        MesStatus = MesStatus.SFCsOn;
                    }
                }
                else if (Result.SFCsConfig.EnableSfc && !Result.SFCsConfig.Lock)
                {
                    if (Result.IsSFC)
                    {
                        if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Your are trying turn SFCs off, Are you sure?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { DefaultButtonFocus = MessageDialogResult.Negative }) == MessageDialogResult.Negative)
                        {
                            return;
                        }
                    }

                    Result.IsSFC = !Result.IsSFC;
                    MesStatus = Result.IsSFC ? MesStatus.SFCsOn : MesStatus.SFCsOff;
                }
            }
            else
            {
                MessageBox.Show($"Invalid Operation When {Result.Status}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cmd_Reset(object obj)
        {
            if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning. HIGH RISK OPERATION", $"You are trying to RESET the Status for Slot {SlotIndex}, Are you sure. DO CARE CHECKING if the hardware problem had been resolved and the dead lock had been released",  MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { DefaultButtonFocus = MessageDialogResult.Negative}) == MessageDialogResult.Affirmative)
            {
                Execution.Start();
                Result.Status = TF_TestStatus.IDLE;
            }
        }

        private void cmd_Start(object obj)
        {
            TestStatus = "START";
            IsEnable = true;
        }

        private void cmd_Stop(object obj)
        {
            TestStatus = "STOPPED";
            IsEnable = false;
        }

        public List<TF_Result> AttachResults = new List<TF_Result>();

        private void Result_TestEnd(object sender, EventArgs args)
        {
            // the event might from another thread
            Dispatcher.Invoke(() => 
            {
                VisibilityRecordList = ShowRecordList ? Visibility.Visible : Visibility.Collapsed;
                VisibilityRecordChart = ShowRecordChart ? Visibility.Visible : Visibility.Collapsed;

                if (ShowRecordChart)
                {
                    if (AttachResultInChart)
                    {
                        AttachResults.Add(Result.Clone() as TF_Result);
                        RefreshAttachData(this, ActivePlot, StepData);

                        foreach (var item in ItemPlotMap)
                        {
                            RefreshAttachData(this, item.Value, item.Key);
                        }
                    }
                    else
                    {
                        RefreshData(this, ActivePlot, StepData);

                        foreach (var item in ItemPlotMap)
                        {
                            RefreshData(this, item.Value, item.Key);
                        }
                    }
                }

                if (ShowRecordList)
                {
                    FlatStepData.Clear();
                    var d = Result.StepDatas.ToFlatList();
                    for (int i = 1; i < d.Count; i++)
                    {
                        FlatStepData.Add(d[i].Element);
                    }
                }

                StatusTimer?.Stop();
                TestProgress = string.Empty;
                TimeoutTimer?.Stop();
                var rs = sender as TF_Result;
                switch (rs.Status)
                {
                    case TF_TestStatus.WAIVE:
                        Background = Brushes.LimeGreen;
                        STS_Pass += 1;
                        STS_All += 1;

                        TestStatus = Result.Specification.Secondary is null ? rs.Status.ToString() : $"{rs.Status} -> {rs.Grade}";

                        Message = $"{rs.SerialNumber} WAIVE. Grade {rs.Grade}";
                        if (ShowFailureChart)
                        {
                            if (GetFirstDefectItems(rs.StepDatas) is Nest<TF_StepData> defectitem)
                            {
                                FailurePlot.Visibility = Visibility.Visible;
                                RefreshData(this, FailurePlot, defectitem);
                            }
                        }
                        break;
                    case TF_TestStatus.PASSED:
                        Background = Brushes.LimeGreen;
                        STS_Pass += 1;
                        STS_All += 1;

                        TestStatus = Result.Specification.Secondary is null ? rs.Status.ToString() : $"{rs.Status} -> {rs.Grade}";
                        break;
                    case TF_TestStatus.FAILED:

                        Background = Brushes.Red;


                        STS_Fail += 1;
                        STS_All += 1;
                        TestStatus = rs.Status.ToString();

                        if (GetFirstDefectItems(rs.StepDatas) is Nest<TF_StepData> defectitem_1)
                        {
                            if (ShowFailureChart)
                            {
                                FailurePlot.Visibility = Visibility.Visible;
                                RefreshData(this, FailurePlot, defectitem_1);
                            }

                            if (defectitem_1.Element is TF_ItemData item)
                            {
                                if (item.Value is TF_Curve)
                                {
                                    Message = string.Join("; ", rs.Defect.Select(x => $"{x.Code}: {x.Desc}-> Curve Data"));
                                }
                                else
                                {
                                    Message = string.Join("; ", rs.Defect.Select(x => $"{x.Code}: {x.Desc}-> {x.Value}"));
                                }
                            }
                        }

                        break;
                    case TF_TestStatus.TERMINATED:
                        Background = Brushes.Purple;
                        STS_All += 1;
                        TestStatus = rs.Status.ToString();
                        break;
                    case TF_TestStatus.ERROR:
                        Background = Brushes.Yellow;
                        Message = ((TF_Result)sender).ErrorMessage.Info;
                        STS_All += 1;
                        TestStatus = rs.Status.ToString();
                        break;
                    case TF_TestStatus.ABORT:
                        Background = Brushes.Gray;
                        Message = ((TF_Result)sender).ErrorMessage.ToString();
                        STS_All += 1;
                        TestStatus = rs.Status.ToString();
                        break;

                    default:
                        TestStatus = rs.Status.ToString();
                        Background = SystemColors.ControlBrush;
                        break;
                }
            }
            );
        }

        //bool IsMesEditable_Preview = true;

        private void Result_TestStart(object sender, EventArgs args)
        {
            Dispatcher.Invoke(() =>
                {
                    if(sender is TF_Result rs)
                    {
                        SerialNumber = rs.SerialNumber;

                        TestStatus = rs.Status.ToString();
                        SnBorder = 0;
                        Message = string.Empty;
                        Background = SystemColors.ControlBrush;
                        TRACE_SlotTestCount++;
                        EngineUtilities.AddTestCount(Result.StationConfig, Result.SocketIndex);
                        //_UpdateUiWhenTesting.BeginInvoke(this, null, null);
                        ProgressCount = 0;
                        StatusTimer?.Start();
                        TimeoutTimer?.Start();
                    }
                    //IsMesEditable_Preview = IsMesEditable;
                    //IsMesEditable = false;

                    if(ShowFailureChart) FailurePlot.Visibility = Visibility.Collapsed;


                }
            );
        }

        private void Result_StatusChanged(object sender, TF_TestStatus status) 
        {
            Dispatcher.Invoke(() =>
            {
                switch (status)
                {
                    case TF_TestStatus.IDLE:
                    case TF_TestStatus.TEST_INIT:
                    case TF_TestStatus.NULL:
                    case TF_TestStatus.WAIT_DUT:
                        TestStatus = status.ToString();

                        break;

                    case TF_TestStatus.DISABLED:
                        TestStatus = TF_TestStatus.DISABLED.ToString();
                        Background = Brushes.DarkGray;
                        break;
                }
            }
            );            
        }

        readonly static string[] ProgressString = new string[] { "", ".", ".~",".~^", ".~", "." }; 
        private Action<VM_Slot> _UpdateUiWhenTesting = (VM_Slot vm) =>
        {
            var dt = DateTime.Now;
            int i = 0;

            vm.Dispatcher.Invoke(() =>
            {
                while (vm.Result.Status == TF_TestStatus.TESTING)
                {
                    Thread.Sleep(200);
                    //vm.Dispatcher.Invoke(() =>
                    //{
                        var elapsed = DateTime.Now.Subtract(dt).TotalSeconds;
                        vm.ElapsedTime = elapsed.ToString("F2");
                        vm.TestProgress = ProgressString[i % ProgressString.Length];
                    //});
                
                    i++;
                }

            
                vm.TestProgress = string.Empty;
            });
        };

        const double LimitMargin = 0.1;
        public List<TF_ItemData> ItemContainsInfinity = new List<TF_ItemData>();
        public static void RefreshData(VM_Slot self, WpfPlot wpfplot, Nest<TF_StepData> data)
        {
            if (data is null) return;

            if (data.Element is TF_ItemData itemdata)
            {
                bool hasinfinity = self.ItemContainsInfinity.Contains(itemdata);

                try
                {
                    if (itemdata.Limit.Comp == Comparison.NULL)
                    {
                        if (data.Children?.FirstOrDefault()?.Element is TF_ItemData pitemdata)
                        {
                            if (pitemdata.Value is double)
                            {
                                wpfplot.Plot.Clear();

                                double[] value_d = new double[data.Count];
                                double[] hl = new double[data.Count];
                                double[] ll = new double[data.Count];
                                string[] labels = new string[data.Count];
                                double[] ys = new double[data.Count];

                                bool havelimit = false;
                                for (int i = 0; i < data.Count; i++)
                                {
                                    if (data[i].Element is TF_ItemData item)
                                    {
                                        if (item.Result == TF_ItemStatus.NotTested) continue;
                                        value_d[i] = (double)item.Value;

                                        if (item.Limit.USL is double hlv)
                                        {
                                            hl[i] = hlv - value_d[i];
                                            if (!double.IsNaN(hlv))
                                            {
                                                havelimit = true;
                                            }
                                        }
                                        else
                                        {
                                            hl[i] = double.NaN;
                                        }

                                        if (item.Limit.LSL is double llv)
                                        {
                                            ll[i] = value_d[i] - llv;
                                            if (!double.IsNaN(llv))
                                            {
                                                havelimit = true;
                                            }
                                        }
                                        else
                                        {
                                            ll[i] = double.NaN;
                                        }
                                    }

                                    labels[i] = data[i].Element.Name;
                                    ys[i] = i;
                                }

                                wpfplot.Plot.YTicks(labels);
                                wpfplot.Plot.YAxis.MinorLogScale(false);

                                var barplot = wpfplot.Plot.AddBar(value_d);

                                barplot.Orientation = ScottPlot.Orientation.Horizontal;
                                barplot.ShowValuesAboveBars = true;
                                barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });
                                barplot.FillColorNegative = System.Drawing.Color.Cyan;
                                if (havelimit)
                                {
                                    var ebplot = wpfplot.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                    ebplot.LineWidth = 1;
                                    ebplot.MarkerSize = 10;
                                }

                                wpfplot.Plot.XAxis.Label(pitemdata.Limit.Unit);
                                wpfplot.Plot.XAxis.TickLabelFormat((double a) => { return a.ToString(); });
                                wpfplot.Plot.XAxis.MinorLogScale(false);
                                wpfplot.Plot.YLabel(null);

                                wpfplot.Plot.Title($"{data.Element.Name}");
                                wpfplot.Plot.Legend(true);

                                wpfplot.Refresh();
                            }
                            else if (pitemdata.Value is TF_Curve cv_template)
                            {
                                wpfplot.Plot.Clear();
                                wpfplot.Plot.ResetLayout();
                                for (int i = 0; i < data.Count; i++)
                                {
                                    if (((TF_ItemData)data[i].Element).Value is TF_Curve cv)
                                    {
                                        if (cv.Length == 0) continue;

                                        var xs = cv.XLog ? cv.X.Select(x => Math.Log10(x)).ToArray() : cv.X;
                                        var ys = cv.YLog ? cv.Y.Select(x => Math.Log10(x)).ToArray() : cv.Y;

                                        if (hasinfinity)
                                        {
                                            ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                            xs = xs.Take(ys.Length).ToArray();
                                        }

                                        var plot = wpfplot.Plot.AddScatter(xs, ys, markerSize: 0);
                                        plot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;
                                        if (((TF_ItemData)data[i].Element)?.Limit?.Tag is TF_Limit template)
                                        {
                                            if (template.LSL is TF_Curve llcv)
                                            {
                                                if (llcv.Length > 0)
                                                {
                                                    var llplot = wpfplot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                                    llplot.Label = $"{data[i].Element.Name}_LL";
                                                }
                                            }

                                            if (template.USL is TF_Curve hlcv)
                                            {
                                                if (hlcv.Length > 0)
                                                {
                                                    var hlplot = wpfplot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                                    hlplot.Label = $"{data[i].Element.Name}_HL";
                                                }
                                            }
                                        }

                                        plot.Label = data[i].Element.Name;
                                    }
                                }

                                //wpfplot.Plot.XTicks();
                                if (cv_template.XLog)
                                {
                                    wpfplot.Plot.XAxis.TickLabelFormat(logTickLabels);
                                }
                                else
                                {
                                    wpfplot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                wpfplot.Plot.XAxis.MinorLogScale(cv_template.XLog);
                                wpfplot.Plot.XAxis.MinorGrid(true);

                                //wpfplot.Plot.YTicks();
                                if (cv_template.YLog)
                                {
                                    wpfplot.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                                }
                                else
                                {
                                    wpfplot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                wpfplot.Plot.YAxis.MinorLogScale(cv_template.YLog);
                                wpfplot.Plot.YAxis.MinorGrid(true);

                                wpfplot.Plot.YAxis.AutomaticTickPositions();

                                wpfplot.Plot.XAxis.Label(cv_template.X_Unit);
                                wpfplot.Plot.YAxis.Label(cv_template.Y_Unit);

                                wpfplot.Plot.Title(data.Element.Name);

                                var legend = wpfplot.Plot.Legend();
                                legend.FillColor = System.Drawing.Color.Transparent;

                                wpfplot.Refresh();
                            }
                        }
                    }
                    else
                    {
                        if (itemdata.Value is double vd)
                        {
                            wpfplot.Plot.Clear();

                            double[] value_d = new double[1] { vd };
                            double[] hl = new double[1];
                            double[] ll = new double[1];
                            double[] ys = new double[1] { 0 };
                            if (itemdata.Limit.USL is double hlv)
                            {
                                hl[0] = hlv - vd;
                            }
                            else
                            {
                                hl[0] = double.NaN;
                            }

                            if (itemdata.Limit.LSL is double llv)
                            {
                                ll[0] = vd - llv;
                            }
                            else
                            {
                                ll[0] = double.NaN;
                            }

                            var barplot = wpfplot.Plot.AddBar(new double[1] { vd });
                            barplot.Orientation = ScottPlot.Orientation.Horizontal;
                            barplot.Label = itemdata.Name;
                            barplot.ShowValuesAboveBars = true;
                            barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });
                            barplot.FillColorNegative = System.Drawing.Color.Cyan;
                            if (double.IsNaN(ll[0]) && double.IsNaN(hl[0]))
                            {
                            }
                            else
                            {
                                var ebplot = wpfplot.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                ebplot.LineWidth = 3;
                                ebplot.MarkerSize = 10;
                            }

                            wpfplot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            wpfplot.Plot.YTicks(new string[1] { itemdata.Name });
                            wpfplot.Plot.XLabel(itemdata.Limit?.Unit);
                            wpfplot.Plot.YLabel(null);
                            wpfplot.Plot.Title(data.Parent?.Element?.Name);
                            wpfplot.Plot.XAxis.MinorLogScale(false);
                            wpfplot.Plot.Legend(false);

                            wpfplot.Refresh();
                        }
                        else if (itemdata.Value is TF_Curve cv)
                        {
                            wpfplot.Plot.Clear();
                            //wpfplot.Plot.YTicks(new string[0]{ });  // this will make Ytick to be null

                            if (cv.Length == 0) return;
                            double[] xs = null;
                            double[] ys = null;

                            //double[] hls = null;
                            //double[] lls = null;

                            if (cv.XLog)
                            {
                                xs = cv.X.Select(x => Math.Log10(x)).ToArray();
                            }
                            else
                            {
                                xs = cv.X;
                            }

                            if (cv.YLog)
                            {
                                ys = cv.Y.Select(x => Math.Log10(x)).ToArray();
                            }
                            else
                            {
                                ys = cv.Y;
                            }

                            if (hasinfinity)
                            {
                                ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                xs = xs.Take(ys.Length).ToArray();
                            }

                            var plot = wpfplot.Plot.AddScatter(xs, ys, markerSize: 0);  // Ys might contains Infinity or NaN
                            plot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;  // need update library. .netfx 4.6.2 required
                            plot.Label = itemdata.Name;

                            TF_Curve tllcv = null;
                            TF_Curve thlcv = null;
                            if (itemdata.Limit.Tag is TF_Limit template)
                            {
                                if (template.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        tllcv = llcv;
                                        var llplot = wpfplot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                    }
                                }

                                if (template.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = wpfplot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                        thlcv = hlcv;
                                    }
                                }
                            }
                            else
                            {
                                if (itemdata.Limit.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        var llplot = wpfplot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                        tllcv = llcv;
                                    }
                                }

                                if (itemdata.Limit.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = wpfplot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                        thlcv = hlcv;
                                    }
                                }
                            }
                            
                            if (itemdata.Result == TF_ItemStatus.Failed && cv.YLog)
                            {
                                double yl = tllcv?.Y?.Min() ?? double.NaN;
                                double yh = thlcv?.Y?.Max() ?? double.NaN;
                                if (cv.YLog)
                                {
                                    if(!double.IsNaN(yl))
                                    {
                                        yl = Math.Log10(yl) * (1 - LimitMargin);
                                    }

                                    if (!double.IsNaN(yh))
                                    {
                                        yh = Math.Log10(yh) * (1 + LimitMargin);
                                    }
                                }

                                wpfplot.Plot.SetAxisLimitsY(yl, yh);
                            }
                            else
                            {
                                wpfplot.Plot.AxisAutoY();
                            }

                            if (cv.XLog)
                            {
                                wpfplot.Plot.XAxis.TickLabelFormat(logTickLabels);
                            }
                            else
                            {
                                wpfplot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });

                            }
                            wpfplot.Plot.XAxis.MinorLogScale(cv.XLog);
                            wpfplot.Plot.XAxis.MinorGrid(true);

                            //wpfplot.Plot.YTicks();
                            if (cv.YLog)
                            {
                                wpfplot.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                            }
                            else
                            {
                                wpfplot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            }
                            wpfplot.Plot.YAxis.MinorLogScale(cv.YLog);
                            wpfplot.Plot.YAxis.MinorGrid(true);

                            wpfplot.Plot.YAxis.AutomaticTickPositions();

                            wpfplot.Plot.XAxis.Label(cv.X_Unit);
                            wpfplot.Plot.YAxis.Label(cv.Y_Unit);

                            wpfplot.Plot.Title(data.Parent?.Element?.Name);
                            wpfplot.Plot.Legend(false);

                            wpfplot.Refresh();
                        }
                    }
                }
                catch (InvalidOperationException ioex)
                {
                    self.UILog($"Draw Graph Error on {itemdata.Name}. Err: {ioex.Message}");
                    if (!hasinfinity)
                    {
                        self.ItemContainsInfinity.Add(itemdata);
                        RefreshData(self, wpfplot, data);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        public static void RefreshAttachData(VM_Slot self, WpfPlot wpfplot, Nest<TF_StepData> data)
        {
            if (data is null) return;

            if (data.Element is TF_ItemData itemdata)
            {
                bool hasinfinity = self.ItemContainsInfinity.Contains(itemdata);

                try
                {
                    if (itemdata.Limit.Comp == Comparison.NULL)
                    {
                        if (data.Children?.FirstOrDefault()?.Element is TF_ItemData pitemdata)
                        {
                            if (pitemdata.Value is double)
                            {
                                wpfplot.Plot.Clear();

                                double[] value_d = new double[data.Count];
                                double[] hl = new double[data.Count];
                                double[] ll = new double[data.Count];
                                string[] labels = new string[data.Count];
                                double[] ys = new double[data.Count];

                                bool havelimit = false;
                                for (int i = 0; i < data.Count; i++)
                                {
                                    if (data[i].Element is TF_ItemData item)
                                    {
                                        if (item.Result == TF_ItemStatus.NotTested) continue;
                                        value_d[i] = (double)item.Value;

                                        if (item.Limit.USL is double hlv)
                                        {
                                            hl[i] = hlv - value_d[i];
                                            if (!double.IsNaN(hlv))
                                            {
                                                havelimit = true;
                                            }
                                        }
                                        else
                                        {
                                            hl[i] = double.NaN;
                                        }

                                        if (item.Limit.LSL is double llv)
                                        {
                                            ll[i] = value_d[i] - llv;
                                            if (!double.IsNaN(llv))
                                            {
                                                havelimit = true;
                                            }
                                        }
                                        else
                                        {
                                            ll[i] = double.NaN;
                                        }
                                    }

                                    labels[i] = data[i].Element.Name;
                                    ys[i] = i;
                                }

                                wpfplot.Plot.YTicks(labels);
                                wpfplot.Plot.YAxis.MinorLogScale(false);

                                var barplot = wpfplot.Plot.AddBar(value_d);

                                barplot.Orientation = ScottPlot.Orientation.Horizontal;
                                barplot.ShowValuesAboveBars = true;
                                barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });

                                if (havelimit)
                                {
                                    var ebplot = wpfplot.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                    ebplot.LineWidth = 1;
                                    ebplot.MarkerSize = 10;
                                }

                                wpfplot.Plot.XAxis.Label(pitemdata.Limit.Unit);
                                wpfplot.Plot.XAxis.TickLabelFormat((double a) => { return a.ToString(); });
                                wpfplot.Plot.XAxis.MinorLogScale(false);
                                wpfplot.Plot.YLabel(null);

                                wpfplot.Plot.Title($"{data.Element.Name}");
                                wpfplot.Plot.Legend(true);

                                wpfplot.Refresh();
                            }
                            else if (pitemdata.Value is TF_Curve cv_template)
                            {
                                wpfplot.Plot.Clear();
                                wpfplot.Plot.ResetLayout();
                                for (int i = 0; i < data.Count; i++)
                                {
                                    if (((TF_ItemData)data[i].Element).Value is TF_Curve cv)
                                    {
                                        if (cv.Length == 0) continue;

                                        var xs = cv.XLog ? cv.X.Select(x => Math.Log10(x)).ToArray() : cv.X;
                                        var ys = cv.YLog ? cv.Y.Select(x => Math.Log10(x)).ToArray() : cv.Y;

                                        if (hasinfinity)
                                        {
                                            ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                            xs = xs.Take(ys.Length).ToArray();
                                        }

                                        var plot = wpfplot.Plot.AddScatter(xs, ys, markerSize: 0);
                                        plot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;
                                        if (((TF_ItemData)data[i].Element)?.Limit?.Tag is TF_Limit template)
                                        {
                                            if (template.LSL is TF_Curve llcv)
                                            {
                                                if (llcv.Length > 0)
                                                {
                                                    var llplot = wpfplot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                                    llplot.Label = $"{data[i].Element.Name}_LL";
                                                }
                                            }

                                            if (template.USL is TF_Curve hlcv)
                                            {
                                                if (hlcv.Length > 0)
                                                {
                                                    var hlplot = wpfplot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                                    hlplot.Label = $"{data[i].Element.Name}_HL";
                                                }
                                            }
                                        }

                                        plot.Label = data[i].Element.Name;
                                    }
                                }

                                //wpfplot.Plot.XTicks();
                                if (cv_template.XLog)
                                {
                                    wpfplot.Plot.XAxis.TickLabelFormat(logTickLabels);
                                }
                                else
                                {
                                    wpfplot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                wpfplot.Plot.XAxis.MinorLogScale(cv_template.XLog);
                                wpfplot.Plot.XAxis.MinorGrid(true);

                                //wpfplot.Plot.YTicks();
                                if (cv_template.YLog)
                                {
                                    wpfplot.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                                }
                                else
                                {
                                    wpfplot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                wpfplot.Plot.YAxis.MinorLogScale(cv_template.YLog);
                                wpfplot.Plot.YAxis.MinorGrid(true);

                                wpfplot.Plot.YAxis.AutomaticTickPositions();

                                wpfplot.Plot.XAxis.Label(cv_template.X_Unit);
                                wpfplot.Plot.YAxis.Label(cv_template.Y_Unit);

                                wpfplot.Plot.Title(data.Element.Name);

                                wpfplot.Plot.Legend(true);

                                wpfplot.Refresh();
                            }
                        }
                    }
                    else
                    {
                        if (itemdata.Value is double vd)
                        {
                            wpfplot.Plot.Clear();
                            double[] index_d = new double[self.AttachResults.Count];
                            double[] value_d = new double[self.AttachResults.Count];
                            //double[] hl = new double[self.AttachResults.Count];
                            //double[] ll = new double[self.AttachResults.Count];
                            //double[] ys = new double[1] { 0 };

                            for (int i = 0; i < self.AttachResults.Count; i++)
                            {
                                if(FetchStepWithSameLimit(self.AttachResults[i].StepDatas, itemdata.Limit) is TF_ItemData fetch)
                                {
                                    value_d[i] = (double)fetch.Value;
                                    
                                }
                                index_d[i] = i;
                                if (!double.IsNaN(value_d[i]))
                                {
                                    var plot = wpfplot.Plot.AddBar(index_d[i], value_d[i]);
                                    plot.Label = self.AttachResults[i].SerialNumber;
                                }
                            }

                            

                            var signalplot = wpfplot.Plot.AddScatterPoints(index_d, value_d);
                            signalplot.Label = itemdata.Name;
                            signalplot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;

                            if (itemdata.Limit.USL is double hlv)
                            {
                                var hlline = wpfplot.Plot.AddHorizontalLine(hlv);
                                hlline.Label = "HSL";
                            }

                            if (itemdata.Limit.LSL is double llv)
                            {
                                var llline = wpfplot.Plot.AddHorizontalLine(llv);
                                llline.Label = "LSL";
                            }

                            wpfplot.Plot.XTicks(self.AttachResults.Select(x => x.SerialNumber).ToArray());
                            wpfplot.Plot.YLabel(itemdata.Limit?.Unit);
                            wpfplot.Plot.Title(itemdata.Name);
                            //wpfplot.Plot.XAxis.MinorGrid(false);
                            //wpfplot.Plot.XAxis.MinorLogScale(false);
                            wpfplot.Plot.YAxis.MinorGrid(false);
                            wpfplot.Plot.YAxis.MinorLogScale(false);

                            wpfplot.Refresh();
                        }
                        else if (itemdata.Value is TF_Curve cv)
                        {
                            wpfplot.Plot.Clear();
                            //wpfplot.Plot.YTicks(new string[0]{ });  // this will make Ytick to be null

                            if (cv.Length == 0) return;
                            double[] xs = null;
                            double[] ys = null;

                            //double[] hls = null;
                            //double[] lls = null;

                            if (cv.XLog)
                            {
                                xs = cv.X.Select(x => Math.Log10(x)).ToArray();
                            }
                            else
                            {
                                xs = cv.X;
                            }

                            if (cv.YLog)
                            {
                                ys = cv.Y.Select(x => Math.Log10(x)).ToArray();
                            }
                            else
                            {
                                ys = cv.Y;
                            }

                            if (hasinfinity)
                            {
                                ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                xs = xs.Take(ys.Length).ToArray();
                            }

                            var plot = wpfplot.Plot.AddScatter(xs, ys, markerSize: 0);  // Ys might contains Infinity or NaN
                            plot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;  // need update library. .netfx 4.6.2 required
                            plot.Label = itemdata.Name;

                            TF_Curve tllcv = null;
                            TF_Curve thlcv = null;
                            if (itemdata.Limit.Tag is TF_Limit template)
                            {
                                if (template.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        tllcv = llcv;
                                        var llplot = wpfplot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                    }
                                }

                                if (template.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = wpfplot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                        thlcv = hlcv;
                                    }
                                }
                            }
                            else
                            {
                                if (itemdata.Limit.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        var llplot = wpfplot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                        tllcv = llcv;
                                    }
                                }

                                if (itemdata.Limit.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = wpfplot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                        thlcv = hlcv;
                                    }
                                }
                            }

                            if (itemdata.Result == TF_ItemStatus.Failed && cv.YLog)
                            {
                                double yl = tllcv?.Y?.Min() ?? double.NaN;
                                double yh = thlcv?.Y?.Max() ?? double.NaN;
                                if (cv.YLog)
                                {
                                    if (!double.IsNaN(yl))
                                    {
                                        yl = Math.Log10(yl) * (1 - LimitMargin);
                                    }

                                    if (!double.IsNaN(yh))
                                    {
                                        yh = Math.Log10(yh) * (1 + LimitMargin);
                                    }
                                }

                                wpfplot.Plot.SetAxisLimitsY(yl, yh);
                            }
                            else
                            {
                                wpfplot.Plot.AxisAutoY();
                            }

                            if (cv.XLog)
                            {
                                wpfplot.Plot.XAxis.TickLabelFormat(logTickLabels);
                            }
                            else
                            {
                                wpfplot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });

                            }
                            wpfplot.Plot.XAxis.MinorLogScale(cv.XLog);
                            wpfplot.Plot.XAxis.MinorGrid(true);

                            //wpfplot.Plot.YTicks();
                            if (cv.YLog)
                            {
                                wpfplot.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                            }
                            else
                            {
                                wpfplot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            }
                            wpfplot.Plot.YAxis.MinorLogScale(cv.YLog);
                            wpfplot.Plot.YAxis.MinorGrid(true);

                            wpfplot.Plot.YAxis.AutomaticTickPositions();

                            wpfplot.Plot.XAxis.Label(cv.X_Unit);
                            wpfplot.Plot.YAxis.Label(cv.Y_Unit);

                            wpfplot.Plot.Title(itemdata.Name);
                            wpfplot.Plot.Legend(true);

                            wpfplot.Refresh();
                        }
                    }
                }
                catch (InvalidOperationException ioex)
                {
                    self.UILog($"Draw Graph Error on {itemdata.Name}. Err: {ioex.Message}");
                    if (!hasinfinity)
                    {
                        self.ItemContainsInfinity.Add(itemdata);
                        RefreshAttachData(self, wpfplot, data);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        static string logTickLabels(double y) => Math.Pow(10, y).ToString();
        static string logTickLabels_N0(double y) => Math.Pow(10, y).ToString("N0");
        static string logTickLabels_G(double y) => Math.Pow(10, y).ToString("G");

        private void cmd_ShowRecordInNewWindow(object obj)
        {
            Window window = new Window();
            Ctrl_RecordView recordView = new Ctrl_RecordView();

            if(FlatStepData.Count == 0)
            {
                var d = Result.StepDatas.ToFlatList();
                for (int i = 1; i < d.Count; i++)
                {
                    FlatStepData.Add(d[i].Element);
                }
            }

            recordView.ItemsSource = FlatStepData;

            window.Content = recordView;

            window.ShowDialog();
        }

        private void cmd_ShowChartInNewWindow(object obj)
        {
            MessageBox.Show("Not Implemented yet");
        }

        private void cmd_ExportResultAsList(object obj)
        {
            MessageBox.Show("Not Implemented yet", "ExportResultAsList");
        }

        public static Nest<TF_StepData> GetFirstDefectItems(Nest<TF_StepData> nest)
        {
            if (nest.Element.Result == TF_ItemStatus.Failed)
            {
                if (nest.Element is TF_ItemData itemdata)
                {
                    if (itemdata.Limit.Comp != Comparison.NULL)
                    {
                        return nest;
                    }
                    else
                    {
                        foreach (var sub in nest.Children)
                        {
                            if(GetFirstDefectItems(sub) is Nest<TF_StepData> subitem)
                            {
                                return subitem;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public void RegisterItemPlot(Nest<TF_StepData> item, WpfPlot plot)
        {
            ItemPlotMap.Add(item, plot);
            if(AttachResultInChart)
            {
                RefreshAttachData(this, plot, item);
            }
            else
            {
                RefreshData(this, plot, item);
            }
            
        }

        public void UnregisterItemPlot(Nest<TF_StepData> item)
        {
            ItemPlotMap.Remove(item);
        }

        private static TF_ItemData FetchStepWithSameLimit(Nest<TF_StepData> tree, TF_Limit limit)
        {
            foreach (var sub in tree)
            {
                if (sub.Element is TF_ItemData subitem)
                {
                    if (subitem.Limit == limit)
                    {
                        return subitem;
                    }
                    else
                    {
                        var fetch = FetchStepWithSameLimit(sub, limit);
                        if ( fetch != null)
                        {
                            return fetch;
                        }
                    }
                }
            }
            return null;
        }
    }

    public enum MesStatus
    {
        Offline,
        Disable,
        SFCsOn,
        SFCsOff,
    }
}
