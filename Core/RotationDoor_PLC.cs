using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ToucanCore.HAL;

namespace ToucanCore
{
    //public class RotationDoor_PLC : IFixture
    //{
    //    public int SocketCount { get; } = 2;
    //    public bool AutoDutIn { get; set; }

    //    public bool AutoDutOut { get; set; }
    //    public FixtureState State { get; private set; }

    //    public string Resource { get; set; }

    //    public string Model => "PLC";

    //    public string SN => string.Empty;

    //    public bool IsOpen => Port.IsOpen;

    //    public bool IsInitialized { get; private set; }

    //    public event EventHandler<DutMessage> DutIning;
    //    public event EventHandler<DutMessage> DutInDone;
    //    public event EventHandler<DutMessage> DutOuting;
    //    public event EventHandler<DutMessage> DutOuted;
    //    public event EventHandler<DutMessage> OnDutPresent;
    //    public event EventHandler<DutMessage> OnDutAbsent;
    //    public event EventHandler<DutMessage> FrontDoorOpening;
    //    public event EventHandler<DutMessage> FrontDoorOpened;
    //    public event EventHandler<DutMessage> FrontDoorClosing;
    //    public event EventHandler<DutMessage> FrontDoorClosed;
    //    public event EventHandler<DutMessage> RearDoorOpening;
    //    public event EventHandler<DutMessage> RearDoorOpened;
    //    public event EventHandler<DutMessage> RearDoorClosing;
    //    public event EventHandler<DutMessage> RearDoorClosed;
    //    public event EventHandler Initializing;
    //    public event EventHandler Initialized;
    //    public event EventHandler Cleared;
    //    public event EventHandler<DutMessage> EmergencyTrigged;
    //    public event EventHandler<DutMessage> FixtureError;

    //    private SerialPort Port;

    //    public Dictionary<string, string> IoMaps { get; } = new Dictionary<string, string>();

    //    public int ActiveSlotIndex { get; private set; }

    //    public int SetIO(string setting)
    //    {
    //        return 1;
    //    }

    //    public int Clear()
    //    {
    //        Port?.Close();
    //        Port?.Dispose();
    //        return 1;
    //    }

    //    public int CloseFrontDoor(int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int CloseRearDoor(int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int DutIn(int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int DutOut(int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int EmergencyStop()
    //    {
    //        return 1;
    //    }

    //    public int Initialize()
    //    {
    //        try
    //        {
    //            Port = new SerialPort(Resource);
    //            Port.BaudRate = 9600;
    //            Port.Parity = Parity.Even;
    //            Port.StopBits = StopBits.Two;
    //            Port.DataBits = 7;

    //            if (Port.IsOpen)
    //            {
    //                Port.Close();
    //            }
    //            Port.Open();

    //            IoMaps.Clear();
    //            IoMaps.Add("DoorOpened", "X1");
    //            IoMaps.Add("DoorClosed", "X2");
    //            IoMaps.Add("DutIn", "X3");
    //        }
    //        catch
    //        { }
    //        return 1;
    //    }

    //    public int OpenFrontDoor(int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int OpenRearDoor(int slot = 0)
    //    {
    //        return 1;
    //    }

    //    public int SetFixtureState(FixtureState state)
    //    {
    //        return 1;
    //    }

    //    public int Open()
    //    {
    //        if (!IsOpen)
    //        {
    //            Port.Open();
    //        }

    //        return 1;
    //    }

    //    public int Close()
    //    {
    //        if (IsOpen)
    //        {
    //            Port.Close();
    //        }

    //        return 1;
    //    }

    //    public int GetIDN(out string idn)
    //    {
    //        idn = SN;
    //        return 1;
    //    }

    //    public int GetStateDutIn(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateDutOut(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateDutPresent(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateSafety(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateFrontDoorOpen(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateFrontDoorClose(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateRearDoorOpen(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetStateRearDoorClose(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int GetSlotActiveState(out bool state, int slot = 0)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public int CheckDutReady(out bool state, int slot = 0)
    //    {
    //        state = true;
    //        DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot });
    //        return 1;
    //    }

    //    public int SettingUI()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public class Test
    //{
    //    public IFixture Fixture;

    //    public IRelayArray RelayArray;

    //    public bool AutoMode;

    //    public int[][] SlotRelays;

    //    public void TestFixture(string sn)
    //    {
    //        Fixture = new RotationDoor_PLC() { Resource = "COM1" };
    //        Fixture.Initialize();
    //        Fixture.Open();

    //        SlotRelays = new int[Fixture.SocketCount][];

    //        var activeslot = -1;

    //        if (Fixture is RotationDoor_PLC)
    //        {
    //            for (int i = 0; i < Fixture.SocketCount; i++)
    //            {
    //                Fixture.GetSlotActiveState(out bool state, i);

    //                if (state)
    //                {
    //                    activeslot = i;
    //                    break;
    //                }
    //            }
    //        }

    //        StartNewTest(sn, activeslot);
    //    }

    //    public void StartNewTest(string sn, int slot)
    //    {
    //        if (AutoMode)
    //        {
    //            Fixture.OpenFrontDoor();
    //            Fixture.DutIn();

    //            int[] slotrelay = SlotRelays[slot];
    //            RelayArray.SetRelay(true, slotrelay);

    //            // Engine.Run();

    //            RelayArray.SetRelay(false, slotrelay);

    //            Fixture.DutOut(slot);
    //            Fixture.OpenFrontDoor(slot);
    //        }
    //        else
    //        {

    //            bool frontdoorstate = false;

    //            while (!frontdoorstate)
    //            {
    //                Fixture.GetStateFrontDoorClose(out bool temp, slot);
    //                frontdoorstate = temp;

    //                if (!temp)
    //                {
    //                    Thread.Sleep(200);
    //                }
    //            }

    //            bool dutinstate = false;

    //            while (!dutinstate)
    //            {
    //                Fixture.GetStateFrontDoorClose(out bool temp, slot);
    //                dutinstate = temp;

    //                if (!temp)
    //                {
    //                    Thread.Sleep(200);
    //                }
    //            }

    //            int[] slotrelay = SlotRelays[slot];
    //            RelayArray.SetRelay(true, slotrelay);

    //            // Engine.Run();

    //            RelayArray.SetRelay(false, slotrelay);
    //        }
    //    }
    //}

}
