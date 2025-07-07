using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCore;
using ToucanCore.HAL;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class RfidReader_LVHM : TF_Base, ISerialNumberReader
    {
        public string Model => "LVHM RFID";

        public string SN => string.Empty;

        public string Resource { get; set; }

        public bool IsOpen => Port?.IsOpen ?? false;

        public bool IsInitialized { get; private set; }

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        private SerialPort Port;

        public int Clear()
        {
            Port?.Close();
            Port?.Dispose();
            return 1;
        }

        public int Close()
        {
            Port?.Close(); return 1;
        }

        public int GetIDN(out string idn)
        {
            idn = null;
            return 1;
        }

        public int Initialize()
        {
#if DEBUG
            IsInitialized = true;
            return 1;
#endif

            if (IsInitialized) return 1;
            if (Port?.IsOpen == true)
            {
                Port.Close();
            }

            Port = new SerialPort(Resource);
            //Port.BaudRate = 115200;
            //Port.Parity = Parity.None;
            //Port.StopBits = StopBits.One;
            //Port.DataBits = 8;

            Port.ReadTimeout = 100;

            Port.Open();
            Info($"Open {Port.PortName} {Port.IsOpen}");

            Port.ReadExisting();

            //Port.Write("init mode\r\n");
            Initializing?.Invoke(this, null);

            //Thread.Sleep(100);

            //var status = Port.ReadExisting();

            ////Info($"Fixture Init Response: {status}");

            //Port.Write("version\r\n");

            //System.Threading.Thread.Sleep(100);

            //var ver = Port.ReadExisting();

            //Info($"Fixture Version: {ver}");

            IsInitialized = true;
            Initialized?.Invoke(this, null);
            return 1;
        }

        public int Open()
        {
#if DEBUG
            return 1;
#endif
            if (!IsOpen)
            {
                Port.Open();
            }
            return IsOpen ? 1 : 0;
        }

        static byte[] RequestMsg = new byte[] { 0x01, 0x08, 0xA3, 0x20, 0x08, 0x01, 0x00, 0x7C };
        public string ReadSerialNumber()
        {
#if DEBUG
            return "TS";
#endif

            try
            {
                Port.ReadExisting();
                Port.Write(RequestMsg, 0, RequestMsg.Length);

                
                string sn = string.Empty;
                DateTime t0 = DateTime.Now;

                byte[] bytes = new byte[Port.ReadBufferSize];
                do
                {
                    Thread.Sleep(100);

                    if (Port.BytesToRead <= 5) continue;

                    Port.Read(bytes, 0, Port.BytesToRead);

                    if (bytes[4] != 0) continue;

                    var data = bytes.Take(Port.BytesToRead - 1);

                    byte chk = 0;
                    foreach (byte b in data)
                    {
                        chk = (byte) (chk ^ b);
                    }

                    chk = (byte)(~chk);
                    if (bytes[Port.BytesToRead - 1] != chk) continue;

                    sn = Encoding.UTF8.GetString(data.Skip(5).ToArray());
                }
                while (string.IsNullOrEmpty(sn) && DateTime.Now.Subtract(t0).TotalMilliseconds < 500);

                return sn;
            }
            catch(TimeoutException)
            {
                return string.Empty;
            }
        }
    }
}
