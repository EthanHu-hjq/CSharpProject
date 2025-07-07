using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApEngine.Base
{
    public class Calib_AnalogOutput
    {
        [XmlAttribute("enable")]
        public bool Enable { get; set; }
        public AudioPrecision.API.OutputConnectorType ConnectorType { get; set; }
        [XmlAttribute("dbrg")]
        public string dBrG { get; set; } //= 100; //mVrms
        [XmlAttribute("dbm")]
        public string dBm { get; set; } // = 600; // ohm
        [XmlAttribute("watts")]
        public string Watts { get; set; } //= 8; // ohm
    }
}
