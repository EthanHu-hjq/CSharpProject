using NModbus;
using NModbus.Device;
using NModbus.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCore;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class ATC04 : TF_Base, IFixture
    {
        public int SocketCount => 1;

        public bool AutoDutIn { get; set; }
        public bool AutoDutOut { get; set; }

        public FixtureState State { get; private set; }

        public string Support => "SAC01 ATC04 BSS703";
        public string Model => "SAC01 ATC04 BSS703";

        public string SN => "";

        public string Resource { get; set; }

        public bool IsOpen { get; private set; }

        public bool IsInitialized { get; private set; }

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

        public int CheckDutReady(out bool state, int slot = 0)   /// 可测试
        {
            var rs = ModbusMaster.ReadHoldingRegisters(0, 561, 1);

            state = (rs[0] & 0x01) > 0;
            if(state)
            {
                DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot });
            }

            Info($"Get REG 561, rtn {rs[0]}");
            
            return 1;
        }

        public int Clear()
        {
            return 1;
        }

        public int Close()
        {
            ModbusMaster?.Dispose();
            return 1;
        }

        public int CloseFrontDoor(int slot = 0)
        {
            return 1; // Cannot op manually
        }

        public int CloseRearDoor(int slot = 0)
        {
            return 1;
        }

        public int DutIn(int slot = 0)  // RFID OK
        {
            ModbusMaster.WriteSingleRegister(0, 570, 0x01);
            return 1;
        }

        public int DutOut(int slot = 0)
        {
            return 1;
        }

        public int EmergencyStop()
        {
            throw new NotImplementedException();
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
            var rs = ModbusMaster.ReadHoldingRegisters(0, 561, 1);

            state = (rs[0] & 0x01) > 0;

            if (state)
            {
                DutInDone?.Invoke(this, new DutMessage() { SocketIndex = slot });
            }
            Info($"Get REG 561, rtn {rs[0]}");
            return 1;
        }

        public int GetStateDutOut(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateDutPresent(out bool state, int slot = 0)   /// RFID 可读
        {
            var rs = ModbusMaster.ReadHoldingRegisters(0, 560, 1);
            
            if ((rs[0] & 0x10) > 0)   // PLC Reset, D560.4
            {
                ModbusMaster.WriteSingleRegister(0, 570, 0x10);
                Info($"Set REG 570, 0x10");
                Thread.Sleep(50);
                rs = ModbusMaster.ReadHoldingRegisters(0, 560, 1);
                
            }

            state = (rs[0] & 0x01) > 0;
            Info($"Get REG 560, rtn {rs[0]}");
            if (state)
            {
                OnDutPresent?.Invoke(this, new DutMessage() { SocketIndex = slot });
            }

            return 1;
        }

        public int GetStateFrontDoorClose(out bool state, int slot = 0)   // Check If Reset OK
        {
            var rs = ModbusMaster.ReadHoldingRegisters(0, 560, 1);
            state = (rs[0] & 0x10) == 0;
            
            if (state)
            {
                FrontDoorClosed?.Invoke(this, new DutMessage() { SocketIndex = slot });
            }

            return 1;
        }

        public int GetStateFrontDoorOpen(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateRearDoorClose(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateRearDoorOpen(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        public int GetStateSafety(out bool state, int slot = 0)
        {
            state = true;
            return 1;
        }

        IModbusFactory modbusFactory = null;
        public int Initialize()
        {
            modbusFactory = new ModbusFactory();
            return 1;
        }

        IModbusMaster ModbusMaster;
        public int Open()
        {
            var tcpclient = new TcpClient();
            tcpclient.Connect(new IPEndPoint(IPAddress.Parse(Resource), 502));
            ModbusMaster = modbusFactory.CreateMaster(tcpclient);
            ModbusMaster.Transport.WriteTimeout = 3000;
            ModbusMaster.Transport.ReadTimeout = 3000;
            Info($"Open {Resource}");
            return 1;
        }

        public int OpenFrontDoor(int slot = 0)
        {
            ModbusMaster.WriteSingleRegister(0, 571, 1);
            Info($"Set REG 571, 0x01");
            return 1;
        }

        public int OpenRearDoor(int slot = 0)
        {
            throw new NotImplementedException();
        }

        public int SettingUI()
        {
            throw new NotImplementedException();
        }

        public int SkipDut(int slot=0)  // RFID NG
        {
            ModbusMaster.WriteSingleRegister(0, 570, 0x02);
            Info($"Set REG 571, 0x02");
            return 1;
        }
    }
}
