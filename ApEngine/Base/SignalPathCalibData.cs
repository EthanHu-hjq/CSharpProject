using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ApEngine.Base
{
    public class SignalPathCalibData : IXmlSerializable
    {
        public Calib_AcousticInput AcousticInput { get; set; }
        public Calib_AnalogInput AnalogInput { get; set; }
        public Calib_AcousticOutput AcousticOutput { get; set; }
        public Calib_AnalogOutput AnalogOutput { get; set; }

        public EqCalibData InputEq { get; set; } = new EqCalibData();
        public EqCalibData OutputEq { get; set; } = new EqCalibData();
        // For Measurement
        public Calib_LoudspeakerProductionTest[] LoudspeakerProductionTests { get; set; }
        // For Measurement
        public Calib_ImpedanceThieleSmall[] ImpedanceThieleSmalls { get; set; }

        [XmlArray]
        public EqTableFile[] EqTableFiles { get; set; }
        public Dictionary<string, string> MultitoneAnalyzer { get; } = new Dictionary<string, string>();

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool isEmpty = reader.IsEmptyElement;

            if (isEmpty) return;

            reader.Read();

            var acousticinput = new XmlSerializer(typeof(Calib_AcousticInput));
            var acousticoutput = new XmlSerializer(typeof(Calib_AcousticOutput));
            var analoginput = new XmlSerializer(typeof(Calib_AnalogInput));
            var analogoutput = new XmlSerializer(typeof(Calib_AnalogOutput));
            var eqcalib = new XmlSerializer(typeof(EqCalibData));
            var eqfiles = new XmlSerializer(typeof(EqTableFile[]));
            var imps = new XmlSerializer(typeof(Calib_ImpedanceThieleSmall[]));
            var lspts = new XmlSerializer(typeof(Calib_LoudspeakerProductionTest[]));

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (acousticinput.CanDeserialize(reader))
                {
                    AcousticInput = acousticinput.Deserialize(reader) as Calib_AcousticInput;
                }

                if (acousticoutput.CanDeserialize(reader))
                {
                    AcousticOutput = acousticoutput.Deserialize(reader) as Calib_AcousticOutput;
                }

                if (analoginput.CanDeserialize(reader))
                {
                    
                    AnalogInput = analoginput.Deserialize(reader) as Calib_AnalogInput;
                }
                
                if (analogoutput.CanDeserialize(reader))
                {
                    AnalogOutput = analogoutput.Deserialize(reader) as Calib_AnalogOutput;
                }

                if (eqfiles.CanDeserialize(reader))
                {
                    EqTableFiles = eqfiles.Deserialize(reader) as EqTableFile[];
                }

                if (imps.CanDeserialize(reader))
                {
                    ImpedanceThieleSmalls = imps.Deserialize(reader) as Calib_ImpedanceThieleSmall[];
                }

                if (lspts.CanDeserialize(reader))
                {
                    LoudspeakerProductionTests = lspts.Deserialize(reader) as Calib_LoudspeakerProductionTest[];
                }

                if (reader.Name == "InputEq")
                {
                    reader.ReadStartElement("InputEq");
                    if (eqcalib.CanDeserialize(reader))
                    {
                        if (eqcalib.Deserialize(reader) is EqCalibData data)  // the inputeq and output eq should not be null
                        {
                            InputEq = data;
                        }
                    }
                    reader.ReadEndElement();
                }

                if (reader.Name == "OutputEq")
                {
                    reader.ReadStartElement("OutputEq");
                    if (eqcalib.CanDeserialize(reader))
                    {
                        if (eqcalib.Deserialize(reader) is EqCalibData data)
                        {
                            OutputEq = data;
                        }
                    }
                    reader.ReadEndElement();
                }

                //if (reader.Name == "EqTableFiles")
                //{
                //    reader.ReadStartElement("EqTableFiles");
                //    EqTableFile eq;
                //    do
                //    {
                //        var name = reader.GetAttribute("name");
                //        if (name == null) break;
                //        reader.ReadStartElement("Eq");
                //        eq = eqfiles.Deserialize(reader) as EqTableFile;
                //        EqTableFiles.Add(name, eq);
                //        reader.ReadEndElement();
                //        //reader.Read();
                //    }
                //    while (true);
                //    reader.ReadEndElement();
                //}

                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (AcousticInput != null)
            {
                var acousticinput = new XmlSerializer(typeof(Calib_AcousticInput));
                acousticinput.Serialize(writer, AcousticInput);
            }

            if (AcousticOutput != null)
            {
                var acousticoutput = new XmlSerializer(typeof(Calib_AcousticOutput));
                acousticoutput.Serialize(writer, AcousticOutput);
            }

            if (AnalogInput != null)
            {
                var analoginput = new XmlSerializer(typeof(Calib_AnalogInput));
                analoginput.Serialize(writer, AnalogInput);
            }
            if (AnalogOutput != null)
            {
                var analogoutput = new XmlSerializer(typeof(Calib_AnalogOutput));
                analogoutput.Serialize(writer, AnalogOutput);
            }

            var eqcalib = new XmlSerializer(typeof(EqCalibData));
            writer.WriteStartElement("InputEq");
            eqcalib.Serialize(writer, InputEq);
            writer.WriteEndElement();

            writer.WriteStartElement("OutputEq");
            eqcalib.Serialize(writer, OutputEq);
            writer.WriteEndElement();

            if (EqTableFiles != null)
            {
                var eqfiles = new XmlSerializer(typeof(EqTableFile[]));
                eqfiles.Serialize(writer, EqTableFiles);
            }

            if (LoudspeakerProductionTests != null)
            {
                var xml = new XmlSerializer(typeof(Calib_LoudspeakerProductionTest[]));
                xml.Serialize(writer, LoudspeakerProductionTests);
            }

            if (ImpedanceThieleSmalls != null)
            {
                var xml = new XmlSerializer(typeof(Calib_ImpedanceThieleSmall[]));
                xml.Serialize(writer, ImpedanceThieleSmalls);
            }
        }
    }

    public class EqTableFile
    {
        [XmlAttribute]
        public string StepName { get; set; }
        [XmlAttribute]
        public EQType EqType { get; set; }
        [XmlAttribute]
        public string EqFile { get; set; }
    }
}
