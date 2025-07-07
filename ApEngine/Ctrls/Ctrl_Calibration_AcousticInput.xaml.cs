using ApEngine.Base;
using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;
using TestCore;

namespace ApEngine.Ctrls
{
    /// <summary>
    /// Interaction logic for Ctrl_Calibration_AcousticInput.xaml
    /// </summary>
    public partial class Ctrl_Calibration_AcousticInput : DockPanel
    {
        public static Array ConnectorTypes = Enum.GetValues(typeof(AudioPrecision.API.InputConnectorType));

        public Calib_AcousticInput AcousticInput
        {
            get { return (Calib_AcousticInput)GetValue(AcousticInputProperty); }
            set { SetValue(AcousticInputProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AcousticInput.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AcousticInputProperty =
            DependencyProperty.Register("AcousticInput", typeof(Calib_AcousticInput), typeof(Ctrl_Calibration_AcousticInput), new PropertyMetadata(null));

        public ObservableCollection<VM_AcousticInputChannel> ChannelsData
        {
            get { return (ObservableCollection<VM_AcousticInputChannel>)GetValue(ChannelsDataProperty); }
            set { SetValue(ChannelsDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChannelsDataProperty =
            DependencyProperty.Register("ChannelsData", typeof(ObservableCollection<VM_AcousticInputChannel>), typeof(Ctrl_Calibration_AcousticInput), new PropertyMetadata(new ObservableCollection<VM_AcousticInputChannel>()));

        System.Timers.Timer timer = new System.Timers.Timer(250);
        public Ctrl_Calibration_AcousticInput()
        {
            InitializeComponent();

//#if AP8
//                    var cnt = ApxEngine.ApRef.SignalPathSetup.Channels.Count;

//                    var lch = new Calib_AcousticInputChannel[cnt];
//                    ChannelsData.Clear();
//                    for (int i = 0; i < cnt; i++)
//                    {
//                        lch[i] = new Calib_AcousticInputChannel() { Index = i };
//                        lch[i].SerialNo = ApxEngine.ApRef.SignalPathSetup.Channels[i].SerialNumber;
//                        lch[i].Sensitivity_Expected = ApxEngine.ApRef.SignalPathSetup.Channels[i].ExpectedSensitivity.Value;
//                        lch[i].Sensitivity = ApxEngine.ApRef.SignalPathSetup.Channels[i].Sensitivity.Value;
//                        lch[i].Sensitivity_Tolerance = ApxEngine.ApRef.SignalPathSetup.Channels[i].SensitivityTolerance.Value;

//                        ChannelsData.Add(new VM_AcousticInputChannel(lch[i]));
//                    }
//#else
//            var cnt = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.Count;

//            var lch = new Calib_AcousticInputChannel[cnt];
//            ChannelsData.Clear();
//            for (int i = 0; i < cnt; i++)
//            {
//                lch[i] = new Calib_AcousticInputChannel() { Index = i };
//                lch[i].SerialNo = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetSerialNum(i);
//                lch[i].Sensitivity_Expected = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetExpectedSensitivity(i) * Calib_AcousticInputChannel.SensitivityUnitIndex;
//                lch[i].Sensitivity = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetSensitivity(i) * Calib_AcousticInputChannel.SensitivityUnitIndex;
//                //var ddd = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorLevel.TextWithReferenceValue;

//                ChannelsData.Add(new VM_AcousticInputChannel(lch[i]));
//            }
//#endif

            timer.Elapsed += Timer_Elapsed;

            this.GotFocus += Ctrl_Calibration_AcousticInput_GotFocus;
            LostFocus += Ctrl_Calibration_AcousticInput_LostFocus;

            if (IsFocused) Ctrl_Calibration_AcousticInput_GotFocus(this, null);

            DataContextChanged += Ctrl_Calibration_AcousticInput_DataContextChanged;
        }

        private void Ctrl_Calibration_AcousticInput_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Calib_AcousticInput calib)
            {
                AcousticInput = calib;

#if AP8
                if (calib.Level is null)
                {
                    calib.Level = ApxEngine.ApRef.SignalPathSetup.Channels.CalibratorLevel.Text;
                    calib.Frequency = ApxEngine.ApRef.SignalPathSetup.Channels.CalibratorFrequency;
                    calib.Tolerance = ApxEngine.ApRef.SignalPathSetup.Channels.CalibratorFrequencyTolerance;

                    calib.Channels = ChannelsData.Select(x => x.ChannelData.Clone() as Calib_AcousticInputChannel).ToList();
                }
                else
                {
                    ApxEngine.ApRef.SignalPathSetup.Channels.CalibratorLevel.Text = calib.Level;
                    ApxEngine.ApRef.SignalPathSetup.Channels.CalibratorFrequency = calib.Frequency;
                    ApxEngine.ApRef.SignalPathSetup.Channels.CalibratorFrequencyTolerance = calib.Tolerance;
                }

                var cnt = ApxEngine.ApRef.SignalPathSetup.Channels.Count;
                //var cnt = Math.Min(ChannelsData.Count, calib.Channels.Count());

                ChannelsData.Clear();
                for (int i = 0; i < calib.Channels.Count; i++)
                {
                    var ch = calib.Channels.ElementAt(i);
                    ChannelsData.Add(new VM_AcousticInputChannel(ch));
                }

                for (int i = calib.Channels.Count; i < cnt; i++)
                {
                    var ch = new Calib_AcousticInputChannel() { Index = i };
                    ch.SerialNo = ApxEngine.ApRef.SignalPathSetup.Channels[i].SerialNumber;
                    ch.Sensitivity_Expected = ApxEngine.ApRef.SignalPathSetup.Channels[i].ExpectedSensitivity.Value * Calib_AcousticInputChannel.SensitivityUnitIndex;
                    ch.Sensitivity = ApxEngine.ApRef.SignalPathSetup.Channels[i].Sensitivity.Value * Calib_AcousticInputChannel.SensitivityUnitIndex;
                    //var ddd = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorLevel.TextWithReferenceValue;

                    calib.Channels.Add(ch);

                    ChannelsData.Add(new VM_AcousticInputChannel(ch));
                }
#else
                if (calib.Level is null)
                {
                    calib.Level = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorLevel.Text;
                    calib.Frequency = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorFrequency;
                    calib.Tolerance = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorFrequencyTolerance;

                    calib.Channels = ChannelsData.Select(x => x.ChannelData.Clone() as Calib_AcousticInputChannel).ToList();
                }
                else
                {
                    ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorLevel.Text = calib.Level;
                    ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorFrequency = calib.Frequency;
                    ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorFrequencyTolerance = calib.Tolerance;
                }

                var cnt = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.Count;
                //var cnt = Math.Min(ChannelsData.Count, calib.Channels.Count());

                ChannelsData.Clear();
                for (int i = 0; i < calib.Channels.Count; i++)
                {
                    var ch = calib.Channels.ElementAt(i);
                    ChannelsData.Add(new VM_AcousticInputChannel(ch));
                }

                for (int i = calib.Channels.Count; i < cnt; i++)
                {
                    var ch = new Calib_AcousticInputChannel() { Index = i };
                    ch.SerialNo = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetSerialNum(i);
                    ch.Sensitivity_Expected = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetExpectedSensitivity(i) * Calib_AcousticInputChannel.SensitivityUnitIndex;
                    ch.Sensitivity = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetSensitivity(i) * Calib_AcousticInputChannel.SensitivityUnitIndex;
                    //var ddd = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.CalibratorLevel.TextWithReferenceValue;

                    calib.Channels.Add(ch);

                    ChannelsData.Add(new VM_AcousticInputChannel(ch));
                }
#endif
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var vald = ApxEngine.ApRef.SignalPathSetup.Level.GetValues();
            var val = ApxEngine.ApRef.SignalPathSetup.Level.GetText();

#if AP8
                    var len = Math.Min(ApxEngine.ApRef.SignalPathSetup.Channels.Count, val.Length);
#else
            var len = Math.Min(ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.Count, val.Length);
#endif
            //ApxEngine.ApRef.SignalPathSetup.InputConnector.Type = AudioPrecision.API.InputConnectorType.AnalogBalanced;
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < len; i++)
                {
                    ChannelsData[i].Level = val[i];
                }
            });
        }

        private void Ctrl_Calibration_AcousticInput_LostFocus(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void Ctrl_Calibration_AcousticInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (((Calib_AcousticInput)DataContext)?.ConnectorType is AudioPrecision.API.InputConnectorType type)
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
#if AP8
                        if (ApxEngine.ApRef.SignalPathSetup.Measure != MeasurandType.Acoustic) ApxEngine.ApRef.SignalPathSetup.Measure = MeasurandType.Acoustic;
#else
                if (!ApxEngine.ApRef.SignalPathSetup.AcousticInput) ApxEngine.ApRef.SignalPathSetup.AcousticInput = true;
#endif
                timer.Start();
            }
        }
    }

