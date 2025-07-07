using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.HAL
{
    public class DutMessage
    {
        public int SocketIndex { get; set; }
        public string Message { get; set; }
        public object Tag { get; set; }
    }
}
