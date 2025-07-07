using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ToucanCore.Abstraction.HAL
{
    public static class HalHelper
    {
        public static Regex RE_IpAddr = new Regex(@"\d+\.\d+\.\d+\.\d+");
        public static string[] TRUE_STRING { get; } = { "1", "true", "True", "TRUE" };
    }
}
