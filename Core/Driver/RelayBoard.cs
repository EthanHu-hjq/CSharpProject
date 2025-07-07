using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TestCore;
using ToucanCore.HAL;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class RelayBoard : TF_Base, IRelayArray
    {
        public int ChannelCount => 16;

        public string Model => "TYM_16_RelayBoard";

        public string SN => string.Empty;

        public string Resource { get; set; }

        public bool IsOpen => Port?.IsOpen ?? false;

        public bool IsInitialized { get; private set; }

        public int OnValue_Stored => throw new NotImplementedException();

        public int OffValue_Stored => throw new NotImplementedException();

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        private SerialPort Port;
        public int Clear()
        {
            //mutex?.Close();
            //mutex?.Dispose();
            Port?.Dispose();
            return 1;
        }

        public int Close()
        {
            if(IsOpen)
            {
                Port?.Close();
                //mutex.ReleaseMutex();
            }
            return 1;
        }

        public int GetIDN(out string idn)
        {
            throw new NotImplementedException();
        }

        //Mutex mutex;
        //bool running;
        public int Initialize()
        {
            Initializing?.Invoke(this, null);
            Port?.Close();
            Port?.Dispose();
            Port = new SerialPort(Resource);
            Port.BaudRate = 9600;
            Port.DataBits = 8;
            Port.StopBits = StopBits.One;
            Port.Parity = Parity.None;
            Initialized?.Invoke(this, null);

            //mutex?.Dispose();
            //mutex = new Mutex(true, Resource, out running);

            Reset();
            return 1;
        }

        public int Open()
        {
            var rtn = OpenPort();
            if (rtn > 0)
            {
                Close();
            }

            return rtn;
        }

        private int OpenPort()
        {
            if (IsOpen) return 1;
            //mutex.WaitOne();
            Port?.Open();
            return IsOpen ? 1: 0;
        }

        public int Reset()
        {
            SetRelay(0);
            return 1;
        }

        public int SetRelay(bool state, params int[] channels)
        {
            throw new NotImplementedException();
        }

        public int SetRelay(int channleindex, bool state)
        {
            throw new NotImplementedException();
        }

        byte[] offcmd = new byte[] { 0, 0x5a, 0x60, address, 6, 0, 0, 0, 0 };
        byte[] oncmd = new byte[] { 0, 0x5a, 0x60, address, 5, 0, 0, 0, 0 };

        const byte address = 1;

        public int SetRelay(int value, int mask=0xffff)
        {
            lock (this)
            {
                try
                {
                    OpenPort();
                    //byte[] groupcmd = new byte[] { 0x00, 0x5a, 0x60, address, 08, 0, 0, 0, 0 };

                    int onval = 0;
                    int offval = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        int bitval = 1 << i;

                        if ((bitval & mask) > 0)
                        {
                            if ((value & bitval) > 0)
                            {
                                onval += bitval;
                            }
                            else
                            {
                                offval += bitval;
                            }
                        }
                    }

                    if (offval > 0)
                    {
                        offcmd[5] = (byte)offval;
                        offcmd[6] = (byte)(offval >> 8);

                        offcmd[8] = (byte)(0x5a + 0x60 + address + 0x06 + offcmd[5] + offcmd[6]);
                        Port.DiscardInBuffer();
                        Port.Write(offcmd, 0, offcmd.Length);

                        var start = DateTime.Now;
                        Thread.Sleep(50);
                        while (Port.BytesToRead < 9 && DateTime.Now.Subtract(start).TotalMilliseconds < 300)
                        {
                            Thread.Sleep(20);
                        }

                        var rtn = Encoding.ASCII.GetBytes(Port.ReadExisting());

                        VerifyReturn(rtn);
                    }

                    if (onval > 0)
                    {
                        if (offval > 0) Thread.Sleep(50);
                        oncmd[5] = (byte)onval;
                        oncmd[6] = (byte)(onval >> 8);

                        oncmd[8] = (byte)(0x5a + 0x60 + address + 0x05 + oncmd[5] + oncmd[6]);

                        Port.DiscardInBuffer();
                        Port.Write(oncmd, 0, oncmd.Length);

                        var start = DateTime.Now;
                        Thread.Sleep(50);
                        while (Port.BytesToRead < 9 && DateTime.Now.Subtract(start).TotalMilliseconds < 300)
                        {
                            Thread.Sleep(20);
                        }

                        var rtn = Encoding.ASCII.GetBytes(Port.ReadExisting());

                        VerifyReturn(rtn);
                    }

                    Info($"Set Relay {value}, Mask {mask} OK");
                }
                finally
                {
                    Close();
                }
            }
            
            return 1;
        }

        private bool VerifyReturn(byte[] rtn)
        {
            if(rtn.Length >= 9)
            {
                var checksum = 0;
                for (int i = 0; i < 8; i++)
                    checksum += rtn[i];
                checksum = (byte)(checksum & 0xff);
                if (rtn[8] != checksum)
                {
                    Warn($"Checksum Failed. expect {checksum.ToString("X02")}, frame {string.Join(" ", rtn.Select(x => x.ToString("X02")))}");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                Warn($"Frame failed, frame {string.Join(" ", rtn.Select(x => x.ToString("X02")))}");
                return false;
            }
        }
    }
}
