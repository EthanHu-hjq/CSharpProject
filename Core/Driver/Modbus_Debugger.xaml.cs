using NModbus;
using NModbus.Device;
using NModbus.IO;
using NModbus.Serial;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
using ToucanCore.HAL;

namespace ToucanCore.Driver
{
    /// <summary>
    /// Modbus_Debugger.xaml 的交互逻辑
    /// </summary>
    public partial class Modbus_Debugger : Window
    {
        public static Array Types { get; } = Enum.GetValues(typeof(Modbus_Debugger.CommandTypes));

        public string Resource
        {
            get { return (string)GetValue(ResourceProperty); }
            set { SetValue(ResourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Resource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ResourceProperty =
            DependencyProperty.Register("Resource", typeof(string), typeof(Modbus_Debugger), new PropertyMetadata(null));

        public string LogData
        {
            get { return (string)GetValue(LogDataProperty); }
            set { SetValue(LogDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RecieveData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LogDataProperty =
            DependencyProperty.Register("LogData", typeof(string), typeof(Modbus_Debugger), new PropertyMetadata(null));

        public CommandTypes CommandType
        {
            get { return (CommandTypes)GetValue(CommandTypeProperty); }
            set { SetValue(CommandTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CommandTypes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CommandTypeProperty =
            DependencyProperty.Register("CommandType", typeof(CommandTypes), typeof(Modbus_Debugger), new PropertyMetadata(CommandTypes.ReadHoldingRegister));


        public byte SlaveAddr
        {
            get { return (byte)GetValue(SlaveAddrProperty); }
            set { SetValue(SlaveAddrProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlaveAddr.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlaveAddrProperty =
            DependencyProperty.Register("SlaveAddr", typeof(byte), typeof(Modbus_Debugger), new PropertyMetadata((byte)01));

        public string StartAddrStr
        {
            get { return (string)GetValue(StartAddrStrProperty); }
            set { SetValue(StartAddrStrProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartAddrStr.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartAddrStrProperty =
            DependencyProperty.Register("StartAddrStr", typeof(string), typeof(Modbus_Debugger), new PropertyMetadata("0x0000", StartAddrStrChanged));

        private static void StartAddrStrChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is Modbus_Debugger self && e.NewValue is string text)
            {
                try
                {
                    var match = Modbus_Debugger.RE_HEX.Match(text);
                    if (match.Success)
                    {
                        self.StartAddr = ushort.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                    }
                    else
                    {
                        self.StartAddr = ushort.Parse(text);
                    }
                }
                catch
                {
                    self.StartAddrStr = e.OldValue as string;
                }
            }
        }

        public ushort StartAddr
        {
            get { return (ushort)GetValue(StartAddrProperty); }
            set { SetValue(StartAddrProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RegisterAddr.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartAddrProperty =
            DependencyProperty.Register("StartAddr", typeof(ushort), typeof(Modbus_Debugger), new PropertyMetadata((ushort)0));

        public ushort RegisterCount
        {
            get { return (ushort)GetValue(RegisterCountProperty); }
            set { SetValue(RegisterCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RegisterCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RegisterCountProperty =
            DependencyProperty.Register("RegisterCount", typeof(ushort), typeof(Modbus_Debugger), new PropertyMetadata((ushort)1));

        public string WriteData
        {
            get { return (string)GetValue(WriteDataProperty); }
            set { SetValue(WriteDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WriteData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WriteDataProperty =
            DependencyProperty.Register("WriteData", typeof(string), typeof(Modbus_Debugger), new PropertyMetadata(null));

        TcpClient currenttcpclient;
        SerialPort currentserialport;

        IModbusMaster ModbusMaster;
        ModbusFactory Factory = new ModbusFactory();
        public Modbus_Debugger()
        {
            InitializeComponent();
        }

        public enum CommandTypes
        {
            ReadCoil = 0x01,
            ReadDiscrete = 0x02,
            ReadHoldingRegister = 0x03,
            ReadInputRegister = 0x04,
            WriteCoil = 0x05,
            WriteRegister = 0x06,
            WriteMultiCoil = 0x0F,
            WriteMultiRegister = 0x10,
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Resource)) return;

            if(currentserialport != null)
            {
                currentserialport.Close();
            }

            if (HalHelper.RE_IpAddr.Match(Resource).Success)
            {
                currenttcpclient = new TcpClient(Resource, 502);
                currentserialport = null;
                ModbusMaster = Factory.CreateMaster(currenttcpclient);

                LogData += $"{DateTime.Now.ToShortTimeString()}: Modbus TCP {Resource} Open \r\n";
            }
            else
            {
                currentserialport = new SerialPort(Resource);
                if(!currentserialport.IsOpen) currentserialport.Open();
                currenttcpclient = null;
                ModbusMaster = Factory.CreateRtuMaster(currentserialport);

                LogData += $"{DateTime.Now.ToShortTimeString()}: Modbus RTU {Resource} Open \r\n";
            }

            //if (PLC.IsOpen)
            //{
            //    PLC.Close();
            //    LogData += $"{DateTime.Now.ToShortTimeString()}: PLC {PLC.Resource} Closed\r\n";
            //}
            //else
            //{
            //    PLC.Open();
            //    LogData += $"{DateTime.Now.ToShortTimeString()}: PLC {PLC.Resource} Opened\r\n";
            //}
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            switch(CommandType)
            {
                case CommandTypes.ReadCoil:
                    var rtncoils = ModbusMaster.ReadCoils(SlaveAddr, StartAddr, RegisterCount);
                    LogData += $"{DateTime.Now.ToShortTimeString()}: Read Coil {StartAddr} from {SlaveAddr}, rtn: {string.Join(" ", rtncoils.Select(x=> x?1:0))}\r\n";
                    break;

                case CommandTypes.ReadDiscrete:
                    var rtninputs = ModbusMaster.ReadInputs(SlaveAddr, StartAddr, RegisterCount);
                    LogData += $"{DateTime.Now.ToShortTimeString()}: Read Input {StartAddr} from {SlaveAddr}, rtn: {string.Join(" ", rtninputs.Select(x => x ? 1 : 0))}\r\n";
                    break;

                case CommandTypes.ReadHoldingRegister:
                    var rtnreg = ModbusMaster.ReadHoldingRegisters(SlaveAddr, StartAddr, RegisterCount);
                    LogData += $"{DateTime.Now.ToShortTimeString()}: Read holding Reg {StartAddr} from {SlaveAddr}, rtn: {string.Join(" ", rtnreg.Select(x => x.ToString("X04")))}\r\n";
                    break;

                case CommandTypes.ReadInputRegister:
                    ModbusMaster.ReadInputRegisters(SlaveAddr, StartAddr, RegisterCount);
                    break;

                case CommandTypes.WriteCoil:
                    var val = false; 
                    if(!string.IsNullOrEmpty(WriteData))
                    {
                        val = !WriteData.StartsWith("00");
                    }

                    ModbusMaster.WriteSingleCoil(SlaveAddr, StartAddr, val);
                    break;

                case CommandTypes.WriteRegister:
                    var data1 = WriteData.Split(new string[] { " ", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
                    ModbusMaster.WriteSingleRegister(SlaveAddr, StartAddr, ushort.Parse(data1[0], System.Globalization.NumberStyles.HexNumber));
                    LogData += $"{DateTime.Now.ToShortTimeString()}: Write Single Reg {StartAddr} from {SlaveAddr}. Data: {WriteData}\r\n";
                    break;

                case CommandTypes.WriteMultiCoil:
                    ModbusMaster.WriteMultipleCoils(SlaveAddr, StartAddr, null);
                    break;

                case CommandTypes.WriteMultiRegister:
                    var data = WriteData.Split(new string[] { " ", ",", ";"}, StringSplitOptions.RemoveEmptyEntries);
                    ModbusMaster.WriteMultipleRegisters(SlaveAddr, StartAddr, data.Select(x=>ushort.Parse(x)).ToArray());
                    LogData += $"{DateTime.Now.ToShortTimeString()}: Write Reg {StartAddr} from {SlaveAddr}. Data: {WriteData}\r\n";

                    break;
            }
        }

        static Regex RE_HEX = new Regex("0x([0-9a-fA-F]+)");
        private void OnVerifyHex(object sender, TextCompositionEventArgs e)
        {
            if(RE_HEX.Match(e.Text).Success)
            {
                int data = int.Parse(e.Text, System.Globalization.NumberStyles.HexNumber);
            }
        }
    }
}
