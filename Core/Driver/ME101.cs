using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToucanCore.HAL;
using TestCore;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class ME101 : TF_Base, IFixture, IRelayArray
    {
        readonly static string FilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "ME101.xml");
        public int SocketCount { get; } = 1;
        public const string Version = "1ED3C5-TYM-V2.1-2024.01.17";//"1ED3C5-TYM-V1.5-2021.7.5";
        public bool AutoDutIn { get; set; }

        public bool AutoDutOut { get; set; }
        public FixtureState State { get; private set; }

        public string Resource { get; set; }

        public string Support => "ME101";
        public string Model { get; } = "ME101"; // if specified version, it mean this only for this model 

        public string SN { get; } = string.Empty;

        public bool IsOpen => Port.IsOpen;

        public bool IsInitialized { get; protected set; }

        public int ChannelCount { get; } = 3;
        public int OnValue_Stored => throw new NotImplementedException();

        public int OffValue_Stored => throw new NotImplementedException();

        public event EventHandler<DutMessage> DutIning;
        public event EventHandler<DutMessage> DutInDone;
        public event EventHandler<DutMessage> DutOuting;
        public event EventHandler<DutMessage> DutOuted;
        public event EventHandler<DutMessage> OnDutPresent;
        public event EventHandler<DutMessage> OnDutAbsent;
        public event EventHandler<DutMessage> FrontDoorOpening;
        public event EventHandler<DutMessage> FrontDoorOpened;
        public event EventHandler<DutMessage> FrontDoorClosing;
        public event EventHandler<DutMessage> FrontDoorClosed;
        public event EventHandler<DutMessage> RearDoorOpening;
        public event EventHandler<DutMessage> RearDoorOpened;
        public event EventHandler<DutMessage> RearDoorClosing;
        public event EventHandler<DutMessage> RearDoorClosed;

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;
        public event EventHandler<DutMessage> EmergencyTrigged;
        public event EventHandler<DutMessage> FixtureError;

        private SerialPort Port;

        public int Clear()
        {
            if (Port?.IsOpen == true)
            {
                Port?.Close();
            }

            Port?.Dispose();

            if (IsInitialized)
            {
                IsInitialized = false;
            }

            return 1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int CloseFrontDoor(int slot = 0)
        {
            Port.ReadExisting();
            Port.WriteLine("pre-action");
            FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Close Door" });

            try
            {
                var resp = Port.ReadLine();

                if (resp == "pre-action ok")
                {
                    FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Door Closed" });
                }
            }
            catch
            { }

            return 1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int CheckDutReady(out bool state, int slot = 0)
        {
            state = false;
            Port.ReadExisting();
            Port.WriteLine("start ready");

            try
            {
                var resp = Port.ReadLine();

                if (resp == "start test")
                {
                    state = true;
                    DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Dut is Ready" });

                    Port.WriteLine("start clear");
                }
            }
            catch
            { }

            return 1;
        }

        public int CloseRearDoor(int slot = 0)
        {
            return 1;
        }

        public int DutIn(int slot = 0)
        {
            return 1;
        }

        public int DutOut(int slot = 0)
        {
            return 1;
        }

        public int EmergencyStop()
        {
            return 1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Initialize()
        {
            Port = new SerialPort(Resource);
            Port.BaudRate = 115200;
            Port.Parity = Parity.None;
            Port.StopBits = StopBits.One;
            Port.DataBits = 8;
            Port.NewLine = "\r\n";

            Port.ReadTimeout = 200;

            if (Port.IsOpen)
            {
                Port.Close();
            }
            Port.Open();

            Port.ReadExisting();

            Port.WriteLine("init mode");
            Initializing?.Invoke(this, null);

            //Port.DataReceived += Port_DataReceived;
            System.Threading.Thread.Sleep(100);

            var status = Port.ReadExisting();

            Info($"Fixture Init Response: {status}");

            Port.WriteLine("version");

            System.Threading.Thread.Sleep(100);

            var ver = Port.ReadLine();

            Info($"Fixture Version: {ver}. Driver {Version}");

            IsInitialized = true;
            Initialized?.Invoke(this, null);

            return 1;
        }

        //private string tail = string.Empty;
        //private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        //{
        //    if (sender is SerialPort sp)
        //    {
        //        var data = sp.ReadExisting();

        //        var responses = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);

        //        foreach (var resp in responses)
        //        {

        //        }
        //    }
        //}

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int OpenFrontDoor(int slot = 0)
        {
            Port.ReadExisting();

            Port.WriteLine("post-action");  //open door
            FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Opening Front Door" });

            try
            {
                var resp = Port.ReadLine();

                if (resp == "post-action ok") // door open
                {
                    FrontDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Opened" });
                }
            }
            catch
            { }

            return 1;
        }

        public int OpenRearDoor(int slot = 0)
        {
            return 1;
        }

        public int SetFixtureState(FixtureState state)
        {
            return 1;
        }

        public int Open()
        {
            if (!IsOpen)
            {
                Port.Open();
            }

            return 1;
        }

        public int Close()
        {
            if (IsOpen)
            {
                Port.Close();
            }

            return 1;
        }

        public int GetIDN(out string idn)
        {
            idn = SN;
            return 1;
        }

        public int SetRelay(bool state, params int[] channels)
        {
            foreach (var idx in channels)
            {
                SetRelay(idx, state);
            }
            return 1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int SetRelay(int channleindex, bool state)
        {
            var cmd = $"CY{channleindex + 1}1 {(channleindex % 2 == 0 ? (state ? "ON" : "OFF") : (state ? "IN" : "OUT"))}";
            Port.ReadExisting();

            Port.WriteLine(cmd);

            //Port.ReadLine();
            return 1;
        }

        public int Reset()
        {
            return SetRelay(false, 0, 1, 2);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            Port.ReadExisting();

            Port.WriteLine("door status");

            try
            {
                var resp = Port.ReadLine();

                if (resp == "door open") // door open
                {
                    FrontDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Opened" });
                    state = true;
                    return 1;
                }
            }
            catch
            { }
            state = false;
            return 1;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int GetStateFrontDoorClose(out bool state, int slot = 0)
        {
            Port.ReadExisting();

            Port.WriteLine("door status");

            try
            {
                var resp = Port.ReadLine();

                if (resp == "door close") // door close
                {
                    FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Closed" });
                    state = true;
                    return 1;
                }
            }
            catch
            { }
            state = false;
            return 1;
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateDutIn(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateDutOut(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateDutPresent(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateSafety(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetSlotActiveState(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int SetRelay(int value, int mask)
        {
            throw new NotImplementedException();
        }

        

        public int SettingUI()
        {
            throw new NotImplementedException();
            //PLC_Mitsubishi_FX_SettingUI setting = new PLC_Mitsubishi_FX_SettingUI(FilePath, null);

            //return setting.ShowDialog() == true ? 1 : 0;
        }
    }
}
