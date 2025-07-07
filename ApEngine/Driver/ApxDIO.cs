using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToucanCore.Abstraction.HAL;

namespace ApEngine.Driver
{
    public class ApxDIO : IRelayArray
    {
        public int ChannelCount => throw new NotImplementedException();

        public int OnValue_Stored => throw new NotImplementedException();

        public int OffValue_Stored => throw new NotImplementedException();

        public string Model => throw new NotImplementedException();

        public string SN => throw new NotImplementedException();

        public string Resource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsOpen => throw new NotImplementedException();

        public bool IsInitialized => throw new NotImplementedException();

        public event EventHandler Initializing;
        public event EventHandler Initialized;
        public event EventHandler Cleared;

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
            throw new NotImplementedException();
        }

        public int Reset()
        {
            throw new NotImplementedException();
        }

        public int SetRelay(bool state, params int[] channels)
        {
            throw new NotImplementedException();
        }

        public int SetRelay(int channleindex, bool state)
        {
            throw new NotImplementedException();
        }

        public int SetRelay(int value, int mask)
        {
            throw new NotImplementedException();
        }
    }
}
