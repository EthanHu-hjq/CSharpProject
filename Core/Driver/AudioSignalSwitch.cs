using NModbus.Device;
using NModbus.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    public class AudioSignalSwitch : TF_Base, IRelayArray
    {
        public int ChannelCount => 24;

        public int OnValue_Stored => throw new NotImplementedException();

        public int OffValue_Stored => throw new NotImplementedException();

        public string Model => "Audio Signal Switch";

        public string SN => string.Empty;

        public string Resource { get; set; }

        public bool IsOpen { get; private set; }

        public bool IsInitialized { get; private set; }

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

        ModbusMaster ModbusMaster;

        public int Clear()
        {
            throw new NotImplementedException();
        }

        public int Close()
        {
            throw new NotImplementedException();
        }

        public int GetIDN(out string idn)
        {
            throw new NotImplementedException();
        }

        public int Initialize()
        {
            throw new NotImplementedException();
        }

        public int Open()
        {
            ModbusTransport transport = new ModbusIpTransport(null, null, null);
            ModbusMaster = new ModbusIpMaster((transport));

            ModbusMaster.ReadCoils(01, 02, 1);
            return 1;
        }

        public int Reset()
        {
            throw new NotImplementedException();
        }

        public int SetRelay(bool state, params int[] channels)
        {
            ModbusMaster.WriteSingleRegister(01, 00, 01);
            ModbusMaster.WriteMultipleRegisters(01, 00, null);

            return 1;
        }

        public int SetRelay(int channleindex, bool state)
        {
            throw new NotImplementedException();
        }

        public int SetRelay(int value, int mask = 65535)
        {
            throw new NotImplementedException();
        }
    }
}
