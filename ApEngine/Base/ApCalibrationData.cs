using ApEngine.UIs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml;
using System.Xml.Serialization;
using TestCore.Base;

using ToucanCore.Abstraction.Engine;

namespace ApEngine.Base
{
    /// <summary>
    /// This Calibration is for hardware, no matter what the script is, which should stored into Physical Station Instance
    /// </summary>
    public class ApCalibrationData : IXmlSerializable, IExpirableData
    {
        public const string FileExt = ".pcd";
        public string Version { get; private set; } = "0.1";
        public const string Target = "Apx";

        public string EqType { get; set; }
        public string EqSerialNumber { get; set; }

        [XmlAttribute]
        public DateTime UpdateTime { get; set; }
        [XmlAttribute]
        public TimeSpan ValidTime { get; set; }
        [XmlAttribute]
        public TimeSpan WarnTime { get; set; }

        [XmlIgnore]
        public string RelevantDir { get; set; }

        [XmlArray]
        public IEnumerable<Calib_AcousticInput> AcousticInputs { get; set; } = new List<Calib_AcousticInput>();
        [XmlArray]
        public IEnumerable<Calib_AcousticOutput> AcousticOutputs { get; set; } = new List<Calib_AcousticOutput>();
        [XmlArray]
        public IEnumerable<Calib_AnalogInput> AnalogInputs { get; set; } = new List<Calib_AnalogInput>();
        [XmlArray]
        public IEnumerable<Calib_AnalogOutput> AnalogOutputs { get; set; } = new List<Calib_AnalogOutput>();

        [XmlArray]
        public IEnumerable<EqCalibData> InputEqDatas { get; set; } = new List<EqCalibData>();
        [XmlArray]
        public IEnumerable<EqCalibData> OutputEqDatas { get; set; } = new List<EqCalibData>();

        [XmlArray]
        public IEnumerable<Calib_ImpedanceThieleSmall> ImpedanceThieleSmalls { get; set; } = new List<Calib_ImpedanceThieleSmall>();

        [XmlArray]
        public IEnumerable<Calib_LoudspeakerProductionTest> LoudspeakerProductionTests { get; set; } = new List<Calib_LoudspeakerProductionTest>();

        public int Save(string path)
        {
            File.WriteAllText(path, XmlSerializerHelper.Serialize(this));

            return 1;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool isEmpty = reader.IsEmptyElement;

            if (isEmpty) return;

            Version = reader.GetAttribute("version");
            EqType = reader.GetAttribute("eqtype");
            EqSerialNumber = reader.GetAttribute("eqsn");

            if (TimeSpan.TryParse(reader.GetAttribute("validtime"), out TimeSpan outvalid))
            {
                ValidTime = outvalid;
            }

            if (TimeSpan.TryParse(reader.GetAttribute("warntime"), out TimeSpan outwarn))
            {
                WarnTime = outwarn;
            }

            reader.Read();

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var acousticin = new XmlSerializer(typeof(Calib_AcousticInput[]));
                reader.ReadStartElement(nameof(AcousticInputs));
                AcousticInputs = acousticin.Deserialize(reader) as IEnumerable<Calib_AcousticInput>;
                reader.ReadEndElement();

                var acousticout = new XmlSerializer(typeof(Calib_AcousticOutput[]));
                reader.ReadStartElement(nameof(AcousticOutputs));
                AcousticOutputs = acousticout.Deserialize(reader) as IEnumerable<Calib_AcousticOutput>;
                reader.ReadEndElement();

                var analogin = new XmlSerializer(typeof(Calib_AnalogInput[]));
                reader.ReadStartElement(nameof(AnalogInputs));
                AnalogInputs = analogin.Deserialize(reader) as IEnumerable<Calib_AnalogInput>;
                reader.ReadEndElement();

                var analogout = new XmlSerializer(typeof(Calib_AnalogOutput[]));
                reader.ReadStartElement(nameof(AnalogOutputs));
                AnalogOutputs = analogout.Deserialize(reader) as IEnumerable<Calib_AnalogOutput>;
                reader.ReadEndElement();

                var eq = new XmlSerializer(typeof(EqCalibData[]));
                reader.ReadStartElement("InputEqDatas");
                InputEqDatas = eq.Deserialize(reader) as IEnumerable<EqCalibData>;
                reader.ReadEndElement();

                reader.ReadStartElement("OutputEqDatas");
                OutputEqDatas = eq.Deserialize(reader) as IEnumerable<EqCalibData>;
                reader.ReadEndElement();

                var impedance = new XmlSerializer(typeof(Calib_ImpedanceThieleSmall[]));
                reader.ReadStartElement(nameof(ImpedanceThieleSmalls));
                ImpedanceThieleSmalls = impedance.Deserialize(reader) as IEnumerable<Calib_ImpedanceThieleSmall>;
                reader.ReadEndElement();

                var loudspeaker = new XmlSerializer(typeof(Calib_LoudspeakerProductionTest[]));
                reader.ReadStartElement(nameof(LoudspeakerProductionTests));
                LoudspeakerProductionTests = loudspeaker.Deserialize(reader) as IEnumerable<Calib_LoudspeakerProductionTest>;
                reader.ReadEndElement();

                reader.MoveToContent();
            }

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("version", Version);
            writer.WriteAttributeString("target", Target);
            writer.WriteAttributeString("eqtype", EqType);
            writer.WriteAttributeString("eqsn", EqSerialNumber);
            writer.WriteAttributeString("validtime", ValidTime.ToString());
            writer.WriteAttributeString("warntime", WarnTime.ToString());

