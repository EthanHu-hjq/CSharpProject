using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApEngine.Base
{
    public class Calib_AcousticOutput
    {
        [XmlAttribute("enable")]
        public bool Enable { get; set; } = true;
        public AudioPrecision.API.OutputConnectorType ConnectorType { get; set; }
        [XmlAttribute("voltratio")]
        public double VoltageRatio { get; set; } = double.NaN;
        [XmlAttribute("reffreq")]
        public double RefFreq { get; set; } = double.NaN;
    }
}
