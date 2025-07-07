using ApEngine.Base;
using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TestCore;

#pragma warning disable 0619

namespace ApEngine.Ctrls
{
    /// <summary>
    /// Interaction logic for Ctrl_Calibration_AnalogInput.xaml
    /// </summary>
    public partial class Ctrl_Calibration_AnalogInput : DockPanel
    {


        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Unit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata("Vrms", UnitChanged));

        private static void UnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is string unit)
            {
                ApxEngine.ApRef.SignalPathSetup.Level.Axis.Unit = unit;
            }
        }

        System.Timers.Timer timer = new System.Timers.Timer(250);

        Calib_AnalogInput AnalogInput { get; set; }

        public ObservableCollection<VM_AnalogInputChannel> ChannelsData
        {
            get { return (ObservableCollection<VM_AnalogInputChannel>)GetValue(ChannelsDataProperty); }
            set { SetValue(ChannelsDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ChannelsData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChannelsDataProperty =
            DependencyProperty.Register("ChannelsData", typeof(ObservableCollection<VM_AnalogInputChannel>), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(new ObservableCollection<VM_AnalogInputChannel>()));

        public string dBrA
        {
            get { return (string)GetValue(dBrAProperty); }
            set { SetValue(dBrAProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBrA.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBrAProperty =
            DependencyProperty.Register("dBrA", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBrAChanged));
        private static void dBrAChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBrA = e.NewValue as string;
        }


        public string dBrAOffset
        {
            get { return (string)GetValue(dBrAOffsetProperty); }
            set { SetValue(dBrAOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBrAOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBrAOffsetProperty =
            DependencyProperty.Register("dBrAOffset", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBrAOffsetChanged));
        private static void dBrAOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBrAOffset = e.NewValue as string;
        }

        public string dBrB
        {
            get { return (string)GetValue(dBrBProperty); }
            set { SetValue(dBrBProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBrB.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBrBProperty =
            DependencyProperty.Register("dBrB", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBrBChanged));
        private static void dBrBChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBrB = e.NewValue as string;
        }
        public string dBrBOffset
        {
            get { return (string)GetValue(dBrBOffsetProperty); }
            set { SetValue(dBrBOffsetProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBrBOffset.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBrBOffsetProperty =
            DependencyProperty.Register("dBrBOffset", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBrBOffsetChanged));
        private static void dBrBOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBrBOffset = e.NewValue as string;
        }
        public string dBSPL1
        {
            get { return (string)GetValue(dBSPL1Property); }
            set { SetValue(dBSPL1Property, value); }
        }

        // Using a DependencyProperty as the backing store for dBSPL1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBSPL1Property =
            DependencyProperty.Register("dBSPL1", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBSPL1Changed));

        private static void dBSPL1Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBSpl1 = e.NewValue as string;
        }

        public string dBSPL2
        {
            get { return (string)GetValue(dBSPL2Property); }
            set { SetValue(dBSPL2Property, value); }
        }

        // Using a DependencyProperty as the backing store for dBSPL2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBSPL2Property =
            DependencyProperty.Register("dBSPL2", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBSPL2Changed));

        private static void dBSPL2Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBSpl2 = e.NewValue as string;
        }

        public string dBSPL1_CalibratorLevel
        {
            get { return (string)GetValue(dBSPL1_CalibratorLevelProperty); }
            set { SetValue(dBSPL1_CalibratorLevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBSPL1_CalibratorLevel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBSPL1_CalibratorLevelProperty =
            DependencyProperty.Register("dBSPL1_CalibratorLevel", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBSPL1CalibLevelChanged));

        private static void dBSPL1CalibLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBSpl1CalibratorLevel = e.NewValue as string;
        }

        public string dBSPL2_CalibratorLevel
        {
            get { return (string)GetValue(dBSPL2_CalibratorLevelProperty); }
            set { SetValue(dBSPL2_CalibratorLevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBSPL2_CalibratorLevel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBSPL2_CalibratorLevelProperty =
            DependencyProperty.Register("dBSPL2_CalibratorLevel", typeof(string), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(null, dBSPL2CalibLevelChanged));

        private static void dBSPL2CalibLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Ctrl_Calibration_AnalogInput ctrl) ctrl.AnalogInput.dBSpl2CalibratorLevel = e.NewValue as string;
        }

        //public AudioPrecision.API.InputConnectorType ConnectorType
        //{
        //    get { return (AudioPrecision.API.InputConnectorType)GetValue(ConnectorTypeProperty); }
        //    set { SetValue(ConnectorTypeProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for ConnectorType.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty ConnectorTypeProperty =
        //    DependencyProperty.Register("ConnectorType", typeof(AudioPrecision.API.InputConnectorType), typeof(Ctrl_Calibration_AnalogInput), new PropertyMetadata(AudioPrecision.API.InputConnectorType.AnalogBalanced, ConnectorTypeChanged));

        //private static void ConnectorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is Ctrl_Calibration_AnalogInput vm)
        //    {
        //        var type = ApxEngine.ApRef.SignalPathSetup.InputConnector.Type;
        //        var newval = ((AudioPrecision.API.InputConnectorType)e.NewValue);
        //        if (newval!= type)
        //        {
        //            ApxEngine.ApRef.SignalPathSetup.InputConnector.Type = newval;
        //        }
        //    }
        //}

        public Ctrl_Calibration_AnalogInput()
        {
            InitializeComponent();

            var cnt = ApxEngine.ApRef.SignalPathSetup.Level.ChannelCount;

            ChannelsData.Clear();
            for (int i = 0; i < cnt; i++)
            {
                ChannelsData.Add(new VM_AnalogInputChannel(i, cmd_SetDbra, cmd_SetDbrb, cmd_SetDbspl1, cmd_SetDbspl2));
            }

            timer.Elapsed += Timer_Elapsed;

            this.GotFocus += Ctrl_Calibration_AnalogInput_GotFocus;
            LostFocus += Ctrl_Calibration_AnalogInput_LostFocus;

            if (IsFocused) Ctrl_Calibration_AnalogInput_GotFocus(this, null);

            DataContextChanged += Ctrl_Calibration_AnalogInput_DataContextChanged;
        }

        private void Ctrl_Calibration_AnalogInput_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Calib_AnalogInput calib)
            {
                if (calib.dBSpl1 is null && calib.dBrA is null)
                {
                    calib.dBrAOffset = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrAOffset.Text;
                    calib.dBrBOffset = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrBOffset.Text;
                    calib.dBSpl1CalibratorLevel = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text;
                    calib.dBSpl2CalibratorLevel = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text;

                    calib.dBrA = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrA.Text;
                    calib.dBrB = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrB.Text;
                    calib.dBSpl1 = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl1.Text;
                    calib.dBSpl2 = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl2.Text;

                    calib.dBm = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBm.Text;
                    calib.Watts = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.Watts.Text;
                    calib.Unit = ApxEngine.ApRef.SignalPathSetup.Level.Axis.Unit;
                }
                else
                {
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrAOffset.Text = calib.dBrAOffset;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrBOffset.Text = calib.dBrBOffset;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text = calib.dBSpl1CalibratorLevel;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text = calib.dBSpl2CalibratorLevel;

                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrA.Text = calib.dBrA;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrB.Text = calib.dBrB;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl1.Text = calib.dBSpl1;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl2.Text = calib.dBSpl2;

                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBm.Text = calib.dBm;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.Watts.Text = calib.Watts;
                    //ApxEngine.ApRef.SignalPathSetup.Level.Axis.Unit = calib.Unit;
                }
                AnalogInput = calib;

                dBSPL1 = calib.dBSpl1;
                dBSPL2 = calib.dBSpl2;
                dBrA = calib.dBrA;
                dBrB = calib.dBrB;
                dBSPL1_CalibratorLevel = calib.dBSpl1CalibratorLevel;
                dBSPL2_CalibratorLevel = calib.dBSpl2CalibratorLevel;
                dBrAOffset = calib.dBrAOffset;
                dBrBOffset = calib.dBrBOffset;

                if (Ctrls.CtrlStatic.UnitList.Contains(calib.Unit))
                {
                    Unit = calib.Unit;
                }
                else
                {
                    Unit = calib.Unit = CtrlStatic.UnitList?.FirstOrDefault();
                }
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var vald = ApxEngine.ApRef.SignalPathSetup.Level.GetValues();
                var val = ApxEngine.ApRef.SignalPathSetup.Level.GetText();
                //var len = Math.Min(ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.Count, val.Length);
                var len = Math.Min(ApxEngine.ApRef.SignalPathSetup.InputChannelCount, val.Length);
                for (int i = 0; i < len; i++)
                {
                    ChannelsData[i].Level = val[i];
                }
            });
        }

        private void Ctrl_Calibration_AnalogInput_LostFocus(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void Ctrl_Calibration_AnalogInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((Calib_AnalogInput)(DataContext))?.ConnectorType is AudioPrecision.API.InputConnectorType type)
            {
                if (ApxEngine.ApRef.ActiveMeasurement is ISignalPathSetup)
                {
                }
                else
                {
                    var d = ApxEngine.ApRef.Sequence.GetSignalPath(0).GetMeasurement(0).Name;
                    ApxEngine.ApRef.Sequence.GetSignalPath(0).GetMeasurement(0).Show();
                }

                if (ApxEngine.ApRef.SignalPathSetup.InputConnector.Type != type)
                {
                    ApxEngine.ApRef.SignalPathSetup.InputConnector.Type = type;
                }

                //if (ApxEngine.ApRef.SignalPathSetup.AcousticInput) ApxEngine.ApRef.SignalPathSetup.AcousticInput = false;
                if (ApxEngine.ApRef.SignalPathSetup.Measure != MeasurandType.Voltage) ApxEngine.ApRef.SignalPathSetup.Measure = MeasurandType.Voltage;
                timer.Start();
            }
        }

        private void cmd_SetDbspl1(object obj)
        {
            if (obj is int index)
            {
                ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.SetReferenceFromInput(AudioPrecision.API.DbReferenceType.dBSpl1, index);
                dBSPL1 = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl1.Text;
            }
        }

        private void cmd_SetDbspl2(object obj)
        {
            if (obj is int index)
            {
                ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.SetReferenceFromInput(AudioPrecision.API.DbReferenceType.dBSpl2, index);
                dBSPL2 = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBSpl2.Text;
            }
        }

        private void cmd_SetDbra(object obj)
        {
            if (obj is int index)
            {
                ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.SetReferenceFromInput(AudioPrecision.API.DbReferenceType.dBrA, index);
                dBrA = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrA.Text;
            }
        }

        private void cmd_SetDbrb(object obj)
        {
            if (obj is int index)
            {
                //ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.Calibrate(Index);
                ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.SetReferenceFromInput(AudioPrecision.API.DbReferenceType.dBrB, index);
                dBrB = ApxEngine.ApRef.SignalPathSetup.References.AnalogInputReferences.dBrB.Text;
            }
        }
    }

    public class VM_AnalogInputChannel : DependencyObject
    {
        public DelegateCommand SetDbspl1 { get; }
        public DelegateCommand SetDbspl2 { get; }
        public DelegateCommand SetDbra { get; }
        public DelegateCommand SetDbrb { get; }

        public int Index { get; }
        public string Channel { get => $"CH{Index + 1}"; }

        public string Level
        {
            get { return (string)GetValue(LevelProperty); }
            set { SetValue(LevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Level.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register("Level", typeof(string), typeof(VM_AnalogInputChannel), new PropertyMetadata(null));



        //Calib_AnalogInputChannel ChannelData;
        public VM_AnalogInputChannel(int index, Action<object> cmd_SetDbra, Action<object> cmd_SetDbrb, Action<object> cmd_SetDbspl1, Action<object> cmd_SetDbspl2)
        {
            Index = index;

            SetDbra = new DelegateCommand(cmd_SetDbra);
            SetDbrb = new DelegateCommand(cmd_SetDbrb);
            SetDbspl1 = new DelegateCommand(cmd_SetDbspl1);
            SetDbspl2 = new DelegateCommand(cmd_SetDbspl2);
        }
    }
}
