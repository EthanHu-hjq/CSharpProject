using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.HAL
{
    public interface ISerialNumberReader : IHardware
    {
        string ReadSerialNumber();
    }
}
