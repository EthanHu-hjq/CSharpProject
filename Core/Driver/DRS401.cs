using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using TestCore;
using ToucanCore.HAL;
using static System.Windows.Forms.AxHost;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    /// <summary>
    /// 旋转门
    /// </summary>
    public class DRS401 : TF_Base, IFixture, IAutoActiveSlotFixture, ILockableFixture
    {
        readonly static string FilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "DRS401.xml");
        public FixtureManipulationConfig ManipulationConfig { get; } = new FixtureManipulationConfig(new string[] { "Door Closed", "Lock Test", "Sensor A", "Sensor B" }, new string[] { "Y5", "X6", "X13", "X11" }, new string[] { "Lock Test" }, new string[] { "X6" });
        public int SocketCount => 2;
        public bool AutoDutIn { get; set; }

        public bool AutoDutOut { get; set; }
        public FixtureState State { get; }

        public string Support => "DRS401/DRL501";
        public string Model => "DRS401/DRL501";

        public string SN => "Rotation Door 01";

        public string Resource { get => PLC.Resource; set { PLC.Resource = value; } }

        /// <summary>
        /// Active which means active for User Interaction, basically is the socket which wait for input sn and test
        /// </summary>
        public int ActiveSocketIndex { get; private set; }
        public bool IsOpen => PLC.Port?.IsOpen ?? false;

        public bool IsInitialized => PLC.IsInitialized;

        public event EventHandler<DutMessage> DutIning;
        public event EventHandler<DutMessage> DutInDone;
        public event EventHandler<DutMessage> DutOuting;
        public event EventHandler<DutMessage> DutOuted;

        /// <summary>
        /// Need Monitoring the DUT Present Sensor
        /// </summary>
        public event EventHandler<DutMessage> OnDutPresent;
        /// <summary>
        /// Need Monitoring the DUT Present Sensor
        /// </summary>
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

        //FixtureManipulator Simulator = new FixtureManipulator();
        PLC_Mitsubishi_FX PLC = new PLC_Mitsubishi_FX();

        public bool FrontDoorState { get; private set; }
        public bool RearDoorState { get; private set; }
        public bool DutPresentState { get; private set; } = true;
        public bool DutInState { get; private set; }
        public bool DutOutState { get; private set; }
        public bool SafetyState { get; private set; } = true;

        public int CheckDutReady(out bool state, int slot = 0)
        {
            Info("CheckDutReady");
            state = true;
            //DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        public int Clear()
        {
            PLC?.Clear();
            //Simulator?.Close();
            Cleared?.Invoke(this, null);

            if (Application.Current?.MainWindow != null && IsInitialized)
            {
                Application.Current.MainWindow.PreviewKeyDown -= MainWindow_KeyDown;   // Need to be PreviewKeyDown, otherwise the KeyDown will filter the function key such as Space, return
            }
            return 1;
        }

        public int Close()
        {
            PLC?.Close();
            return 1;
        }

        internal void UpdateSocketIndex(int socketindex)
        {
            ActiveSocketIndex = socketindex;
        }

        public int CloseFrontDoor(int slot = 0)
        {
            //var currslot = ActiveSocketIndex > 0 ? 0 : 1;
            //if (slot != currslot)
            //{
            //    Warn($"Socket {slot} is outside, In Rotation Door, it will close socket base on current socket");
            //    slot = currslot;
            //}

            //FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot });
            //Info($"Close Front Door {slot}");
            //Thread.Sleep(1000);
            //FrontDoorState = true;
            //FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        public int CloseRearDoor(int slot = 0)
        {
            //RearDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot });
            //Info($"Close Rear Door {slot}");
            //Thread.Sleep(1000);
            //RearDoorState = true;
            //RearDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        /// <summary>
        /// Rotation happens on DUT In and out
        /// Ignore slot, for it detect the slot by hardware
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public int DutIn(int slot = 0)
        {
            //var currindex = ActiveSocketIndex;
            //ActiveSocketIndex = ActiveSocketIndex > 0 ? 0 : 1;
            //DutOuting?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            //DutIning?.Invoke(this, new DutMessage() { SocketIndex = currindex });
            //DutInDone?.Invoke(this, new DutMessage() { SocketIndex = currindex });
            //DutOuted?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            //Info($"DUT IN. Slot {currindex}, DUT Out for {ActiveSocketIndex}");
            return 1;
        }

        public int DutOut(int slot = 0)
        {
            //var currindex = ActiveSocketIndex;
            //ActiveSocketIndex = ActiveSocketIndex > 0 ? 0 : 1;
            //DutOuting?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });

            //var ch = ManipulationConfig.OutCommands["DutOut"];

            //var rtn = 0;
            //if(PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address))
            //{
            //    var data = PLC.ForceStatus(type, address, true);

            //    rtn = 1;
            //}

            //DutOuted?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            //Info($"DUT Out. Slot {currindex}, DUT Out for {ActiveSocketIndex}");
            //return rtn;
            return 1;
        }

        public int EmergencyStop()
        {
            MessageBox.Show($"EmergencyStop");
            Info($"EmergencyStop");
            return 1;
        }

        public int GetIDN(out string idn)
        {
            idn = SN;
            //MessageBox.Show($"GetIDN {idn}");
            return 1;
        }

        public int GetSlotActiveState(out bool state, int slot = 0)
        {

            if(slot == 0)
            {

            }
            state = slot == ActiveSocketIndex;
            return 1;
        }

        public int GetStateDutIn(out bool state, int slot = 0)
        {
            if (MessageBox.Show($"If slot: {slot} DUT is in?", "GetStateDutIn", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                state = true;
            }
            else
            {
                state = false;
            }
            return 1;
        }

        public int GetStateDutOut(out bool state, int slot = 0)
        {
            if (MessageBox.Show($"If slot: {slot} DUT is out?", "GetStateDutOut", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                state = true;
            }
            else
            {
                state = false;
            }
            return 1;
        }

        public int GetStateDutPresent(out bool state, int slot = 0)
        {
            state = DutPresentState;
            //if (MessageBox.Show($"If slot: {slot} DUT is present?", "GetStateDutPresent", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    state = true;
            //}
            //else
            //{
            //    state = false;
            //}
            return 1;
        }

        public int GetStateFrontDoorClose(out bool state, int slot = 0)
        {
            var ch = ManipulationConfig.InCommands["Door Closed"];

            PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);

            var data = PLC.ReadRegister(type, address, 1, out _, out state);

            if(state)
            {
                FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Closed" });
            }

            //if (MessageBox.Show($"If slot: {slot} Front Door is closed?", "GetStateFrontDoorClose", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    state = true;
            //}
            //else
            //{
            //    state = false;
            //}
            return 1;
        }

        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            var ch = ManipulationConfig.InCommands["Door Closed"];

            PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);

            var data = PLC.ReadRegister(type, address, 1, out _, out bool isclosed);

            state = !isclosed;
            //if (MessageBox.Show($"If slot: {slot} Front Door is Open?", "GetStateFrontDoorOpen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    state = true;
            //}
            //else
            //{
            //    state = false;
            //}
            return data;
        }

        public int GetStateSafety(out bool state, int slot = 0)
        {
            state = SafetyState;
            //if (MessageBox.Show($"If slot: {slot} Safety is OK?", "GetStateSafety", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    state = true;
            //}
            //else
            //{
            //    state = false;
            //}
            return 1;
        }

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            state = true;
            //if (MessageBox.Show($"If slot: {slot} Rear Door is Closed?", "GetStateRearDoorClose", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    state = true;
            //}
            //else
            //{
            //    state = false;
            //}
            return 1;
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            state = false;
            //if (MessageBox.Show($"If slot: {slot} Rear Door is Openned?", "GetStateRearDoorOpen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            //{
            //    state = true;
            //}
            //else
            //{
            //    state = false;
            //}
            return 1;
        }

        public int Initialize()
        {
            if (!IsInitialized)
            {
                if (System.IO.File.Exists(FilePath))
                {
                    ManipulationConfig.Update(FixtureManipulationConfig.Load(FilePath));
                }

                Initializing?.Invoke(this, null);
                Initialized?.Invoke(this, null);

                //Simulator.DataContext = this;
                //Simulator.Show();

                PLC?.Initialize();

                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.PreviewKeyDown += MainWindow_KeyDown;   // Need to be PreviewKeyDown, otherwise the KeyDown will filter the function key such as Space, return
                }

            }
            return 1;
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Back && e.KeyboardDevice?.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                SetLockState(0, false);
            }
        }

        public int Open()
        {
            //ActiveSocketIndex = RandomIndex.Next(2);
            if(!IsOpen)
            {
                PLC?.Open();
                SetLockState(0, false);
            }
            
            return 1;
        }

        public int OpenFrontDoor(int slot = 0)
        {
            //var currslot = ActiveSocketIndex > 0 ? 0 : 1;
            //if (slot != currslot)
            //{
            //    Warn($"Socket {slot} is outside, In Rotation Door, it will open socket base on current socket");
            //    slot = currslot;
            //}

            //FrontDoorOpening?.Invoke(this, new DutMessage() { SocketIndex = slot });
            //Info($"Open Fron Door {slot}");
            //Thread.Sleep(1500);
            //FrontDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        public int OpenRearDoor(int slot = 0)
        {
            //RearDoorOpening?.Invoke(this, new DutMessage() { SocketIndex = slot });
            //Info($"Open Rear Door {slot}");
            //Thread.Sleep(1500);
            //RearDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }


        public int SettingUI()
        {
            FixtureManipulationSetting fms = new FixtureManipulationSetting(FilePath, ManipulationConfig);
            return fms.ShowDialog() == true ? 1 : 0;
        }

        public int GetActiveSocketIndex()
        {
            var ch = ManipulationConfig.InCommands["Sensor A"];

            PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);

            PLC.ReadRegister(type, address, 1, out _, out bool isactiver);

            if (isactiver)
            {
                return 1;   // when sensor A detected, actually the active slot is 1 for op loading dut
            }
            else
            {
                ch = ManipulationConfig.InCommands["Sensor B"];

                if (PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type1, out address))
                {
                    PLC.ReadRegister(type1, address, 1, out _, out isactiver);

                    if (isactiver)
                    {
                        return 0;
                    }
                }
                else
                {
                    // No Sensor B
                    return 0;
                }
            }

            return -1;
        }

        public int GetLockState(int slotindex, out bool islocked)
        {
            var ch = ManipulationConfig.InCommands["Lock Test"];

            PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);
            return PLC.ReadRegister(type, address, 1, out _, out islocked);
        }

        public int SetLockState(int slotindex, bool islocked)
        {
            if (!islocked)
            {
                var ch = ManipulationConfig.OutCommands["Lock Test"];
                PLC_Mitsubishi_FX.ParseChannel(ch, out PLC_Mitsubishi_FX.RegisterType type, out short address);
                Debug($"Set Fixture slot {slotindex} Lock {islocked}");
                return PLC.ForceStatus(type, address, true);   // true is unlock in this driver
            }
            else
            {
                // Force Off does not work for this
                return 1;
            }
        }
    }

    
}
