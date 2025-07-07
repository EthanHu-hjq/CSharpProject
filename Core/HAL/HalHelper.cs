using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ToucanCore.HAL
{
    public static class HalHelper
    {
        public static Regex RE_IpAddr = new Regex(@"\d+\.\d+\.\d+\.\d+");
        public static string[] TRUE_STRING { get; } = { "1", "true", "True", "TRUE" };
    }
}
