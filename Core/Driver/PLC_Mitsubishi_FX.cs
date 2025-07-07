using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO.Pipes;
using System.IO.Ports;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Markup.Localizer;
using TestCore;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    /// <summary>
    /// https://www.cnblogs.com/chengcanghai/p/10132862.html
    /// https://www.cnblogs.com/young525/p/5873796.html
    /// </summary>
    
    public class PLC_Mitsubishi_FX : TF_Base, IHardware
    {
        public string Model => "Mitsubishi IQ-F FX";

        public string SN => string.Empty;

        public string Resource { get; set; }

        public bool IsOpen => Port.IsOpen;

        public bool IsInitialized { get; set; }

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        internal SerialPort Port { get; private set; }

        public int Clear()
        {
            Port?.Dispose();
            return 1;
        }

        public int Close()
        {
            if(Port?.IsOpen == true) Port?.Close();
            return 1;
        }

        public int GetIDN(out string idn)
        {
            ReadRegister(RegisterType.D, 0x0250, 16, out byte[] readdata, out bool state);

            idn = "Mitsubishi";
            return 1;
        }

        public int Initialize()
        {
            Port = new SerialPort(Resource);

            Port.BaudRate = 9600;
            Port.DataBits = 7;
            Port.Parity = Parity.Even;
            Port.StopBits = StopBits.Two;

            IsInitialized = true;

            return 1;
        }

        public int Open()
        {
            if (!IsOpen)
            {
                Port.Open();
            }
            Thread.Sleep(10);
            Port.Close();

            return 1;
        }

        private static short OctToDec(short oct)
        {
            short dec;

            int temp = 0;
            int res = 0;

            int i = 0;
            do
            {
                temp = oct / 10;
                res += (oct % 10) * (int)Math.Pow(8, i);
                i++;
                oct = (short)temp;
            }
            while (temp != 0);

            return (short)res;
        }

        public static bool ParseChannel(string channel, out RegisterType type, out short address)
        {
            var match = RE_RegisterName.Match(channel);
            if(match.Success)
            {
                Enum.TryParse<RegisterType>(match.Groups[1].Value, true, out type);
                address = short.Parse(match.Groups[2].Value);

                switch(type)
                {
                    case RegisterType.X:
                    case RegisterType.Y:
                        address = OctToDec(address);
                        break;
                }

                return true;
            }
            type = RegisterType.D;
            address = 0;

            return false;
        }

        public int ForceStatus(RegisterType type, short address, bool ison)
        {
            lock (this)
            {
                try
                {
                    if (!Port.IsOpen) Port.Open();

                    var addrstr = string.Empty;
                    byte[] addrbytes = null;
                    byte[] crcbytes = new byte[2];
                    byte[] bytes = new byte[9];

                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address + 0xE00).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address + 0x800).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address + 0x500).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address + 0x400).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    bytes[0] = START_BYTE;  // START

                    if (ison)
                    {
                        bytes[1] = 0x37; // "7"
                    }
                    else
                    {
                        bytes[1] = 0x38;
                    }

                    bytes[2] = addrbytes[2];
                    bytes[3] = addrbytes[3];
                    bytes[4] = addrbytes[0];
                    bytes[5] = addrbytes[1];
                    bytes[6] = END_BYTE;  //END
                    bytes[7] = 0x00;
                    bytes[8] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[7] = crcbytes[0];
                    bytes[8] = crcbytes[1];

                    Port.DiscardInBuffer();

                    Debug($"ForceState {ison} to {type} {address}. write {string.Join(" ", bytes.Select(x => x.ToString("X02")))}");
                    Port.Write(bytes, 0, bytes.Length);

                    System.Threading.Thread.Sleep(100);
                    var rtn = Port.ReadExisting();

                    Debug($"ForceState rtn:{string.Join(" ", rtn.StringToByteArray().Select(x => x.ToString("X02")))}");
                }
                finally
                {
                    Port.Close();
                }
            }

            return 1;
        }

        public int ReadRegister(RegisterType type, short address, int readlength, out byte[] readdata, out bool state)
        {
            lock (this)
            {
                try
                {
                    if (!Port.IsOpen) Port.Open();
                    var addrstr = string.Empty;
                    byte[] addrbytes = null;
                    byte[] crcbytes = new byte[2];
                    var bytes = new byte[11];
                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address * 3).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address * 2 + 0x1C0).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address * 2 + 0x100).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address / 8 + 0xA0).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address / 8 + 0x80).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    var datalenbytes = Encoding.UTF8.GetBytes(readlength.ToString("X02"));

                    bytes[0] = START_BYTE;  // START
                    bytes[1] = 0x30; // "0"
                    bytes[2] = addrbytes[0];
                    bytes[3] = addrbytes[1];
                    bytes[4] = addrbytes[2];
                    bytes[5] = addrbytes[3];
                    bytes[6] = datalenbytes[0];
                    bytes[7] = datalenbytes[1];
                    bytes[8] = END_BYTE;  //END
                    bytes[9] = 0x00;
                    bytes[10] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[9] = crcbytes[0];
                    bytes[10] = crcbytes[1];

                    Port.DiscardInBuffer();
                    Port.Write(bytes, 0, bytes.Length);

                    var send = Encoding.UTF8.GetString(bytes);

                    Debug(string.Join(" ", bytes.Select(x => x.ToString("X02"))));

                    System.Threading.Thread.Sleep(100);

                    var data = Encoding.ASCII.GetBytes(Port.ReadExisting());

                    readdata = new byte[readlength * 2];
                    state = false;
                    if (data.Length < readlength * 2 + 2) return -1;

                    if (data[0] == START_BYTE && data[readlength * 2 + 1] == END_BYTE)  // Not do CHK
                    {
                        for (int i = 0; i < readlength * 2; i++)
                        {
                            readdata[i] = data[i + 1];
                        }

                        var registerdata = new byte[2];
                        registerdata[0] = readdata[0];
                        registerdata[1] = readdata[1];

                        var strdata = Encoding.ASCII.GetString(registerdata);

                        //var regvals = (byte)((registerdata[0] - 0x30) << 4) + (registerdata[1] - 0x30);  //byte.Parse(strdata);
                        var regvals = byte.Parse(strdata, System.Globalization.NumberStyles.HexNumber);
                        var index = address % 8;

                        state = (regvals & (byte)(1 << index)) > 0;

                        Debug($"Rtn {string.Join(" ", data.Select(x => x.ToString("X02")))} Data {strdata}, Status {state} on addr {address}");

                        return 1;
                    }
                    else
                    {
                        Debug($"Rtn {string.Join(" ", data.Select(x => x.ToString("X02")))}. Chksum Failed");
                    }
                }
                finally
                {
                    Port.Close();
                }
            }
            return -1;
        }

        /// <summary>
        /// Not Validated
        /// </summary>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <param name="writedata"></param>
        /// <returns></returns>
        public int WriteRegister(RegisterType type, short address, byte[] writedata)
        {
            lock (this)
            {
                try
                {
                    if (!Port.IsOpen) Port.Open();
                    var addrstr = string.Empty;
                    byte[] addrbytes = null;

                    byte[] datalenbytes = new byte[2];
                    byte[] crcbytes = new byte[2];
                    byte[] bytes = null;

                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address * 3).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address * 2 + 0x1C0).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address * 2 + 0x100).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address + 0xA0).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address + 0x80).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    bytes = new byte[11 + writedata.Length];
                    datalenbytes = Encoding.UTF8.GetBytes(writedata.Length.ToString("X02"));
                    bytes[0] = START_BYTE;  // START
                    bytes[1] = 0x31;
                    bytes[2] = addrbytes[0];
                    bytes[3] = addrbytes[1];
                    bytes[4] = addrbytes[2];
                    bytes[5] = addrbytes[3];
                    bytes[6] = datalenbytes[0];
                    bytes[7] = datalenbytes[1];

                    for (int i = 0; i < writedata.Length; i++)
                    {
                        bytes[8 + i] = writedata[i];
                    }

                    bytes[8 + writedata.Length] = END_BYTE;  //END
                    bytes[9 + writedata.Length] = 0x00;
                    bytes[10 + writedata.Length] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[9 + writedata.Length] = crcbytes[0];
                    bytes[10 + writedata.Length] = crcbytes[1];

                    Port.ReadExisting();
                    Port.Write(bytes, 0, bytes.Length);

                    System.Threading.Thread.Sleep(10);

                    var data = Port.ReadExisting();

                    if (data.Length > 0 && data[0] == 0x06)
                    {
                        return 1;
                    }
                }
                finally
                {
                    Port.Close();
                }
            }

            return -1;
        }

        /// <summary>
        /// e.g. 02 30 31 30 31 34 30 32 03 35 42
        /// </summary>
        /// <param name="type"></param>
        /// <param name="action"></param>
        /// <param name="address"></param>
        /// <param name="readlength">work for read</param>
        /// <param name="writedata">work for write</param>
        /// <returns></returns>
        internal static byte[] Encode(RegisterType type, RegisterAction action, short address, int readlength, byte[] writedata = null)
        {
            var addrstr = string.Empty;
            byte[] addrbytes = null;

            byte[] datalenbytes = new byte[2];
            byte[] crcbytes = new byte[2];
            byte[] bytes = null;
            switch (action)
            {
                case RegisterAction.Read:
                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address * 3).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address * 2 + 0x1C0).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address * 2 + 0x100).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address / 8 + 0xA0).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address / 8 + 0x80).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    datalenbytes = Encoding.UTF8.GetBytes(readlength.ToString("X02"));
                    bytes = new byte[11];
                    bytes[0] = START_BYTE;  // START
                    bytes[1] = 0x30; // "0"
                    bytes[2] = addrbytes[0];
                    bytes[3] = addrbytes[1];
                    bytes[4] = addrbytes[2];
                    bytes[5] = addrbytes[3];
                    bytes[6] = datalenbytes[0];
                    bytes[7] = datalenbytes[1];
                    bytes[8] = END_BYTE;  //END
                    bytes[9] = 0x00;
                    bytes[10] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[9] = crcbytes[0];
                    bytes[10] = crcbytes[1];
                    break;

                case RegisterAction.Write:
                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address * 3).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address * 2 + 0x1C0).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address * 2 + 0x100).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address + 0xA0).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address + 0x80).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    var len = writedata.Length * 2;
                    bytes = new byte[11 + len];
                    datalenbytes = Encoding.UTF8.GetBytes(writedata.Length.ToString("X02"));
                    bytes[0] = START_BYTE;  // START
                    bytes[1] = 0x31;
                    bytes[2] = addrbytes[0];
                    bytes[3] = addrbytes[1];
                    bytes[4] = addrbytes[2];
                    bytes[5] = addrbytes[3];
                    bytes[6] = datalenbytes[0];
                    bytes[7] = datalenbytes[1];

                    for (int i = 0; i < writedata.Length; i++)
                    {
                        var temp = Encoding.ASCII.GetBytes(writedata[i].ToString("X02"));
                        bytes[8 + i * 2] = temp[0];
                        bytes[8 + i * 2 + 1] = temp[1];
                    }

                    bytes[8 + len] = END_BYTE;  //END
                    bytes[9 + len] = 0x00;
                    bytes[10 + len] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[9 + len] = crcbytes[0];
                    bytes[10 + len] = crcbytes[1];
                    break;

                case RegisterAction.ForceOn:
                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address + 0xE00).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address + 0x800).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address + 0x500).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address + 0x400).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    bytes = new byte[9];
                    bytes[0] = START_BYTE;  // START
                    bytes[1] = 0x37; // "7"
                    bytes[2] = addrbytes[2];
                    bytes[3] = addrbytes[3];
                    bytes[4] = addrbytes[0];
                    bytes[5] = addrbytes[1];
                    bytes[6] = END_BYTE;  //END
                    bytes[7] = 0x00;
                    bytes[8] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[7] = crcbytes[0];
                    bytes[8] = crcbytes[1];
                    break;

                case RegisterAction.ForceOff:
                    switch (type)
                    {
                        case RegisterType.S:
                            addrstr = (address).ToString("X04");
                            break;
                        case RegisterType.C:
                            addrstr = (address + 0xE00).ToString("X04"); break;  //x1C0 448
                        case RegisterType.T:
                            addrstr = (address + 0xC0).ToString("X04"); break;
                        case RegisterType.D:
                            addrstr = (address * 2 + 0x1000).ToString("X04"); break;
                        case RegisterType.M:
                            addrstr = (address + 0x800).ToString("X04"); break;
                        case RegisterType.Y:
                            addrstr = (address + 0x500).ToString("X04"); break;  // 0xA0 160
                        case RegisterType.X:
                            addrstr = (address + 0x400).ToString("X04"); break;   //x80 128
                    }
                    addrbytes = Encoding.UTF8.GetBytes(addrstr);
                    bytes = new byte[9];
                    bytes[0] = START_BYTE;  // START
                    bytes[1] = 0x38; // "8"
                    bytes[2] = addrbytes[2];
                    bytes[3] = addrbytes[3];
                    bytes[4] = addrbytes[0];
                    bytes[5] = addrbytes[1];
                    bytes[6] = END_BYTE;  //END
                    bytes[7] = 0x00;
                    bytes[8] = 0x00;

                    crcbytes = Encoding.UTF8.GetBytes((bytes.Skip(1).Sum(x => x) % 0x100).ToString("X02"));
                    bytes[7] = crcbytes[0];
                    bytes[8] = crcbytes[1];
                    break;
            }
            return bytes;
        }

        const byte START_BYTE = 0x02;
        const byte END_BYTE = 0x03;

        /// <summary>
        /// e.g. 02 33 30 37 35 03 44 32
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>tail of message</returns>
        internal static string Decode(string msg, out IEnumerable<byte> data)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            data = null;
            if (bytes.Length < 4) return msg;

            var startidx = msg.IndexOf((char)START_BYTE);

            if(startidx >= 0 )
            {
                var endidx = msg.IndexOf((char)END_BYTE);

                if(endidx >= 0 )
                {
                    if (bytes.Length - endidx >= 3)
                    {
                        //byte[] data = new byte[endidx - startidx - 1];

                        var data1 = bytes.Skip(startidx + 1).Take(endidx - startidx - 1).Select(x => (byte)(x - 0x30));

                        var crc = (data1.Sum(x => x) + END_BYTE) % 0x100;
                        if (((bytes[endidx + 1] - 0x30) << 4) + (bytes[endidx + 2] - 0x30) == crc)
                        {
                            data = data1;
                        }
                    }
                }

                return msg;
            }

            return msg;
        }

        internal static Regex RE_RegisterName = new Regex("([SCTDMYX]{1})(\\d+)");

        /// <summary>
        /// 
        /// </summary>
        /// <param name="registername">should be Type with a index, such as X1 or Y8</param>
        /// <returns></returns>
        public string Query(string registername)
        {
            var re = RE_RegisterName.Match(registername);
            if (re.Success)
            {
                lock (this)
                {
                    try
                    {
                        Port.Open();
                        var type = (RegisterType)(Enum.Parse(typeof(RegisterType), re.Groups[1].Value));
                        var msg = Encode(type, RegisterAction.Read, short.Parse(re.Groups[2].Value), 0);
                        Port.Write(msg, 0, msg.Length);
                        byte[] bytes = new byte[255];
                        Port.Read(bytes, 0, Port.BytesToRead);
                    }
                    finally
                    {
                        Port.Close();
                    }
                }
                //TODO
                return string.Empty;
            }
            else
            {
                throw new NotSupportedException($"CMD {registername} is not supported");
            }
        }

        public enum RegisterType
        {
            S, C, T, D, M, Y, X
        }

        public enum RegisterAction
        {
            Read,
            Write,
            ForceOn,
            ForceOff
        }
    }

    
}
