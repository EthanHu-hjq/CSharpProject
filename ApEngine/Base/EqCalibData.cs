using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApEngine.Base
{
    public class EqCalibData
    {
        public int Index { get; set; }
        public string Channel { get => $"CH{Index}"; }

        public string EqPath { get; set; }

        public EqCalibData(int index)
        {
            Index = index;
        }

        public EqCalibData()
        { }
    }
}
