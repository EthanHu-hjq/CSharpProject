using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore.Base;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.HAL
{
    //public interface IFixture : IHardware
    //{
    //    /// <summary>
    //    /// Support Hardware
    //    /// </summary>
    //    string Support { get; }

    //    /// <summary>
    //    /// Slot Index start from 0 in program
    //    /// </summary>
    //    int SocketCount { get; }

    //    /// <summary>
    //    /// Property for execution
    //    /// </summary>
    //    bool AutoDutIn { get; set; }
    //    /// <summary>
    //    /// Property for execution
    //    /// </summary>
    //    bool AutoDutOut { get; set; }

    //    FixtureState State { get; }

    //    event EventHandler<DutMessage> DutIning;
    //    event EventHandler<DutMessage> DutInDone;
    //    event EventHandler<DutMessage> DutOuting;
    //    event EventHandler<DutMessage> DutOuted;

    //    /// <summary>
    //    /// Especially for automation, detect picking up and placing
    //    /// </summary>
    //    event EventHandler<DutMessage> OnDutPresent;
    //    event EventHandler<DutMessage> OnDutAbsent;

    //    event EventHandler<DutMessage> FrontDoorOpening;
    //    event EventHandler<DutMessage> FrontDoorOpened;
    //    event EventHandler<DutMessage> FrontDoorClosing;
    //    event EventHandler<DutMessage> FrontDoorClosed;

    //    event EventHandler<DutMessage> RearDoorOpening;
    //    event EventHandler<DutMessage> RearDoorOpened;
    //    event EventHandler<DutMessage> RearDoorClosing;
    //    event EventHandler<DutMessage> RearDoorClosed;

    //    event EventHandler<DutMessage> EmergencyTrigged;
    //    event EventHandler<DutMessage> FixtureError;

    //    /// <summary>
    //    /// Let DUT in. if there are processes such as pallet in --> jig press down. return if CMD send OK.
    //    /// not wait for execution succeed
    //    /// Should execute before DOOR Closed
    //    /// </summary>
    //    /// <param name="slot"></param>
    //    /// <returns>CMD send OK.</returns>
    //    int DutIn(int slot = 0);

    //    /// <summary>
    //    /// Let DUT Out. if there are processes such as jig up --> pallet out. return if CMD send OK.
    //    /// not wait for execution succeed
    //    /// Should execute after DOOR Opened
    //    /// </summary>
    //    /// <param name="slot"></param>
    //    /// <returns>CMD send OK.</returns>
    //    int DutOut(int slot = 0);

    //    /// <summary>
    //    /// Get Sensor State of Dut in. return > 0 means succeed. <0 means error, =0 means no action
    //    /// </summary>
    //    /// <param name="state">Sensor State</param>
    //    /// <param name="slot"></param>
    //    /// <returns></returns>
    //    int GetStateDutIn(out bool state, int slot = 0);
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    /// <param name="state"></param>
    //    /// <param name="slot"></param>
    //    /// <returns></returns>
    //    int GetStateDutOut(out bool state, int slot = 0);

    //    /// <summary>
    //    /// Check DUT if ready. if Ready, then it should be start to Test
    //    /// it mightbe impact from Door, Dut state, Protection, etc
    //    /// </summary>
    //    /// <param name="state"></param>
    //    /// <param name="slot"></param>
    //    /// <returns></returns>
    //    int CheckDutReady(out bool state, int slot = 0);

    //    /// <summary>
    //    /// DUT Present Sensor
    //    /// </summary>
    //    /// <param name="state"></param>
    //    /// <param name="slot"></param>
    //    /// <returns></returns>
    //    int GetStateDutPresent(out bool state, int slot = 0);

    //    /// <summary>
    //    /// Safety Sensor
    //    /// </summary>
    //    /// <param name="state"></param>
    //    /// <param name="slot"></param>
    //    /// <returns></returns>
    //    int GetStateSafety(out bool state, int slot = 0);

    //    int OpenFrontDoor(int slot = 0);
    //    int CloseFrontDoor(int slot = 0);
    //    int GetStateFrontDoorOpen(out bool state, int slot = 0);
    //    int GetStateFrontDoorClose(out bool state, int slot = 0);

    //    int OpenRearDoor(int slot = 0);
    //    int CloseRearDoor(int slot = 0);
    //    int GetStateRearDoorOpen(out bool state, int slot = 0);
    //    int GetStateRearDoorClose(out bool state, int slot = 0);

    //    int GetSlotActiveState(out bool state, int slot = 0);

    //    /// <summary>
    //    /// Setting the Fixture, such as the map of io --> sensor, cylinder
    //    /// should be UI
    //    /// </summary>
    //    /// <returns></returns>
    //    int SettingUI();

    //    int EmergencyStop();
    //}

    //public interface IAutoActiveSlotFixture : IFixture
    //{
    //    int ActiveSocketIndex { get; }

    //    /// <summary>
    //    /// For the active Socket might be change manually, it's neccesary to get it with this method
    //    /// </summary>
    //    /// <returns></returns>
    //    int GetActiveSocketIndex();
    //}

    //public interface ILockableFixture : IFixture
    //{
    //    int GetLockState(int slotindex, out bool islocked);
    //    int SetLockState(int slotindex, bool islocked);
    //}

    public class Fixture_None : IFixture
    {
        public int SocketCount => 1;

        public FixtureState State => throw new NotImplementedException();

        public string Support => "None";
        public string Model => "None";

        public string SN => "None";

        public string Resource { get; set; }

        public bool IsOpen => true;

        public bool IsInitialized => true;

        public bool AutoDutIn { get; set; }

        public bool AutoDutOut { get; set; }

        public int ActiveSlotIndex => 0;

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

        public int CheckDutReady(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int Clear()
        {
            throw new NotImplementedException();
        }

        public int Close()
        {
            throw new NotImplementedException();
        }

        public int CloseFrontDoor(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int CloseRearDoor(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int DutIn(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int DutOut(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int EmergencyStop()
        {
            throw new NotImplementedException();
        }

        public int GetIDN(out string idn)
        {
            throw new NotImplementedException();
        }

        public int GetSlotActiveState(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateDutIn(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateDutOut(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateDutPresent(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateFrontDoorClose(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateSafety(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int Initialize()
        {
            throw new NotImplementedException();
        }

        public int Open()
        {
            throw new NotImplementedException();
        }

        public int OpenFrontDoor(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int OpenRearDoor(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int SettingUI()
        {
            throw new NotImplementedException();
        }
    }

    //[Flags]
    //public enum FixtureState
    //{
    //    Initializing,
    //    Initialized,
    //    OK,
    //    Warning,
    //    Error,
    //    EmergencyStop,
    //}

    //public enum FixtureEvent
    //{
    //    FrontDoorOpening,
    //    FrontDoorOpened,
    //    FrontDoorClosing,
    //    FrontDoorClosed,
    //}

    //public enum FixtureWorkMode
    //{
    //    /// <summary>
    //    /// Scan SN -> (Trigger Test / DUT In -> Door Close) -> Start Test
    //    /// </summary>
    //    Manual,

    //    /// <summary>
    //    /// (Trigger Test / DUT In -> Door Close) -> Auto Scan SN -> Start Test
    //    /// </summary>
    //    ManualWithAutoScanner,

    //    /// <summary>
    //    /// Dut Reader Get SN -> (DUT In -> Door Close) -> Start Test
    //    /// </summary>
    //    AutomationWithDutReader,

    //    /// <summary>
    //    /// DUT On Present -> (DUT In -> Door Close) -> Start Test
    //    /// </summary>
    //    AutomationWithoutReader,
    //}

    ///// <summary>
    ///// Out means set the fixture, if there is status or parameter, it should not specified in Drivers
    ///// </summary>
    //public enum FixtureManipulation_In
    //{
    //    // For In
    //    FrontDoorClosed,
    //    FrontDoorOpened,
    //    RearDoorClosed,
    //    RearDoorOpened,
    //    /// <summary>
    //    /// Holder 是否入仓
    //    /// </summary>
    //    DutIn,
    //    /// <summary>
    //    /// Holder 是否出仓
    //    /// </summary>
    //    DutOut,
    //    /// <summary>
    //    /// DUT 是否在Holdershang
    //    /// </summary>
    //    DutPresent,
    //    /// <summary>
    //    /// DUT测试前置条件是否具备, 如辅助扣压装置等
    //    /// </summary>
    //    DutReady,

    //    /// <summary>
    //    /// 安全措施是否正常, 如光栅信号, 安全继电器信号等
    //    /// </summary>
    //    Safety,

    //    EmergencyStop,

    //    SlotIndexSensor,
    //}

    //public enum FixtureManipulation_Out
    //{
    //    FrontDoor,
    //    DutPallet,

    //    EmergencyStop,
    //    StartTest,

    //    Lock,

    //    // Reserved

    //    LED_PASS,
    //    LED_FAIL,

    //    LED_NORMAL,
    //    LED_WARN,
    //    LED_SUSPEND,
    //}
}
