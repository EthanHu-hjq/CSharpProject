using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TestCore;
using TestCore.Ctrls;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    /// <summary>
    /// For Rotation Door Test
    /// </summary>
    public class FixtureDemo : TF_Base, IFixture, IAutoActiveSlotFixture
    {
        public int SocketCount => 2;
        public bool AutoDutIn { get; set; }

        public bool AutoDutOut { get; set; }
        public FixtureState State { get; }

        public string Support => "Demo";
        public string Model => "Fixture Demo";

        public string SN => "Demo 01";

        public string Resource { get; set; }

        /// <summary>
        /// Active which means active for User Interaction, basically is the socket which wait for input sn and test
        /// </summary>
        public int ActiveSocketIndex { get; private set; }
        public bool IsOpen => true;

        public bool IsInitialized => true;

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

        //FixtureDemoSimulator Simulator = new FixtureDemoSimulator();

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
            //Simulator?.Hide();   // async clear will throw thread exception
            Cleared?.Invoke(this, null);
            return 1;
        }

        public int Close()
        {
            return 1;
        }

        internal void UpdateSocketIndex(int socketindex)
        {
            ActiveSocketIndex = socketindex;
        }

        public int CloseFrontDoor(int slot = 0)
        {
            var currslot = ActiveSocketIndex > 0 ? 0 : 1;
            if(slot != currslot)
            {
                Warn($"Socket {slot} is outside, In Rotation Door, it will close socket base on current socket");
                slot = currslot;
            }

            FrontDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot });
            Info($"Close Front Door {slot}");
            Thread.Sleep(1000);
            FrontDoorState = true;
            FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        public int CloseRearDoor(int slot = 0)
        {
            RearDoorClosing?.Invoke(this, new DutMessage() { SocketIndex = slot });
            Info($"Close Rear Door {slot}");
            Thread.Sleep(1000);
            RearDoorState = true;
            RearDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot });
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
            var currindex = ActiveSocketIndex;
            ActiveSocketIndex = ActiveSocketIndex > 0 ? 0 : 1;
            DutOuting?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            DutIning?.Invoke(this, new DutMessage() { SocketIndex = currindex });
            DutInDone?.Invoke(this, new DutMessage() { SocketIndex = currindex });
            DutOuted?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            Info($"DUT IN. Slot {currindex}, DUT Out for {ActiveSocketIndex}");
            return 1;
        }

        public int DutOut(int slot = 0)
        {
            var currindex = ActiveSocketIndex;
            ActiveSocketIndex = ActiveSocketIndex > 0 ? 0 : 1;
            DutOuting?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            DutIning?.Invoke(this, new DutMessage() { SocketIndex = currindex });
            DutInDone?.Invoke(this, new DutMessage() { SocketIndex = currindex });
            DutOuted?.Invoke(this, new DutMessage() { SocketIndex = ActiveSocketIndex });
            Info($"DUT IN. Slot {currindex}, DUT Out for {ActiveSocketIndex}");
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
            MessageBox.Show($"GetIDN {idn}");
            return 1;
        }

        public int GetSlotActiveState(out bool state, int slot = 0)
        {
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
            if (MessageBox.Show($"If slot: {slot} Front Door is closed?", "GetStateFrontDoorClose", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                state = true;
                FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Closed" });
            }
            else
            {
                state = false;
            }
            return 1;
        }

        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            if (MessageBox.Show($"If slot: {slot} Front Door is Open?", "GetStateFrontDoorOpen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                state = true;
                FrontDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "Front Door Opened" });
            }
            else
            {
                state = false;
            }
            return 1;
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
            if (MessageBox.Show($"If slot: {slot} Rear Door is Closed?", "GetStateRearDoorClose", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                state = true;
            }
            else
            {
                state = false;
            }
            return 1;
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            if (MessageBox.Show($"If slot: {slot} Rear Door is Openned?", "GetStateRearDoorOpen", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                state = true;
            }
            else
            {
                state = false;
            }
            return 1;
        }

        public int Initialize()
        {
            Initializing?.Invoke(this, null);
            Initialized?.Invoke(this, null);

            //Simulator.DataContext = this;
            //Simulator.Show();

            return 1;
        }

        Random RandomIndex = new Random();

        public int Open()
        {
            ActiveSocketIndex = RandomIndex.Next(2);
            return 1;
        }

        public int OpenFrontDoor(int slot = 0)
        {
            var currslot = ActiveSocketIndex > 0 ? 0 : 1;
            if (slot != currslot)
            {
                Warn($"Socket {slot} is outside, In Rotation Door, it will open socket base on current socket");
                slot = currslot;
            }

            FrontDoorOpening?.Invoke(this, new DutMessage() { SocketIndex = slot });
            Info($"Open Fron Door {slot}");
            Thread.Sleep(1500);
            FrontDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        public int OpenRearDoor(int slot = 0)
        {
            RearDoorOpening?.Invoke(this, new DutMessage() { SocketIndex = slot });
            Info($"Open Rear Door {slot}");
            Thread.Sleep(1500);
            RearDoorOpened?.Invoke(this, new DutMessage() { SocketIndex = slot });
            return 1;
        }

        public int SettingUI()
        {
            throw new NotImplementedException();
        }

        public int GetActiveSocketIndex()
        {
            return 0;
        }
    }
}
