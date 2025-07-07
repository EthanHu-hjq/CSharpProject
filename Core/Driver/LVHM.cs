using NModbus;
using NModbus.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    internal class LVHM
    {
    }

    /// <summary>
    /// LVHM Line for large product
    /// </summary>
    public class LineL
    {

    }

    /// <summary>
    /// LVHM Line for sound bar
    /// </summary>
    public class LineS : TF_Base, IFixture
    {


        public int SocketCount => 1;

        public bool AutoDutIn { get; set; }
        public bool AutoDutOut { get; set; }

        public FixtureState State { get; private set; }

        public string Support => "LineS";
        public string Model { get; private set; } = "LineS";

        public string SN { get; private set; }

        public string Resource { get; set; }
        public int Port { get; set; } = 502;

        public bool IsOpen => false;

        public bool IsInitialized => false;

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

        public IModbusMaster Modbus { get; private set; }

        byte Modbus_SlaveAddr = 01;

        bool IsDutReady = false;

        /// <summary>
        /// .0
        /// </summary>
        const ushort REG_DUT_PRESENT = 560;  
        const ushort REG_DUT_ACCEPT = 570; // .0
        const ushort REG_DUT_REJECT = 570; // .1

        /// <summary>
        /// .0
        /// </summary>
        const ushort REG_DUT_READY = 561; 
        const ushort REG_DUT_TESTCOMPLETE = 571; // .0
        const ushort REG_DUT_RETEST = 571; // .1

        const ushort REG_TEST_RESET = 560; // .4
        const ushort REG_TEST_RESETDONE = 570; // .4

        public int CheckDutReady(out bool state, int slot = 0)
        {
            var rtn = Modbus.ReadHoldingRegisters(Modbus_SlaveAddr, REG_DUT_READY, 1);

            state = (rtn[0] & 1) == 1;

            if(state & !IsDutReady)
            {
                DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "DUT Ready" });
            }


            return 1;
        }

        public int Clear()
        {
            return 1;
        }

        public int Close()
        {
            return 1;
        }

        public int CloseFrontDoor(int slot = 0)
        {
            return 1;
        }

        public int CloseRearDoor(int slot = 0)
        {
            return 1;
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
            return 1;
        }

        public int GetIDN(out string idn)
        {
            idn = Model;
            return 1;
        }

        public int GetSlotActiveState(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateDutIn(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateDutOut(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        bool IsDutPresent = false;
        public int GetStateDutPresent(out bool state, int slot = 0)
        {
            var rtn = Modbus.ReadHoldingRegisters(Modbus_SlaveAddr, REG_DUT_PRESENT, 1);

            state = (rtn[0] & 1) == 1;

            if (state)
            {
                if(!IsDutPresent)
                {
                    OnDutPresent?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "DUT Present" });
                }    
                
                IsDutPresent = true;
            }
            else
            {
                if(IsDutPresent)
                {
                    OnDutAbsent?.Invoke(this, new DutMessage() { SocketIndex = slot, Message = "DUT Absent" });
                }
                IsDutPresent = false;
            }

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

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int GetStateSafety(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        static ModbusFactory Factory = new ModbusFactory();
        public int Initialize()
        {
            var currenttcpclient = new TcpClient(Resource, Port);
            Modbus = Factory.CreateMaster(currenttcpclient);

            return 1;
        }

        public int Open()
        {
            return 1;
        }

        public int OpenFrontDoor(int slot = 0)
        {
            return 1;
        }

        public int OpenRearDoor(int slot = 0)
        {
            return 1;
        }

        public int SettingUI()
        {
            throw new NotImplementedException();
        }
    }
}
