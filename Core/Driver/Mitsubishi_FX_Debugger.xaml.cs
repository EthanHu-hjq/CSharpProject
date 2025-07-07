using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
using static ToucanCore.Driver.PLC_Mitsubishi_FX;

namespace ToucanCore.Driver
{
    /// <summary>
    /// Mitsubishi_FX_Debugger.xaml 的交互逻辑
    /// </summary>
    public partial class Mitsubishi_FX_Debugger : Window
    {
//#if DEBUG
//        public static string[] Ports { get; } = new string[] {"COM1", "COM2"};
//#else
        public static string[] Ports { get; } = System.IO.Ports.SerialPort.GetPortNames();
//#endif
        public static Array Commands { get; } = Enum.GetValues(typeof(PLC_Mitsubishi_FX.RegisterAction));



        public PLC_Mitsubishi_FX.RegisterAction Command
        {
            get { return (PLC_Mitsubishi_FX.RegisterAction)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Command.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register("Command", typeof(PLC_Mitsubishi_FX.RegisterAction), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(PLC_Mitsubishi_FX.RegisterAction.Read, SendDataChanged));


        public string Register
        {
            get { return (string)GetValue(RegisterProperty); }
            set { SetValue(RegisterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Register.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RegisterProperty =
            DependencyProperty.Register("Register", typeof(string), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(null, SendDataChanged));


        public int DataLength
        {
            get { return (int)GetValue(DataLengthProperty); }
            set { SetValue(DataLengthProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DataLength.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataLengthProperty =
            DependencyProperty.Register("DataLength", typeof(int), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(2, SendDataChanged));

        private static void SendDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is Mitsubishi_FX_Debugger ctrl)
            {
                if (ctrl.DataLength < 0) return;

                try
                {
                    var re = PLC_Mitsubishi_FX.RE_RegisterName.Match(ctrl.Register);
                    if (re.Success)
                    {
                        var type = (RegisterType)(Enum.Parse(typeof(RegisterType), re.Groups[1].Value));

                        if (ctrl.Command == RegisterAction.Write)
                        {
                            if (string.IsNullOrEmpty(ctrl.WriteData)) return;

                            ctrl.WriteBytes = ctrl.WriteData.Trim().Split(' ').Select(x => byte.Parse(x, System.Globalization.NumberStyles.HexNumber)).ToArray();
                        }
                        ctrl.SendBytes = PLC_Mitsubishi_FX.Encode(type, ctrl.Command, short.Parse(re.Groups[2].Value), ctrl.DataLength, ctrl.WriteBytes);
                        ctrl.SendData = string.Join(" ", ctrl.SendBytes.Select(x => x.ToString("X02")));
                    }
                }
                catch
                {

                }
            }
        }

        public string SendData
        {
            get { return (string)GetValue(SendDataProperty); }
            set { SetValue(SendDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SendData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SendDataProperty =
            DependencyProperty.Register("SendData", typeof(string), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(null));


        public string WriteData
        {
            get { return (string)GetValue(WriteDataProperty); }
            set { SetValue(WriteDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WriteData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WriteDataProperty =
            DependencyProperty.Register("WriteData", typeof(string), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(null, SendDataChanged));

        public byte[] SendBytes { get; set; }
        public byte[] WriteBytes { get; set; } = new byte[0];

        public string LogData
        {
            get { return (string)GetValue(LogDataProperty); }
            set { SetValue(LogDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RecieveData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LogDataProperty =
            DependencyProperty.Register("LogData", typeof(string), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(null));



        public string Resource
        {
            get { return (string)GetValue(ResourceProperty); }
            set { SetValue(ResourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Resource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register("Resource", typeof(string), typeof(Mitsubishi_FX_Debugger), new PropertyMetadata(null, ResourceChanged));

        private static void ResourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        PLC_Mitsubishi_FX PLC = new PLC_Mitsubishi_FX();

        public Mitsubishi_FX_Debugger()
        {
            InitializeComponent();
            Closed += Mitsubishi_FX_Debugger_Closed;
        }

        private void Mitsubishi_FX_Debugger_Closed(object sender, EventArgs e)
        {
            PLC?.Close();
            PLC?.Clear();
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Resource)) return;

            if(PLC.Resource != Resource)
            {
                PLC.Close();
                PLC.Resource = Resource;
                PLC.Initialize();
            }
            
            if(PLC.IsOpen)
            {
                PLC.Close();
                LogData += $"{DateTime.Now.ToShortTimeString()}: PLC {PLC.Resource} Closed\r\n";
            }
            else
            {
                PLC.Open();
                LogData += $"{DateTime.Now.ToShortTimeString()}: PLC {PLC.Resource} Opened\r\n";
            }
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if(SendBytes.Length > 0)
            {
                //PLC.ReadRegister(RegisterType.D, 250, 16, out byte[] data, out bool state);

                //PLC.WriteRegister(RegisterType.D, 571, new byte[] { 01 });

                //PLC.ReadRegister(RegisterType.D, 571, 1, out byte[] data1, out bool state1);

                try
                {
                    PLC.Port.Open();
                    Thread.Sleep(10);

                    PLC.Port.Write(SendBytes, 0, SendBytes.Length);

                    Thread.Sleep(100);

                    var bytes = Encoding.UTF8.GetBytes(PLC.Port.ReadExisting());
                    LogData += $"{DateTime.Now.ToShortTimeString()} recv: {string.Join(" ", bytes.Select(x => x.ToString("X02")))}. Ascii {Encoding.Default.GetString(bytes)}\r\n";
                }
                finally
                {
                    PLC.Port.Close();
                }
            }
            
        }
    }
}
