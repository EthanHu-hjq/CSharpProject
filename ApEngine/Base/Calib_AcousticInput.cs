using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ApEngine.Base
{
    [XmlRoot("AcousticInput")]
    public class Calib_AcousticInput
    {
        [XmlAttribute("enable")]
        public bool Enable { get; set; }
        [XmlAttribute("level")]
        public string Level { get; set; } //= 94; //dBSPL
        [XmlAttribute("frequency")]
        public double Frequency { get; set; } //= 1000; //Hz
        [XmlAttribute("tolerance")]
        public double Tolerance { get; set; } //= 10; // Hz 

        public AudioPrecision.API.InputConnectorType ConnectorType { get; set; }

        [XmlArray]
        public List<Calib_AcousticInputChannel> Channels { get; set; }
    }

    public class Calib_AcousticInputChannel : ICloneable
    {
        public static double SensitivityUnitIndex = 1e3; // Update to mV //  nV

        public int Index { get; set; }
        [XmlIgnore]
        public string Channel { get => $"Ch{Index+1}"; }
        [XmlIgnore]
        public double Level { get; set; }  // just for UI
        public string SerialNo { get; set; }
        public double Sensitivity { get; set; } = 10;
        public double Frequency { get; set; } = 1000; // Hz
        public double Sensitivity_Expected { get; set; } = 10; //mv/Pa
        public double Sensitivity_Tolerance { get; set; } = 1; // dB

        public Calib_AcousticInputChannel()
        {
        }

        public object Clone()
        {
            Calib_AcousticInputChannel clone = new Calib_AcousticInputChannel()
            {
                Index = Index,
                Level = Level,
                SerialNo = SerialNo,
                Sensitivity = Sensitivity,
                Sensitivity_Expected = Sensitivity_Expected,
                Sensitivity_Tolerance = Sensitivity_Tolerance,
                Frequency = Frequency,
            };
            return clone;
        }
    }
}
