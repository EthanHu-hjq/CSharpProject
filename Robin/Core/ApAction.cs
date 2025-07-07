using ApEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace Robin.Core
{
    public class ApAction
    {
        double dBrG { get; set; }
        /// <summary>
        /// DO1: K1. XLR. determinating if inject into 6R8, for seft test or calibration
        /// DO2: K2, PHONE. determinating if inject into 6R8, for seft test or calibration
        /// DO3: High Voltage Input Switch, determinating if inject 301k Res into Analog Balance Input. Connect to K20 Amp
        /// Default with 301k, true for no 301k. if true for brenchmark Amp Test
        /// DO4: Test Progress LED, Door Light
        /// 
        /// D01 and DO2 Should not turn ON in same time. If Both off, the signal could inject from DCX
        /// </summary>
        byte AuxOutState { get; set; }

        public bool? DO1 { get; set; }
        public bool? DO2 { get; set; }
        public bool? DO3 { get; set; }
        public bool? DO4 { get; set; }

        byte Mask = 0b11110000;

        public void Execute()
        {
            //var current = ApxEngine.ApRef.AuxControlMonitor.AuxControlOutputValue;
            var current = ApxEngine.AuxControlOutputValue;
            if (DO1 is bool do1)
            {
                byte val = (byte)(Mask + (byte)(do1 ? 1 : 0));
                current &= val;
            }

            if (DO2 is bool do2)
            {
                byte val = (byte)(Mask + (byte)(do2 ? 2 : 0));
                current &= val;
            }

            if (DO3 is bool do3)
            {
                byte val = (byte)(Mask + (byte)(do3 ? 4 : 0));
                current &= val;
            }

            if (DO4 is bool do4)
            {
                byte val = (byte)(Mask + (byte)(do4 ? 8 : 0));
                current &= val;
            }

            //ApxEngine.ApRef.AuxControlMonitor.AuxControlOutputValue = current;
            ApxEngine.AuxControlOutputValue = current;
        }
    }
}