            var acousticinput = new XmlSerializer(AcousticInputs.GetType());
            writer.WriteStartElement(nameof(AcousticInputs));
            acousticinput.Serialize(writer, AcousticInputs);
            writer.WriteEndElement();

            var acousticoutput = new XmlSerializer(AcousticOutputs.GetType());
            writer.WriteStartElement(nameof(AcousticOutputs));
            acousticoutput.Serialize(writer, AcousticOutputs);
            writer.WriteEndElement();

            var analoginput = new XmlSerializer(AnalogInputs.GetType());
            writer.WriteStartElement(nameof(AnalogInputs));
            analoginput.Serialize(writer, AnalogInputs);
            writer.WriteEndElement();

            var analogoutput = new XmlSerializer(AnalogOutputs.GetType());
            writer.WriteStartElement(nameof(AnalogOutputs));
            analogoutput.Serialize(writer, AnalogOutputs);
            writer.WriteEndElement();

            var eq = new XmlSerializer(InputEqDatas.GetType());

            writer.WriteStartElement("InputEqDatas");
            eq.Serialize(writer, InputEqDatas);
            writer.WriteEndElement();
            writer.WriteStartElement("OutputEqDatas");
            eq.Serialize(writer, OutputEqDatas);
            writer.WriteEndElement();

            var impedance = new XmlSerializer(ImpedanceThieleSmalls.GetType());
            writer.WriteStartElement(nameof(ImpedanceThieleSmalls));
            impedance.Serialize(writer, ImpedanceThieleSmalls);
            writer.WriteEndElement();

            var loudspeaker = new XmlSerializer(LoudspeakerProductionTests.GetType());
            writer.WriteStartElement(nameof(LoudspeakerProductionTests));
            loudspeaker.Serialize(writer, LoudspeakerProductionTests);
            writer.WriteEndElement();
        }

        public static ApCalibrationData Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ApCalibrationData));

            FileInfo fi = new FileInfo(path);

            using (StreamReader sr = new StreamReader(path))
            {
                var obj = serializer.Deserialize(sr) as ApCalibrationData;
                obj.RelevantDir = Path.Combine(Directory.GetParent(path).FullName, Path.GetFileNameWithoutExtension(path));  //For default. the data should be under calibrationbase
                obj.UpdateTime = fi.LastWriteTime;

                return obj;
            }
        }
    }
}
