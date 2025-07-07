using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using TestCore;
using ToucanCore.HAL;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class DS300 : TF_Base, IFixture, IRelayArray
    {
        public int SocketCount { get; } = 1;
        public bool AutoDutIn { get; set; }

        public bool AutoDutOut { get; set; }
        public FixtureState State { get; private set; }

        public string Resource { get; set; }

        public string Support => "DS300 V1.1";
        public string Model => "DS300 V1.1";

        public string SN => "DefaultSN";

        public bool IsInitialized { get; protected set; }

        public int ChannelCount { get; } = 2;

        public int OnValue_Stored => throw new NotImplementedException();

        public int OffValue_Stored => throw new NotImplementedException();

        private SerialPort Port;
        public bool IsOpen => Port?.IsOpen ?? false;

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
        public event EventHandler<DutMessage> EmergencyTrigged;
        public event EventHandler<DutMessage> FixtureError;
        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        public bool FrontDoorStatus { get; private set; }

        public int Clear()
        {
            Port?.Close();
            Port?.Dispose();
            return 1;
        }

        public int CloseFrontDoor(int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("MANUAL_CLOSE\r\n");
            FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Close Door" });

            return 1;
        }

        public int CheckDutReady(out bool state, int slot = 0)
        {
            //state = false;
            //Port.ReadExisting();
            //Port.Write("start ready\r\n");

            //try
            //{
            //    var resp = Port.ReadLine();

            //    if (resp == "start test\r\n")
            //    {
            //        state = true;
            //        DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Dut is Ready" });

            //        Port.Write("start clear\r\n");
            //    }
            //}
            //catch
            //{ }
            state = (MessageBox.Show("Is DUT Ready?", "Check DUT Ready", MessageBoxButton.YesNo) == MessageBoxResult.Yes) ? true : false;

            return 1;
        }

        public int CloseRearDoor(int slot = 0)
        {
            return 1;
        }

        public int DutIn(int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("MANUAL_IN\r\n");
            DutIning?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "DUT Ining" });
            return 1;
        }

        public int DutOut(int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("MANUAL_OUT\r\n");
            DutOuting?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "DUT Outing" });
            return 1;
        }

        public int EmergencyStop()
        {
            //Port.ReadExisting();
            //Port.Write("STOP\r\n");
            MessageBox.Show("Not Support", "Warning");
            return 0;
        }

        public int Initialize()
        {
            if (IsInitialized) return 1;
            if (Port?.IsOpen == true)
            {
                Port.Close();
            }

            Port = new SerialPort(Resource);
            Port.BaudRate = 115200;
            Port.Parity = Parity.None;
            Port.StopBits = StopBits.One;
            Port.DataBits = 8;

            Port.ReadTimeout = 200;

            Port.Open();
            Info($"Open {Port.PortName} {Port.IsOpen}");

            Port.ReadExisting();

            //Port.Write("init mode\r\n");
            Initializing?.Invoke(this, null);

            //Thread.Sleep(100);

            var status = Port.ReadExisting();

            //Info($"Fixture Init Response: {status}");

            Port.Write("version\r\n");

            System.Threading.Thread.Sleep(100);

            var ver = Port.ReadExisting();

            Info($"Fixture Version: {ver}");

            IsInitialized = true;
            Initialized?.Invoke(this, null);

            Port.DataReceived += Port_DataReceived;
            return 1;
        }

        private string tail = string.Empty;
        private string lastcmd = string.Empty;
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (sender is SerialPort sp)
            {
                var data = sp.ReadExisting();

                var responses = data.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                Info($"{data}, {responses.Length}, {responses?.FirstOrDefault()}");
                foreach (var resp in responses)
                {
                    if (lastcmd == resp) continue;
                    lastcmd = resp;
                    Info($"Resp: {resp}");
                    switch (resp)
                    {
                        case "post-action ok":
                        case "MANUAL_OPEN_OK":
                        case "door open":
                            FrontDoorStatus = false;
                            FrontDoorOpened?.Invoke(this, new DutMessage());
                            break;
                        case "pre-action ok":
                        case "MANUAL_CLOSE_OK":
                        case "door close":
                            FrontDoorStatus = true;
                            FrontDoorClosed?.Invoke(this, new DutMessage());
                            break;
                        case "door error":
                            FixtureError?.Invoke(this, new DutMessage() { Message = resp });
                            break;
                        //case "fixture press":
                            //DutInDone?.Invoke(this, new DutMessage());
                            //break;
                        //case "fixture loosen":
                        //    DutOuted?.Invoke(this, new DutMessage());
                        //    break;
                        //case "fixture error":
                        //    break;
                        case "power on":
                            break;
                        case "power off":
                            break;
                        case "power error":
                            break;

                        default:
                            Warn($"{Port.PortName} read data {resp}");
                            break;
                    }
                }
            }
        }

        public int OpenFrontDoor(int slot = 0)
        {
            Port.ReadExisting();
            //Port.WriteLine("post-action\r");  //open door
            Port.Write("MANUAL_OPEN\r\n");
            FrontDoorOpening?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Opening Front Door" });

            //try
            //{
            //    var resp = Port.ReadLine();

            //    if (resp == "post-action ok") // door open
            //    {
            //        FrontDoorOpened?.Invoke(this, new DutMessage() { SlotIndex = slot, Message = "Front Door Opened" });
            //    }
            //}
            //catch
            //{ }

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

            //Info($"Open {Port.PortName}, start loop check status");

            //Task.Run(() =>
            //{
            //    while(IsOpen)
            //    {
            //        Port.Write("door status\r\n");
            //        Thread.Sleep(100);
            //    }
            //});

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

        public int SetRelay(int channleindex, bool state)
        {
            Warn("Not Support Relay");
            return 1;
        }

        public int Reset()
        {
            return SetRelay(false, 0, 1, 2);
        }

        public int GetStateDutIn(int slot = 0)
        {
            Port.ReadExisting();

            //Port.WriteLine("door status");  //open door
            Port.Write("fixture status\r\n");
            //FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Get DUT State" });

            //try
            //{
            //    var resp = Port.ReadLine();

            //    if (resp == "post-action ok") // door open
            //    {
            //        FrontDoorOpened?.Invoke(this, new DutMessage() { SlotIndex = slot, Message = "Front Door Opened" });
            //    }
            //}
            //catch
            //{ }

            return 1;
        }

        public int GetStateDutOut(int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("fixture status\r\n");
            //FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Get DUT State" });

            return 1;
        }

        public int GetStateDutPresent(int slot = 0)
        {
            return 1;
        }

        public int GetStateProtection(int slot = 0)
        {
            return 1;
        }

        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("door status\r\n");
            //FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Get Chamber State" });
            state = false;
            return 1;
        }

        public int GetStateFrontDoorClose(out bool state, int slot = 0)
        {

            Port.ReadExisting();
            Port.Write("door status\r\n");
            state = FrontDoorStatus;
            Info($"Get FrontDoor State, Slot {slot}. State {state}");
            return 1;
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("door status\r\n");
            state = false;
            return 1;
        }

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("door status\r\n");
            state = false;
            return 1;
        }

        public int GetStateDutIn(out bool state, int slot = 0)
        {
            Port.ReadExisting();
            Port.Write("door status\r\n");
            state = false;
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
            Port.ReadExisting();
            Port.Write("door status\r\n");
            state = false;
            return 1;
        }

        public int SetRelay(int value, int mask = 0xffff)
        {
            throw new NotImplementedException();
        }

        public int SettingUI()
        {
            throw new NotImplementedException();
        }
    }
}
