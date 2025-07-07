using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ToucanCore.Driver
{
    internal class PLC_Panasonic
    {
        public const string CMD_GET_VERSION = "%01#RDD0030000303**";
        public const string CMD_SET_AUTOMODE = "%01#WDD00100001010000000051";
        public const string CMD_SET_HALFAUTOMODE = "%01#WDD00100001010100000050";
        public const string CMD_SET_MANUALMODE = "%01#WDD00100001010200000053";
        public const string CMD_GET_BIT = "%01#RCSR0201**";
        public const string CMD_SET_PROBE = "%01#WCSR02021**";
        public const string CMD_GET_PROBEREADY = "%01#RCSR0200**";

        public const string CMD_SET_PASSED = "%01#WDD00250002510100000050";
        public const string CMD_SET_FAILED = "%01#WDD00250002510200000053";

        public const string CMD_SET_STARTTEST = "%01#RCSR0201**";
        public const string CMD_GET_MODE = "%01#RDD001000010154";
        public const string CMD_GET_ENABLESCANNING = "%01#RCSR3500**";
        public const string CMD_SET_SCANSNDONE = "%01#WCSR31311**";
        public const string CMD_SET_SKIPTEST = "%01#WCSR31251**";
        public const string CMD_SET_DUTREADY = "%01#WCSR31261**";
        public const string CMD_SET_UNKNOWN = "%01#WDD002520025302000000**";

        public const string CMD_GET_DUTPRESENT = "%01#RCSR400B**";




    }
}
