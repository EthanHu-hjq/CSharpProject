using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.HAL
{
    //public interface ISerialNumberReader : IHardware
    //{
    //    string ReadSerialNumber();
    //}

    /// <summary>
    /// If one slot one reader // or one fixture one reader, or one station one reader
    /// </summary>
    public class SerialNumberReader_None : ISerialNumberReader
    {
        public string Model => "None";

        public string SN => "null";

        public string Resource { get; set; }

        public bool IsOpen => true;

        public bool IsInitialized => true;

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

        public string ReadSerialNumber()
        {
            throw new NotImplementedException();
        }
    }
}