    public class VM_AcousticInputChannel : DependencyObject
    {
        public int Index { get => ChannelData.Index; }
        public string Channel { get=>ChannelData.Channel; }

        public string Level
        {
            get { return (string)GetValue(LevelProperty); }
            set { SetValue(LevelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Level.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LevelProperty =
            DependencyProperty.Register("Level", typeof(string), typeof(VM_AcousticInputChannel), new PropertyMetadata(null));

        public string SerialNo
        {
            get { return (string)GetValue(SerialNoProperty); }
            set { SetValue(SerialNoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNoProperty =
            DependencyProperty.Register("SerialNo", typeof(string), typeof(VM_AcousticInputChannel), new PropertyMetadata(null, SerialNoChanged));

        private static void SerialNoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_AcousticInputChannel vm)
            {
                vm.ChannelData.SerialNo = (string)e.NewValue;
            }
        }

        //public double Sensitivity { get => ChannelData.Sensitivity; set { ChannelData.Sensitivity = value; } }
        public double Sensitivity_Expected { get => ChannelData.Sensitivity_Expected; set { ChannelData.Sensitivity_Expected = value; } }
        public double Sensitivity_Tolerance { get => ChannelData.Sensitivity_Tolerance; set { ChannelData.Sensitivity_Tolerance = value; } }
        public double Sensitivity
        {
            get { return (double)GetValue(SensitivityProperty); }
            set { SetValue(SensitivityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Sensitivity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SensitivityProperty =
            DependencyProperty.Register("Sensitivity", typeof(double), typeof(VM_AcousticInputChannel), new PropertyMetadata(double.NaN, SensitivityChanged));

        private static void SensitivityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_AcousticInputChannel vm)
            {
                if (e.NewValue is double)
                {
                    vm.ChannelData.Sensitivity = (double)e.NewValue;
                }
            }
        }

        //public double Sensitivity => ChannelData.Sensitivity;
        //public double Frequency { get; set; } = 1000; // Hz
        //public double Sensitivity_Expected { get; set; } = 10; //mv/Pa
        //public double Sensitivity_Tolerance { get; set; } = 1; // dB

        public Calib_AcousticInputChannel ChannelData { get; }

        public DelegateCommand Calibration { get; }
        public VM_AcousticInputChannel(Calib_AcousticInputChannel channel)
        {
            ChannelData = channel;
            Sensitivity = channel.Sensitivity;
            Calibration = new DelegateCommand(cmd_Calibration);
        }

        private void cmd_Calibration(object obj)
        {
#if AP8
            ApxEngine.ApRef.SignalPathSetup.Channels[Index].Calibrate();
            Sensitivity = ApxEngine.ApRef.SignalPathSetup.Channels[Index].Sensitivity.Value * Calib_AcousticInputChannel.SensitivityUnitIndex;
#else
            ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.Calibrate(Index);
            Sensitivity = ApxEngine.ApRef.SignalPathSetup.References.AcousticInputReferences.GetSensitivity(Index) * Calib_AcousticInputChannel.SensitivityUnitIndex;
#endif
        }
    }
}
