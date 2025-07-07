using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.HAL
{
    //public interface IRelayArray : IHardware
    //{
    //    int ChannelCount { get; }
    //    int SetRelay(bool state, params int[] channels);
    //    int SetRelay(int channleindex, bool state);
    //    int SetRelay(int value, int mask=0xffff);
    //    int Reset();

    //    int OnValue_Stored { get; }
    //    int OffValue_Stored { get; }
    //}

    public class RelayArray_None : IRelayArray
    {
        public int ChannelCount { get; } = 0;

        public string Model => "None";

        public string SN => string.Empty;

        public string Resource { get; set; } = string.Empty;

        public bool IsOpen => throw new NotImplementedException();
        
        public bool IsInitialized => throw new NotImplementedException();

        public int OnValue_Stored => throw new NotImplementedException();

        public int OffValue_Stored => throw new NotImplementedException();

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        /// <summary>
        /// Tuple, value1 means On, value means Off
        /// </summary>
        private List<Tuple<string, string>> Commands = new List<Tuple<string, string>>();

        public int Clear()
        {
            return 1;
        }

        public int Close()
        {
            return 1;
        }

        public int GetIDN(out string idn)
        {
            idn = "None";
            return 1;
        }

        public int Initialize()
        {
            return 1;
        }

        public int Open()
        {
            return 1;
        }

        public int Reset()
        {
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

        /// <summary>
        /// 1 to true, 0 to false
        /// </summary>
        /// <param name="value">Hex Data</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int SetRelay(int value, int mask)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// a Proxy for call bat instead of execute hardware, for support 1 station with two fixture
    /// </summary>
    public class RelayArray_Proxy : TF_Base, IRelayArray
    {
        public int ChannelCount { get; } = 16;

        public string Model => "Proxy";

        public string SN => "RelayArray_Proxy";

        public string Resource { get; set; } = string.Empty;

        public bool IsOpen => throw new NotImplementedException();

        public bool IsInitialized => throw new NotImplementedException();

        public int OnValue_Stored => 0;

        public int OffValue_Stored => 0;

        //public bool IsProxy { get; } = true;
        public string Workbase { get; set; }

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        //private string cmd_On;
        //private string cmd_Off;

        Process ProxyProcess { get; set; }

        public int Clear()
        {
            Cleared?.Invoke(this, null);
            return 1;
        }

        public int Close()
        {
            ProxyProcess?.Close();
            return 1;
        }

        public int GetIDN(out string idn)
        {
            idn = "Proxy";
            return 1;
        }

        public int Initialize()
        {
            Initializing?.Invoke(this, null);
            Initialized?.Invoke(this, null);
            return 1;
        }

        public int Open()
        {
            ProxyProcess = new Process();

            var fn = Path.Combine(Workbase, "RelayProxy.bat");

            if(!File.Exists(fn))
            {
                using (var sw = new StreamWriter(fn))
                {
                    sw.WriteLine(":: This is Relay Proxy bat file, Please Add your real command hear");
                    sw.WriteLine(":: This file should accept 2 arguments, the first one is Channel Index, which start from 0, the next one is state, 1 for true, 0 for false ");
                    sw.WriteLine(":: if the channe index is less than 0, it means it should be ignored. in this case, it means it accept the set value for each channel. ");

                    sw.WriteLine();
                    sw.WriteLine(":: use %1 as the first argument, and %2 as the second one ");

                    sw.Flush();
                    sw.Close();
                }
            }

            ProxyProcess.StartInfo.FileName = fn;
            ProxyProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            ProxyProcess.StartInfo.CreateNoWindow = true;

            return 1;
        }

        public int Reset()
        {
            ProxyProcess.StartInfo.Arguments = $"{Resource} -1 0";
            ProxyProcess.Start();
            Info("Reset");
            return 1;
        }

        public int SetRelay(bool state, params int[] channels)
        {
            foreach (var ch in channels)
            {
                ProxyProcess.StartInfo.Arguments = $"{Resource} {ch} {(state ? 1 : 0)}";
                ProxyProcess.Start();
                Info($"Set channel {string.Join(",", channels)} state {(state ? 1 : 0)}");
            }
            return 1;
        }

        public int SetRelay(int channleindex, bool state)
        {
            ProxyProcess.StartInfo.Arguments = $"{Resource} {channleindex} {(state ? 1 : 0)}";
            ProxyProcess.Start();

            Info($"Set channel {channleindex} state {state}");

            return 1;
        }
        /// <summary>
        /// 1 to true, 0 to false
        /// </summary>
        /// <param name="value">Hex Data</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public int SetRelay(int value, int mask)
        {
            ProxyProcess.StartInfo.Arguments = $"{Resource} {-1} {value}";
            ProxyProcess.Start();

            Info($"Set Value {value}");

            return 1;
        }
    }
}
