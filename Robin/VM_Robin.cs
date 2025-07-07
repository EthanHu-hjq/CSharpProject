using Microsoft.Win32;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestCore.Data;
using TestCore;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Navigation;
using ApEngine;
using System.Web.UI;
using System.Windows.Threading;
using Robin.UIs;
using System.Xml.Serialization;
using System.Xml.Schema;
using System.Xml;
using ApEngine.Base;
using TestCore.Base;
using System.Security.Cryptography.X509Certificates;
using TestCore.UI;
using Robin.Core;
using Robin.Ctrls;
using TestCore.Services;
using System.Threading;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Reflection.Emit;
using AudioPrecision.API;
using System.Runtime.Remoting.Channels;
using ControlzEx.Theming;
using System.Windows.Markup;
using System.Web.SessionState;
using System.Reflection;
using MahApps.Metro.Controls.Dialogs;

namespace Robin
{
    public class VM_Robin : DependencyObject
    {
        public static GroupItem<double> dBrGItems = App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.dBrG] as GroupItem<double>;
        public static GroupItem<double> SenseRItems = App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.SenseR] as GroupItem<double>;
        public static GroupItem<string> MicCalItems = App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.Mic_Cal] as GroupItem<string>;
        public static GroupItem<string> OutputEqItems = App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.Output_EQ] as GroupItem<string>;
        public static GroupItem<string> DataExportSpecItems = App.GroupSetting.GlobalDefinitionGroups[GlobalDefinitionGroupName.Export_Data_Specification] as GroupItem<string>;

        public WpfPlot ActivePlot { get; set; }
        public WpfPlot ActivePlot_Attach { get; set; }

        private ApEngine.ApxEngine engine = new ApEngine.ApxEngine();

        public DelegateCommand OpenScript { get; private set; }
        //public DelegateCommand OpenTemplate { get; private set; }
        public DelegateCommand RunAllSequence { get; }
        public DelegateCommand RunScript { get; private set; }

        public DelegateCommand WipeOutLatestData { get; }
        public DelegateCommand SaveScriptEx { get; private set; }
        public DelegateCommand GlobalConfig { get; }
        public DelegateCommand EquipmentConfig { get; }
        public DelegateCommand ShowAbout { get; }
        public DelegateCommand Exit { get; }

        public bool IsEditMode
        {
            get { return (bool)GetValue(IsEditModeProperty); }
            set { SetValue(IsEditModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register("IsEditMode", typeof(bool), typeof(VM_Robin), new PropertyMetadata(false, IsEditModeChanged));

        static ControlzEx.Theming.Theme defaulttheme = ThemeManager.Current.DetectTheme();
        static ControlzEx.Theming.Theme edittheme = ThemeManager.Current.GetTheme("Light.Blue");

        private static void IsEditModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is VM_Robin vm)
            {
                if (vm.IsEditMode)
                {
                    ThemeManager.Current.ChangeTheme(Application.Current, edittheme);
                }
                else
                {
                    ThemeManager.Current.ChangeTheme(Application.Current, defaulttheme);
                }

                if (vm.ScriptEx?.Script != null)
                {
                    vm.ScriptEx.Script.LockScript(!vm.IsEditMode);
                }
            }
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(VM_Robin), new PropertyMetadata(null));



        public string Model
        {
            get { return (string)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(string), typeof(VM_Robin), new PropertyMetadata("Test"));

        public string ModelDescription
        {
            get { return (string)GetValue(ModelDescriptionProperty); }
            set { SetValue(ModelDescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ModelDescription.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelDescriptionProperty =
            DependencyProperty.Register("ModelDescription", typeof(string), typeof(VM_Robin), new PropertyMetadata("Standard"));



        public string VacsData
        {
            get { return (string)GetValue(VacsDataProperty); }
            set { SetValue(VacsDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VacsData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VacsDataProperty =
            DependencyProperty.Register("VacsData", typeof(string), typeof(VM_Robin), new PropertyMetadata("Save"));

        public bool IsVacs
        {
            get { return (bool)GetValue(IsVacsProperty); }
            set { SetValue(IsVacsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsVacs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsVacsProperty =
            DependencyProperty.Register("IsVacs", typeof(bool), typeof(VM_Robin), new PropertyMetadata(true));



        public bool AttachResult
        {
            get { return (bool)GetValue(AttachResultProperty); }
            set { SetValue(AttachResultProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AttachResult.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AttachResultProperty =
            DependencyProperty.Register("AttachResult", typeof(bool), typeof(VM_Robin), new PropertyMetadata(false));


        public Sequence ActiveSequence
        {
            get { return (Sequence)GetValue(ActiveSequenceProperty); }
            set { SetValue(ActiveSequenceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveSequence.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveSequenceProperty =
            DependencyProperty.Register("ActiveSequence", typeof(Sequence), typeof(VM_Robin), new PropertyMetadata(null, ActiveSequenceChanged));

        private static void ActiveSequenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Robin self)
            {
                if (e.NewValue is Sequence seq)
                {
                    if (self.ScriptEx.Script.Activate(seq) > 0)
                    {
                        ApplySequenceSetting(self, seq);

                        self.CheckTestReady();
                    }

                    if (self.Executions.FirstOrDefault(x => x.Name == seq.Name) is Execution exec)
                    {
                        self.ActiveExecution = exec;
                        self.Result = self.ActiveExecution.Results[0];
                    }
                    else
                    {
                        self.ActiveExecution = self.engine.StartExecution(self.ScriptEx.Script, null) as Execution;
                        //self.Result.TestEnd -= self.VM_Robin_TestEnd;

                        self.Result = self.ActiveExecution.Results[0];

                        self.Result.AttachProperties.Add("Model", self.Model);
                        self.Result.AttachProperties.Add("Model_Option", self.ModelDescription);

                        self.Executions.Add(self.ActiveExecution);
                        self.Result.TestStart += self.VM_Robin_TestStart;
                        self.Result.TestEnd += self.VM_Robin_TestEnd;

                        self.AttachResults.Add(seq, new List<TF_Result>());
                    }

                    self.TestResultUpdated?.Invoke(self, null);
                }
            }
        }

        static void ApplySequenceSetting(VM_Robin self, Sequence seq)
        {
            var seqex = self.ScriptEx.SequenceExtendeds.First(x => x.Name == seq.Name);

            double dbrg = double.NaN;

            var dbrgname = seqex.dBrG?.Split(':')[0];

            if (dBrGItems.Keys.Contains(dbrgname ?? String.Empty))
            {
                dbrg = VM_Robin.dBrGItems[dbrgname];
                // How to set dBrG for all signalpath
                //ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.dBrG.Unit = "mVrms";
                //ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.dBrG.Value = VM_Robin.dBrGItems[seqex.dBrG];
            }
            else
            {
                if (double.TryParse(seqex.dBrG, out dbrg))
                {
                }
                else
                {
                    dbrg = double.NaN;
                }
            }
            
            if (!double.IsNaN(dbrg))
            {
                if (App.GroupSetting.HardwareCalibrationData.AnalogOutputs is List<Calib_AnalogOutput> calib)
                {
                    var dbrgstr = $"{dbrg} mVrms";
                    calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.AnalogBalanced, Enable = true, dBrG = dbrgstr });
                    //calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.AnalogUnbalanced, Enable = true, dBrG = dbrgstr });
                    //calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.TransducerInterface, Enable = true, dBrG = dbrgstr });
                    //calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.ASIO, Enable = true, dBrG = dbrgstr });
                    //calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.DigitalBalanced, Enable = true, dBrG = dbrgstr });
                    //calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.DigitalUnbalanced, Enable = true, dBrG = dbrgstr });
                    //calib.Add(new Calib_AnalogOutput() { ConnectorType = OutputConnectorType.DigitalOptical, Enable = true, dBrG = dbrgstr });
                }
            }

            var miccalname = seqex.MicCal?.Split(':')[0];
            if (MicCalItems.Keys.Contains(miccalname ?? String.Empty))
            {
                if (App.GroupSetting.HardwareCalibrationData.AcousticInputs is List<Calib_AcousticInput> calib)
                {
                    //var conntypes = new InputConnectorType[] { InputConnectorType.AnalogBalanced, InputConnectorType.AnalogUnbalanced, InputConnectorType.TransducerInterface,
                    //    InputConnectorType.ASIO, InputConnectorType.DigitalBalanced, InputConnectorType.DigitalUnbalanced, InputConnectorType.DigitalOptical,
                    //    InputConnectorType.AnalogFile, InputConnectorType.DigitalFile,
                    //};

                    var miccalfilepath = Path.Combine(App.CommonFileDir, MicCalItems[miccalname]);
                    MicCalData miccaldata = MicCalData.Load(miccalfilepath);

                    List<Calib_AcousticInputChannel> lcach = new List<Calib_AcousticInputChannel>();
                    foreach (var mic in miccaldata.MicCals)
                    {
                        lcach.Add(new Calib_AcousticInputChannel() { Index = mic.Index, Sensitivity = mic.Sensitivity, SerialNo = mic.ID });
                    }

                    calib.Add(new Calib_AcousticInput() { Channels = lcach });
                    //foreach (var type in conntypes)
                    //{
                    //    calib.Add(new Calib_AcousticInput() { Channels = lcach, ConnectorType = type });
                    //}
                }

                //ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.
            }

            var eqname = seqex.OutputEq?.Split(':')[0];
            if (OutputEqItems.Keys.Contains(eqname ?? String.Empty))
            {
                if (App.GroupSetting.HardwareCalibrationData.OutputEqDatas is List<EqCalibData> leq)
                {
                    leq.Add(new EqCalibData() { EqPath = Path.Combine(App.CommonFileDir, OutputEqItems[eqname]) });
                }

                //ApxEngine.ApRef.SignalPathSetup.OutputEq.LoadEqFromFile(Path.Combine(App.CommonFileDir, OutputEqItems[eqname]), false, true);
            }

            var sensername = seqex.SenseR?.Split(':')[0];
            if (SenseRItems.Keys.Contains(sensername ?? String.Empty))
            {
                if (App.GroupSetting.HardwareCalibrationData.ImpedanceThieleSmalls is List<Calib_ImpedanceThieleSmall> calib_ts)
                {
                    calib_ts.Add(new Calib_ImpedanceThieleSmall() { SenseR = VM_Robin.SenseRItems[sensername] * 10e-3 });
                }

                if (App.GroupSetting.HardwareCalibrationData.LoudspeakerProductionTests is List<Calib_LoudspeakerProductionTest> calib_pt)
                {
                    calib_pt.Add(new Calib_LoudspeakerProductionTest() { SenseR = VM_Robin.SenseRItems[sensername] * 10e-3 });
                }
            }

            self.engine.ApplyCalibDataWithoutMatch(App.GroupSetting.HardwareCalibrationData);

            if (App.HardwareDefinition.ControlStates.Keys.Contains(seqex.AuxOut ?? String.Empty))
            {
                var curr = (~App.HardwareDefinition.ControlMask) & ApxEngine.AuxControlOutputValue;

                var val = App.HardwareDefinition.ControlMask & App.HardwareDefinition.ControlStates[seqex.AuxOut];

                ApxEngine.AuxControlOutputValue = (byte)(curr | val);
            }

            ApxEngine.ApRef.Sequence.Report.Checked = seqex.EnableReport;
            ApxEngine.ApRef.Sequence.Report.AutoSaveReport = seqex.EnableReport;
            ApxEngine.ApRef.Sequence.DataOutput.WriteMeterReadingsToCsvFile = seqex.EnableDataOutput;
        }

        public Sequence[] Sequences
        {
            get { return (Sequence[])GetValue(SequencesProperty); }
            set { SetValue(SequencesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SequenceNames.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SequencesProperty =
            DependencyProperty.Register("Sequences", typeof(Sequence[]), typeof(VM_Robin), new PropertyMetadata(null));

        public bool IsSequenceIdle
        {
            get { return (bool)GetValue(IsSequenceIdleProperty); }
            set { SetValue(IsSequenceIdleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSequenceIdle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSequenceIdleProperty =
            DependencyProperty.Register("IsSequenceIdle", typeof(bool), typeof(VM_Robin), new PropertyMetadata(true));

        public string DutSn
        {
            get { return (string)GetValue(DutSnProperty); }
            set { SetValue(DutSnProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutSn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutSnProperty =
            DependencyProperty.Register("DutSn", typeof(string), typeof(VM_Robin), new PropertyMetadata("S1"));

        public TF_Result Result
        {
            get { return (TF_Result)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Result.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(TF_Result), typeof(VM_Robin), new PropertyMetadata(null));

        public Nest<TF_StepData> StepData
        {
            get { return (Nest<TF_StepData>)GetValue(StepDataProperty); }
            set { SetValue(StepDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StepData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StepDataProperty =
            DependencyProperty.Register("StepData", typeof(Nest<TF_StepData>), typeof(VM_Robin), new PropertyMetadata(null, StepDataChanged));

        private static void StepDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Robin self)
            {
                if (e.NewValue is Nest<TF_StepData> data)
                {
                    RefreshData(self, data);
                    RefreshAttachData(self, data);
                }
            }
        }

        public bool ApVisible
        {
            get { return (bool)GetValue(ApVisibleProperty); }
            set { SetValue(ApVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ApVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ApVisibleProperty =
            DependencyProperty.Register("ApVisible", typeof(bool), typeof(VM_Robin), new PropertyMetadata(false));

        public string ApxVer
        {
            get { return (string)GetValue(ApxVerProperty); }
            set { SetValue(ApxVerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ApxVer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ApxVerProperty =
            DependencyProperty.Register("ApxVer", typeof(string), typeof(VM_Robin), new PropertyMetadata(""));



        public ObservedNest<SampleFileName> Templates
        {
            get { return (ObservedNest<SampleFileName>)GetValue(TemplatesProperty); }
            set { SetValue(TemplatesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Templates.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TemplatesProperty =
            DependencyProperty.Register("Templates", typeof(ObservedNest<SampleFileName>), typeof(VM_Robin), new PropertyMetadata(null));

        public ScriptExtended ScriptEx
        {
            get { return (ScriptExtended)GetValue(ScriptExProperty); }
            set { SetValue(ScriptExProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ScriptEx.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScriptExProperty =
            DependencyProperty.Register("ScriptEx", typeof(ScriptExtended), typeof(VM_Robin), new PropertyMetadata(null));

        static string logTickLabels(double y) => Math.Pow(10, y).ToString();
        static string logTickLabels_N0(double y) => Math.Pow(10, y).ToString("N0");
        static string logTickLabels_G(double y) => Math.Pow(10, y).ToString("G");

        public List<TF_ItemData> ItemContainsInfinity = new List<TF_ItemData>();

        public static void RefreshData(VM_Robin self, Nest<TF_StepData> data)
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
                                self.ActivePlot.Plot.Clear();

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
                                            if(!double.IsNaN(llv))
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

                                self.ActivePlot.Plot.YTicks(labels);
                                self.ActivePlot.Plot.YAxis.MinorLogScale(false);

                                var barplot = self.ActivePlot.Plot.AddBar(value_d);

                                barplot.Orientation = ScottPlot.Orientation.Horizontal;
                                barplot.ShowValuesAboveBars = true;
                                barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });

                                if (havelimit)
                                {
                                    var ebplot = self.ActivePlot.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                    ebplot.LineWidth = 1;
                                    ebplot.MarkerSize = 10;
                                }

                                self.ActivePlot.Plot.XAxis.Label(pitemdata.Limit.Unit);
                                self.ActivePlot.Plot.XAxis.TickLabelFormat((double a) => { return a.ToString(); });
                                self.ActivePlot.Plot.XAxis.MinorLogScale(false);
                                self.ActivePlot.Plot.YLabel(null);

                                self.ActivePlot.Plot.Title($"{data.Element.Name}");
                                self.ActivePlot.Plot.Legend();

                                self.ActivePlot.Refresh();
                            }
                            else if (pitemdata.Value is TF_Curve cv_template)
                            {
                                self.ActivePlot.Plot.Clear();
                                self.ActivePlot.Plot.ResetLayout();
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

                                        var plot = self.ActivePlot.Plot.AddScatter(xs, ys, markerSize: 0);
                                        
                                        if (((TF_ItemData)data[i].Element)?.Limit?.Tag is TF_Limit template)
                                        {
                                            if (template.LSL is TF_Curve llcv)
                                            {
                                                if (llcv.Length > 0)
                                                {
                                                    var llplot = self.ActivePlot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                                    llplot.Label = $"{data[i].Element.Name}_LL";
                                                }
                                            }

                                            if (template.USL is TF_Curve hlcv)
                                            {
                                                if (hlcv.Length > 0)
                                                {
                                                    var hlplot = self.ActivePlot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                                    hlplot.Label = $"{data[i].Element.Name}_HL";
                                                }
                                            }
                                        }

                                        plot.Label = data[i].Element.Name;
                                    }
                                }

                                //self.ActivePlot.Plot.XTicks();
                                if (cv_template.XLog)
                                {
                                    self.ActivePlot.Plot.XAxis.TickLabelFormat(logTickLabels);
                                }
                                else
                                {
                                    self.ActivePlot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                self.ActivePlot.Plot.XAxis.MinorLogScale(cv_template.XLog);
                                self.ActivePlot.Plot.XAxis.MinorGrid(true);

                                //self.ActivePlot.Plot.YTicks();
                                if (cv_template.YLog)
                                {
                                    self.ActivePlot.Plot.YAxis.TickLabelFormat(logTickLabels_G);
                                    
                                }
                                else
                                {
                                    self.ActivePlot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                self.ActivePlot.Plot.YAxis.MinorLogScale(cv_template.YLog);
                                self.ActivePlot.Plot.YAxis.MinorGrid(true);

                                self.ActivePlot.Plot.YAxis.AutomaticTickPositions();

                                self.ActivePlot.Plot.XAxis.Label(cv_template.X_Unit);
                                self.ActivePlot.Plot.YAxis.Label(cv_template.Y_Unit);

                                self.ActivePlot.Plot.Title(data.Element.Name);

                                self.ActivePlot.Plot.Legend();

                                self.ActivePlot.Refresh();
                            }
                        }
                    }
                    else
                    {
                        if (itemdata.Value is double vd)
                        {
                            self.ActivePlot.Plot.Clear();

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

                            var barplot = self.ActivePlot.Plot.AddBar(new double[1] { vd });
                            barplot.Orientation = ScottPlot.Orientation.Horizontal;
                            barplot.Label = itemdata.Name;
                            barplot.ShowValuesAboveBars = true;
                            barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });

                            if (double.IsNaN(ll[0]) && double.IsNaN(hl[0]))
                            {
                            }
                            else
                            {
                                var ebplot = self.ActivePlot.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                ebplot.LineWidth = 3;
                                ebplot.MarkerSize = 10;
                            }

                            self.ActivePlot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            self.ActivePlot.Plot.YTicks(new string[1] { itemdata.Name });
                            self.ActivePlot.Plot.XLabel(itemdata.Limit?.Unit);
                            self.ActivePlot.Plot.YLabel(null);
                            self.ActivePlot.Plot.Title(null);
                            self.ActivePlot.Plot.XAxis.MinorLogScale(false);

                            self.ActivePlot.Refresh();
                        }
                        else if (itemdata.Value is TF_Curve cv)
                        {
                            self.ActivePlot.Plot.Clear();
                            //self.ActivePlot.Plot.YTicks(new string[0]{ });  // this will make Ytick to be null

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

                            if(hasinfinity)
                            {
                                ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                xs = xs.Take(ys.Length).ToArray();
                            }

                            var plot = self.ActivePlot.Plot.AddScatter(xs, ys, markerSize: 0);  // Ys might contains Infinity or NaN
                            plot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;
                            plot.Label = itemdata.Name;

                            if (itemdata.Limit.Tag is TF_Limit template)
                            {
                                if (template.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        var llplot = self.ActivePlot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                    }
                                }

                                if (template.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = self.ActivePlot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                    }
                                }
                            }
                            else
                            {
                                if (itemdata.Limit.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        var llplot = self.ActivePlot.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                    }
                                }

                                if (itemdata.Limit.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = self.ActivePlot.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                    }
                                }
                            }

                            if (cv.XLog)
                            {
                                self.ActivePlot.Plot.XAxis.TickLabelFormat(logTickLabels);
                            }
                            else
                            {
                                self.ActivePlot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });

                            }
                            self.ActivePlot.Plot.XAxis.MinorLogScale(cv.XLog);
                            self.ActivePlot.Plot.XAxis.MinorGrid(true);

                            //self.ActivePlot.Plot.YTicks();
                            if (cv.YLog)
                            {
                                self.ActivePlot.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                            }
                            else
                            {
                                self.ActivePlot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            }
                            self.ActivePlot.Plot.YAxis.MinorLogScale(cv.YLog);
                            self.ActivePlot.Plot.YAxis.MinorGrid(true);

                            self.ActivePlot.Plot.YAxis.AutomaticTickPositions();

                            self.ActivePlot.Plot.XAxis.Label(cv.X_Unit);
                            self.ActivePlot.Plot.YAxis.Label(cv.Y_Unit);

                            self.ActivePlot.Plot.Title(itemdata.Name);
                            self.ActivePlot.Plot.Legend();

                            self.ActivePlot.Refresh();
                        }
                    }
                }
                catch (InvalidOperationException ioex)
                {
                    self.UILog($"Draw Graph Error on {itemdata.Name}. Err: {ioex.Message}");
                    if(!hasinfinity)
                    {
                        self.ItemContainsInfinity.Add(itemdata);
                        RefreshData(self, data);
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        public static void RefreshAttachData(VM_Robin self, Nest<TF_StepData> data)
        {
            if (data is null) return;

            if (data.Element is TF_ItemData itemdata)
            {
                bool hasinfinity = self.ItemContainsInfinity.Contains(itemdata);
                try
                {
                    var rss = self.AttachResults[self.ActiveSequence];

                    if (rss.Count == 0) return;

                    if (itemdata.Value is double vd)
                    {
                        List<int> idxs = new List<int>();
                        if (!FindNestPath(self.Result.StepDatas, data, idxs)) return;

                        idxs.Reverse();

                        self.ActivePlot_Attach.Plot.Clear();

                        double[] value_d = new double[rss.Count];
                        string[] label_d = new string[rss.Count];
                        for (int i = 0; i < rss.Count; i++)
                        {
                            Nest<TF_StepData> temp = rss[i].StepDatas;
                            foreach (var idx in idxs)
                            {
                                temp = temp[idx];
                            }

                            value_d[i] = (double)((TF_ItemData)temp.Element).Value;
                            label_d[i] = $"{rss[i].ProductInfo}_{rss[i].SerialNumber}";
                        }

                        var bar = self.ActivePlot_Attach.Plot.AddBar(value_d);
                        bar.ShowValuesAboveBars = true;
                        bar.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });
                        self.ActivePlot_Attach.Plot.XTicks(labels: label_d);
                        self.ActivePlot_Attach.Plot.XAxis.MinorLogScale(false);

                        self.ActivePlot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                        self.ActivePlot_Attach.Plot.YAxis.Label(itemdata.Limit?.Unit);
                        self.ActivePlot_Attach.Plot.Title(itemdata.Name);

                        self.ActivePlot_Attach.Refresh();
                    }
                    else if (itemdata.Value is TF_Curve cv)
                    {
                        List<int> idxs = new List<int>();
                        if (!FindNestPath(self.Result.StepDatas, data, idxs)) return;

                        idxs.Reverse();

                        var namestep = self.Result.StepDatas;
                        string[] names = new string[idxs.Count];
                        for (int i = 0; i < idxs.Count; i++)
                        {
                            namestep = namestep[idxs[i]];
                            names[i] = namestep.Element.Name;
                        }

                        var name = string.Join(".", names);

                        self.ActivePlot_Attach.Plot.Clear();

                        var value_cv = new TF_Curve[rss.Count];

                        double[] xs = null;
                        double[] ys = null;

                        //double[] hls = null;
                        //double[] lls = null;

                        for (int i = 0; i < rss.Count; i++)
                        {
                            Nest<TF_StepData> temp = rss[i].StepDatas;
                            
                            foreach (var idx in idxs)
                            {
                                temp = temp[idx];
                            }

                            value_cv[i] = (TF_Curve)((TF_ItemData)temp.Element).Value;

                            if (value_cv[i].Length == 0)
                            {
                                continue;
                            }
                            else
                            {

                                if (cv.XLog)
                                {
                                    xs = value_cv[i].X.Select(x => Math.Log10(x)).ToArray();
                                }
                                else
                                {
                                    xs = value_cv[i].X;
                                }

                                if (cv.YLog)
                                {
                                    ys = value_cv[i].Y.Select(x => Math.Log10(x)).ToArray();
                                }
                                else
                                {
                                    ys = value_cv[i].Y;
                                }

                                if (hasinfinity)
                                {
                                    ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                    xs = xs.Take(ys.Length).ToArray();
                                }

                                var plot_attach = self.ActivePlot_Attach.Plot.AddScatter(xs, ys, markerSize: 0);
                                plot_attach.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;
                                plot_attach.Label = $"{rss[i].ProductInfo}_{rss[i].SerialNumber}";
                            }
                        }

                        if (itemdata.Limit.Tag is TF_Limit template)
                        {
                            if (template.LSL is TF_Curve llcv)
                            {
                                if (llcv.Length > 0)
                                {
                                    var llplot = self.ActivePlot_Attach.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                    llplot.Label = $"{itemdata.Name}_LL";
                                }
                            }

                            if (template.USL is TF_Curve hlcv)
                            {
                                if (hlcv.Length > 0)
                                {
                                    var hlplot = self.ActivePlot_Attach.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                    hlplot.Label = $"{itemdata.Name}_HL";
                                }
                            }
                        }
                        else
                        {
                            if (itemdata.Limit.LSL is TF_Curve llcv)
                            {
                                if (llcv.Length > 0)
                                {
                                    var llplot = self.ActivePlot_Attach.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                    llplot.Label = $"{itemdata.Name}_LL";
                                }
                            }

                            if (itemdata.Limit.USL is TF_Curve hlcv)
                            {
                                if (hlcv.Length > 0)
                                {
                                    var hlplot = self.ActivePlot_Attach.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                    hlplot.Label = $"{itemdata.Name}_HL";
                                }
                            }
                        }

                        if (cv.XLog)
                        {
                            self.ActivePlot_Attach.Plot.XAxis.TickLabelFormat(logTickLabels);
                        }
                        else
                        {
                            self.ActivePlot_Attach.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });

                        }
                        self.ActivePlot_Attach.Plot.XAxis.MinorLogScale(cv.XLog);
                        self.ActivePlot_Attach.Plot.XAxis.MinorGrid(true);
                        self.ActivePlot_Attach.Plot.XAxis.AutomaticTickPositions();

                        //self.ActivePlot.Plot.YTicks();
                        if (cv.YLog)
                        {
                            self.ActivePlot_Attach.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                        }
                        else
                        {
                            self.ActivePlot_Attach.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                        }
                        self.ActivePlot_Attach.Plot.YAxis.MinorLogScale(cv.YLog);
                        self.ActivePlot_Attach.Plot.YAxis.MinorGrid(true);

                        self.ActivePlot_Attach.Plot.YAxis.AutomaticTickPositions();

                        self.ActivePlot_Attach.Plot.XAxis.Label(cv.X_Unit);
                        self.ActivePlot_Attach.Plot.YAxis.Label(cv.Y_Unit);

                        self.ActivePlot_Attach.Plot.Title(name);
                        self.ActivePlot_Attach.Plot.Legend();

                        self.ActivePlot_Attach.Refresh();
                    }
                }
                catch (InvalidOperationException ioex)
                {
                    self.UILog($"Draw Graph Error on {itemdata.Name}. Err: {ioex.Message}");
                    if (!hasinfinity)
                    {
                        self.ItemContainsInfinity.Add(itemdata);
                        RefreshAttachData(self, data);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        public static bool FindNestPath(Nest<TF_StepData> parent, Nest<TF_StepData> target, List<int> idx)
        {
            for (int i = 0; i < parent.Count; i++)
            {
                if (parent[i] == target)
                {
                    idx.Add(i);
                    return true;
                }
                else
                {
                    if(FindNestPath(parent[i], target, idx))
                    {
                        idx.Add(i);
                        return true;
                    };
                }
            }
            return false;
        }

        public event EventHandler<byte> AuxInRefreshed;
        public event EventHandler TestResultUpdated;

        public Dictionary<Sequence, List<TF_Result>> AttachResults { get; } = new Dictionary<Sequence, List<TF_Result>>();

        IDialogCoordinator DialogCoordinator { get; }

        public VM_Robin(IDialogCoordinator instance)
        {
            //var d = System.Reflection.Assembly.GetAssembly(typeof(APx500));
            //var a = d.GetName();
            //a.Version = new Version(5, 0);
            DialogCoordinator = instance;

            var temp = dBrGItems?.Select(x => $"{x.Key}: ({x.Value}{dBrGItems.Note})").ToList();  //TODO: Save string does not match
            temp?.Insert(0, "N/A");
            ApSequenceExtended.dBrG_Names = temp?.ToArray();
            
            temp = MicCalItems?.Select(x => $"{x.Key}: ({x.Value})").ToList();
            temp?.Insert(0, "N/A");
            ApSequenceExtended.MicCal_Names = temp?.ToArray();

            temp = SenseRItems?.Select(x => $"{x.Key}: ({x.Value}{SenseRItems.Note})").ToList();
            temp?.Insert(0, "N/A");
            ApSequenceExtended.SenseR_Names = temp?.ToArray();

            temp = OutputEqItems?.Select(x => $"{x.Key}: ({x.Value})").ToList(); ;
            temp?.Insert(0, "N/A");
            ApSequenceExtended.OutputEq_Names = temp?.ToArray();

            temp = App.HardwareDefinition.ControlStates.Keys.ToList();
            //temp = HardwareDefinition.ControlStates.Select(x => $"{x.Key}: ({x.Value})").ToList();
            temp.Insert(0, "N/A");
            ApSequenceExtended.AuxOut_Names = temp.ToArray();

            temp = DataExportSpecItems.Keys.ToList();
            temp.Insert(0, "N/A");
            temp.Add("All Points");
            temp.Add("Same as Graph");
            temp.Add("10 points");
            temp.Add("20 points");
            temp.Add("30 points");
            temp.Add("40 points");
            temp.Add("50 points");
            temp.Add("100 points");
            temp.Add("200 points");
            temp.Add("500 points");

            ApSequenceExtended.DataExportSpec_Names = temp.ToArray();

            engine.OnEngineInitialized += Engine_OnAsyncInitializated; //.OnAsyncInitializated += Engine_OnAsyncInitializated;
            engine.Initialize();//.InitAsyn();

            //engine.OnAsyncEngineStarted += Engine_OnAsyncEngineStarted;
            //engine.StartEngineAsyn();
            engine.OnEngineStarted += Engine_OnAsyncEngineStarted;
            engine.StartEngine();

            OpenScript = new DelegateCommand(cmd_OpenScript);
            RunScript = new DelegateCommand(cmd_RunScript);
            RunAllSequence = new DelegateCommand(cmd_RunAllSequence);

            SaveScriptEx = new DelegateCommand(cmd_SaveScriptEx);

            GlobalConfig = new DelegateCommand(cmd_GlobalConfig);
            EquipmentConfig = new DelegateCommand(cmd_EquipmentConfig);

            ShowAbout = new DelegateCommand(cmd_ShowAbout);

            Exit = new DelegateCommand(cmd_Exit);

            if (!Directory.Exists(App.TemplateDir))
            {
                Directory.CreateDirectory(App.TemplateDir);
            }

            Templates = new ObservedNest<SampleFileName>();
            Templates.Element = new SampleFileName(App.TemplateDir);
            ListFiles(Templates, App.TemplateDir, "*.approjx");

            if (!App.GroupSetting.Variables.Keys.Contains("DataFolder"))
            {
                App.GroupSetting.Variables.Add("DataFolder", "C:\\ProgramData\\TYMPTE\\Robin");
            }

            if (string.IsNullOrEmpty(App.GroupSetting.Variables["DataFolder"]))
            {
                MessageBox.Show("Variable DataFolder is Null, which may make apx sequence Error on running");
            }
            else
            {
                if (Directory.Exists(App.GroupSetting.Variables["DataFolder"]))
                {
                }
                else
                {
                    Directory.CreateDirectory(App.GroupSetting.Variables["DataFolder"]);
                }
            }

            WipeOutLatestData = new DelegateCommand(cmd_WipeOutLatestData);
            System.IO.FileSystemWatcher watcher = new FileSystemWatcher(App.GroupSetting.Variables["DataFolder"], "*.*");
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;
            watcher.Created += Watcher_Created;
            watcher.Changed += Watcher_Created;
            watcher.EnableRaisingEvents = true;
        }

        bool IsTestAll = false;
        List<string> TestDataFiles = new List<string>();
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            //Dispatcher.Invoke(() => { Message = $"{e.FullPath} {e.ChangeType}"; });

            TestDataFiles.Add(e.FullPath);
        }

        private void cmd_WipeOutLatestData(object obj)
        {
            if(Result is null)
            {
                Message = "No Data Detected, Please Run Test at first";
                return;
            }

            if(IsSequenceIdle == false)
            {
                Message = "Sequence is running, please try when it is idle";
            }

            var files = TestDataFiles.Distinct();

            if (DialogCoordinator.ShowModalMessageExternal(this, "Warning", $"It will delete latest {files.Count()} files which generated by latest test in Data Folder permanently and remove the latest record in appended data if appended data enabled, are you sure", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
            {
                if (AttachResult)
                {
                    if (IsTestAll)
                    {
                        foreach (var dict in AttachResults)
                        {
                            dict.Value.RemoveAt(dict.Value.Count - 1);
                        }
                    }
                    else
                    {
                        var list = AttachResults[ActiveSequence];

                        list.RemoveAt(list.Count - 1);
                    }
                }

                foreach (var tdf in files)
                {
                    try
                    {
                        File.Delete(tdf);
                    }
                    catch (FileNotFoundException) { } // File Might be delete manually or execute multiple time
                    catch (Exception ex)
                    {
                        this.UILog($"Wipe out {tdf} failed. Err: {ex.Message}");
                    }
                }
            }
        }

        private void cmd_ShowAbout(object obj)
        {
            UIs.About about = new About();
            about.ShowDialog();
        }

        private void cmd_Exit(object obj)
        {
            engine.StopEngine();
        }

        private void cmd_SaveScriptEx(object obj)
        {
            try
            {
                IsEditMode = false;
                ScriptEx.Save();
                ApplySequenceSetting(this, ActiveSequence);
                Message = "Save Script Ex OK";
            }
            catch(Exception ex)
            {
                Message = $"Save Script Failed. Err:{ex}";
            }
        }

        private void cmd_EquipmentConfig(object obj)
        {
            UIs.EquipmentConfig ec = new EquipmentConfig(App.HardwareDefinition);
            
            if(ec.ShowDialog() == true)
            {
            }
        }

        private void cmd_GlobalConfig(object obj)
        {
            UIs.GlobalConfig ec = new GlobalConfig() { Robin = this};
            ec.IsEditable = IsEditMode;
            if (ec.ShowDialog() == true)
            {
            }
        }

        public void ListFiles(ObservedNest<SampleFileName> nest, string path, string pattern)
        {
            var dirs = Directory.GetDirectories(path);

            foreach (var subdir in dirs)
            {
                ObservedNest<SampleFileName> subnest = new ObservedNest<SampleFileName>();
                ListFiles(subnest, subdir, pattern);

                if(subnest.Count > 0)
                {
                    subnest.Element = new SampleFileName(subdir);
                    nest.Add(subnest);
                }
            }

            var files = Directory.GetFiles(path, pattern);

            if (files.Length > 0)
            {
                foreach (var file in files)
                {
                    nest.Add(new ObservedNest<SampleFileName>() { Element = new SampleFileName(file) });
                }
            }
        }

        private void Engine_OnAsyncEngineStarted(object sender, EventArgs e)
        {
            Message = "Engine Started";
            ApxVer = ApxEngine.ApRef.Version.SoftwareVersion;
            //engine.Variables.Clear();
            //engine.Variables.Add("SUT_Model", Model);
            //engine.Variables.Add("SUT_ID", DutSn);
            //engine.Variables.Add("VACS_Data", VacsData);
            //engine.Variables.Add("SUT_Model_Option", ModelDescription);
        }

        private void Engine_OnAsyncInitializated(object sender, EventArgs e)
        {
            Message = "Engine Initialized";
        }

        public List<Execution> Executions { get; } = new List<Execution>();

        private void cmd_OpenScript(object obj)
        {
            //var dd = ApxEngine.ApRef.Version.SoftwareVersion;

            if (obj is string path)
            {
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "APx Sequence|*.approjx";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == true)
                {
                    path = ofd.FileName;
                }
                else
                {
                    return;
                }
            }

            if (!File.Exists(path))
            {
                Message = $"{path} does not exist";
                return;
            }

            try
            {
                Message = $"Loading Scripting, Please wait";
                var script = engine.LoadScriptFile(path) as Script;

                ScriptEx = ScriptExtended.FromScript(script);

                if (IsEditMode)
                {
                    script.LockScript(false);
                }

                Dictionary<string, string> ConstVariables = new Dictionary<string, string>();
                ConstVariables.Add("SUT_Model", Model);
                ConstVariables.Add("SUT_Model_Option", ModelDescription);
                ConstVariables.Add("SUT_ID", "");
                ConstVariables.Add("VACS_Data", "Save");

                ApxEngine.SetUserDefinedVariable("ProjectName", script.Name);

                foreach (var variable in ConstVariables)
                {
                    ApxEngine.SetUserDefinedVariable(variable.Key, variable.Value);
                }

                // Apply Global Variable
                foreach (var variable in App.GroupSetting.Variables)
                {
                    ApxEngine.SetUserDefinedVariable(variable.Key, variable.Value);
                }

                foreach (var exec in Executions)
                {
                    exec.Stop();
                    //exec.Dispose();
                }

                Executions.Clear();

                Sequences = new Sequence[script.Sequences.Count];

                for (int i = 0; i < script.Sequences.Count; i++)
                {
                    Sequences[i] = script.Sequences.ElementAt(i) as Sequence;
                }

                ActiveSequence = script.ActiveSequence as Sequence;

                Variables = new ObservableCollection<VM_Variable>();
                foreach (var variable in ActiveExecution.Variables)
                {
                    Variables.Add(new VM_Variable(variable));
                }

                ItemContainsInfinity.Clear();

                Message = $"Load Script OK";
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }

        private void VM_Robin_TestStart(object sender, EventArgs args)
        {
            if (sender is TF_Result rs)
            {
                Dispatcher.Invoke(() =>
                {
                    Result = rs;
                    TestResultUpdated?.Invoke(this, null);
                    Message = $"{rs.SerialNumber} Start Testing";
                });
            }
        }

        public ObservableCollection<VM_Variable> Variables
        {
            get { return (ObservableCollection<VM_Variable>)GetValue(VariablesProperty); }
            set { SetValue(VariablesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Variables.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VariablesProperty =
            DependencyProperty.Register("Variables", typeof(ObservableCollection<VM_Variable>), typeof(VM_Robin), new PropertyMetadata(null));

        private void VM_Robin_TestEnd(object sender, EventArgs args)
        {
            if (sender is TF_Result rs)
            {
                Dispatcher.Invoke(() =>
                {
                    Result = rs;
                    IsSequenceIdle = true;
                    RefreshData(this, StepData);

                    if(rs.Status != TF_TestStatus.ERROR)
                    {
                        Message = $"{rs.SerialNumber} test {rs.Status}";

                        if (AttachResult)
                        {
                            AttachResults[ActiveSequence].Add(rs.Clone() as TF_Result);
                            RefreshAttachData(this, StepData);
                        }
                    }
                    else
                    {
                        Message = $"{rs.SerialNumber} test {rs.Status}: {rs.ErrorMessage}";
                    }

                    TestResultUpdated?.Invoke(this, null);

                    foreach (var variable in Variables)
                    {
                        variable.Value = ActiveExecution.Variables.FirstOrDefault(x => x.Name == variable.Name).Value;
                    }

                    CheckTestReady();
                });
            }
        }

        ApEngine.Execution ActiveExecution { get; set; }

        private void cmd_RunScript(object obj)
        {
            if(TestPrepare("Input Serial Number (Run Sequence)"))
            {
                TestDataFiles.Clear();
                IsTestAll = false;

                ScriptEx.Script.Activate(ActiveSequence);
                ActiveExecution.Results[0].SerialNumber = DutSn;
                if (ActiveExecution.StartNewTest(0) > 0)
                {
                    Result.ProductInfo = $"{Model}_{ModelDescription}";
                    IsSequenceIdle = false;
                }
            }
        }

        private void cmd_RunAllSequence(object obj)
        {
            if (TestPrepare("Input Serial Number (Run All)"))
            {
                TestDataFiles.Clear();
                IsTestAll = true;

                int i = 0;
                int len = Sequences.Length;
                ActiveSequence = null;  // for prevent the sequence had been switched in APx UI && Current sequence is the first one. to force refresh
                Task.Run(() =>
                {
                    do
                    {
                        if (ApxEngine.Mre_Operation.WaitOne(100, false))
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ActiveSequence = Sequences[i];

                                i++;
                                ActiveExecution.Results[0].SerialNumber = DutSn;
                                if (ActiveExecution.StartNewTest(0) > 0)
                                {
                                    Message = $"{DutSn} running in {ActiveSequence.Name}";
                                    Result.ProductInfo = $"{Model}_{ModelDescription}";

                                    if (ScriptEx.MergeSequenceReport && (Sequences.Count() > 1))
                                    {
                                        ActiveExecution.OnPostUUTed += ActiveExecution_OnPostUUTed;
                                    }

                                    IsSequenceIdle = false;
                                }
                            });

                            Thread.Sleep(100);  // delay for Execution Get the AP Global Lock
                        }
                    }
                    while (i < len);
                });
            }
        }

        List<string> tempcsvpaths = new List<string>();
        private void ActiveExecution_OnPostUUTed(object sender, TF_Result e)
        {
            if (sender is Execution exec)
            {
                exec.OnPostUUTed -= ActiveExecution_OnPostUUTed;
                SaveRunAllReport(exec, e);
            }
        }

        private void SaveRunAllReport(Execution exec, TF_Result e)
        {
            var path = $"{Path.GetTempPath()}\\[Robin]{e.SerialNumber}_{exec.Entrypoint.Name}_{e.EndTime.ToString("yyyy-MM-dd_HH-mm-ss")}.csv";
            tempcsvpaths.Add(path);

            ApxEngine.ApRef.Sequence.Report.ExportText(path);

            if (exec.Name == ApxEngine.ApRef.Sequence.Sequences[ApxEngine.ApRef.Sequence.Sequences.Count - 1].Name)
            {
                var merge = $"{App.GroupSetting.Variables["DataFolder"]}\\{e.AttachProperties["Model"]}\\{e.AttachProperties["Model_Option"]}\\{exec.Script.Name}\\[Robin]{e.SerialNumber}_Merged_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.csv";

                try
                {
                    using (StreamWriter sw = new StreamWriter(merge))
                    {
                        foreach (var p in tempcsvpaths)
                        {
                            using (StreamReader sr = new StreamReader(p))
                            {
                                sw.Write(sr.ReadToEnd());
                            }

                            sw.WriteLine();

                            File.Delete(p);
                        }

                        sw.Flush();
                    }
                }
                finally
                {
                    tempcsvpaths.Clear();
                }
            }
        }

        bool TestPrepare(string title = "Input SerialNumber")
        {
            if (ActiveExecution is null) return false;

            if (ActiveExecution.IsTesting)
            {
                Message = "Execution is Busy, Action Ignored";
                DialogCoordinator.ShowModalMessageExternal(this, "Action Denied", $"Execution is Busy. Please run later", MessageDialogStyle.Affirmative);
                return false;
            }

            if (ScriptEx?.ShowInputSn == true)
            {
                InputSn input = new InputSn() { Title = title, WindowStartupLocation = WindowStartupLocation.CenterOwner, Model = Model, ModelDescription=ModelDescription, AttachResult = AttachResult, VacsData = VacsData };
                if (input.ShowDialog() == true)
                {
                    DutSn = input.SerialNumber;
                    Model = input.Model;
                    ModelDescription = input.ModelDescription;
                    AttachResult = input.AttachResult;
                    //VacsData = input.VacsData;
                }
                else
                {
                    Message = "Input SN is Cancelled";
                    return false;
                }
            }

            VacsData = IsVacs ? "Save" : String.Empty;

            if (string.IsNullOrEmpty(Model) || string.IsNullOrEmpty(DutSn) || string.IsNullOrEmpty(ModelDescription))
            {
                DialogCoordinator.ShowModalMessageExternal(this, "Action Denied", $"Model: {Model}, Dut SN {DutSn}, Description {ModelDescription} - might be illegal", MessageDialogStyle.Affirmative);

                return false;
            }

            if (ScriptEx?.CheckStartReady == true)
            {
                if (CheckTestReady() == false)
                {
                    Message = "Hardware is not ready, Action Denied";
                    DialogCoordinator.ShowModalMessageExternal(this, "Action Denied", $"Hardware is not ready", MessageDialogStyle.Affirmative);

                    return false;
                }
            }

            ApxEngine.SetUserDefinedVariable("SUT_Model", Model);
            ApxEngine.SetUserDefinedVariable("SUT_ID", DutSn);
            ApxEngine.SetUserDefinedVariable("VACS_Data", VacsData);
            ApxEngine.SetUserDefinedVariable("SUT_Model_Option", ModelDescription);

            Result.AttachProperties["Model"] = Model;
            Result.AttachProperties["Model_Option"] = ModelDescription;

            Message = "Script is Running, please Wait";
            return true;
        }

        bool CheckTestReady()
        {
            var auxin = ApxEngine.ApRef.AuxControlMonitor.AuxControlInputValue;
            AuxInRefreshed?.Invoke(this, auxin);

            return (auxin & App.HardwareDefinition.ReadyMask) == (App.HardwareDefinition.ReadyValue & App.HardwareDefinition.ReadyMask);
        }
    }

    public class SampleFileName
    {
        public string Name { get; }
        public string Path { get; }

        public SampleFileName(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(Path);
        }
    }

    public class VM_Variable : DependencyObject
    {
        public Variable Variable { get; }

        public string Name { get; }

        public string Type { get; }

        public object Value
        {
            get { return (object)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(VM_Variable), new PropertyMetadata(null, ValueChanged));

        private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is null) return;

            if (d is VM_Variable vm)
            {
                if (e.NewValue is string str)
                {
                    ApxEngine.SetUserDefinedVariable(vm.Name, str);
                }
            }
        }

        public VM_Variable(Variable variable)
        {
            Variable = variable;
            Name = variable.Name;
            Type = variable.Type;
            Value = variable.Value;
        }
    }

    public class AuxOut:DependencyObject
    {
        public int Index { get; }
        public string Name { get; }
        public string IOName { get; }
        public bool State { get; }

        public AuxOut(int idx, string name, bool state = false)
        {
            Index = idx;
            Name = name;
            IOName = $"AuxOut{idx}";
            State = state;
        }
    }

    public class HardwareControl : System.Xml.Serialization.IXmlSerializable
    {
        public int CalibValidDay { get; internal set; } = -1;

        public Dictionary<string, byte> ControlStates { get; } = new Dictionary<string, byte>();
        /// <summary>
        /// AuxOut Index for DoorCtrl
        /// </summary>
        public int DoorCtrl { get; internal set; } = -1;

        /// <summary>
        /// AuxOut Index for DutCtrl
        /// </summary>
        public int JigCtrl { get; internal set; } = -1;

        /// <summary>
        /// AuxOut Index for DutCtrl
        /// </summary>
        public int DutCtrl { get; internal set; } = -1;

        public byte ControlMask { get; internal set; } = 0xFF;
        public byte ReadyMask { get; internal set; } = 0x00;
        public byte ReadyValue { get; internal set; } = 0x00;

        public Dictionary<string, byte> InputNames { get; } = new Dictionary<string, byte>();

        public HardwareControl()
        {
        }

        public static HardwareControl FromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(HardwareControl));

            using (StreamReader sr = new StreamReader(path))
            {
                var obj = serializer.Deserialize(sr) as HardwareControl;

                return obj;
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool isEmpty = reader.IsEmptyElement;

            if (isEmpty) return;

            if(reader.GetAttribute("calibvalidday") is string str)
            {
                if (int.TryParse(str, out int day))
                {
                    CalibValidDay = day;
                }
            }

            reader.Read();
            if (reader.Name == "ControlStates")
            {
                if (byte.TryParse(reader.GetAttribute("mask"), out byte bval))
                {
                    ControlMask = bval;
                }

                if (int.TryParse(reader.GetAttribute("doorctrl"), out int val1))
                {
                    DoorCtrl = val1;
                }

                if (int.TryParse(reader.GetAttribute("jigctrl"), out int val2))
                {
                    JigCtrl = val2;
                }

                if (int.TryParse(reader.GetAttribute("dutctrl"), out int val3))
                {
                    DutCtrl = val3;
                }

                ControlStates.Clear();
                reader.MoveToContent();
                reader.Read();
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    ControlStates.Add(reader.GetAttribute("key"), byte.Parse(reader.GetAttribute("val")));
                    reader.Read();
                }
                reader.MoveToElement();
                reader.Read();
            }

            if (reader.Name == "InputNames")
            {
                if (byte.TryParse(reader.GetAttribute("readymask"), out byte bval))
                {
                    ReadyMask = bval;
                }
                if (byte.TryParse(reader.GetAttribute("readyvalue"), out bval))
                {
                    ReadyValue = bval;
                }

                InputNames.Clear();
                reader.MoveToContent();
                reader.Read();
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    InputNames.Add(reader.GetAttribute("key"), byte.Parse(reader.GetAttribute("val")));
                    reader.Read();
                }
                reader.MoveToElement();
                reader.Read();
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            if(CalibValidDay > 0)
            {
                writer.WriteAttributeString("calibvalidday", CalibValidDay.ToString());
            }
            writer.WriteStartElement("ControlStates");
            writer.WriteAttributeString("mask", ControlMask.ToString());
            writer.WriteAttributeString("doorctrl", DoorCtrl.ToString());
            writer.WriteAttributeString("jigctrl", JigCtrl.ToString());
            writer.WriteAttributeString("dutctrl", DutCtrl.ToString());
            foreach (var kpv in ControlStates)
            {
                writer.WriteStartElement("State");
                writer.WriteAttributeString("key", kpv.Key);
                writer.WriteAttributeString("val", kpv.Value.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("InputNames");
            writer.WriteAttributeString("readymask", ReadyMask.ToString());
            writer.WriteAttributeString("readyvalue", ReadyValue.ToString());
            foreach (var kpv in InputNames)
            {
                writer.WriteStartElement("State");
                writer.WriteAttributeString("key", kpv.Key);
                writer.WriteAttributeString("val", kpv.Value.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public int Save(string path)
        {
            File.WriteAllText(path, XmlSerializerHelper.Serialize(this));

            return 1;
        }
    }
}
