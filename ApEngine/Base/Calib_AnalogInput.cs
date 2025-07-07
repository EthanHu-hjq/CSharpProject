using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApEngine.Base
{
    public class Calib_AnalogInput
    {
        [XmlAttribute("enable")]
        public bool Enable { get; set; }

        public AudioPrecision.API.InputConnectorType ConnectorType { get; set; }

        [XmlAttribute("unit")]
        public string Unit { get; set; }

        [XmlAttribute("dbm")]
        public string dBm { get; set; }// = 600;
        [XmlAttribute("watts")]
        public string Watts { get; set; }// = 8;

        public string dBSpl1CalibratorLevel { get; set; }// = 94; // dBSPL
        public string dBSpl2CalibratorLevel { get; set; }// = 94; // dBSPL

        public string dBrA { get; set; }
        public string dBrB { get; set; }
        public string dBSpl1 { get; set; }
        public string dBSpl2 { get; set; }
        public string dBrAOffset { get; set; }
        public string dBrBOffset { get; set; }
    }

    //public class Calib_AnalogInputChannel
    //{
    //    public int Index { get; set; }
    //    [XmlIgnore]
    //    public string Channel { get => $"CH{Index+1}"; }  // In AP, it might be CH, Ch
    //    public double Frequency { get; set; } = 1000; // Hz

    //    public double dBSPL1 { get; set; } = 10;
    //    public double dBSPL2 { get; set; } = 10;

    //    public double dBrA { get; set; }
    //    public double dBrB { get; set; }

    //    public Calib_AnalogInputChannel(int index)
    //    {
    //        Index = index;
    //    }

    //    public Calib_AnalogInputChannel()
    //    {
    //    }
    //}
}
