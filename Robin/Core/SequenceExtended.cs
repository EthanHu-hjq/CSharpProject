using ApEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using TestCore.Data;
using TestCore;
using System.Xml.Linq;
using TestCore.Base;

namespace Robin.Core
{
    [Serializable]
    public class SequenceExtended : TF_Base, System.Xml.Serialization.IXmlSerializable
    {
        public string AuxOut { get; set; }
        public string dBrG { get; set; }
        public string MicCal { get; set; }
        public string SenseR { get; set; }
        public string OutputEq { get; set; }

        public string Name { get; set; }
        [XmlIgnore]
        public Sequence Sequence { get; set; }

        [XmlIgnore]
        public List<DataExportConfig> DataExportConfigs { get; } = new List<DataExportConfig>();

        public TF_Spec Spec { get; set; }

        public bool EnableReport { get; set; } = true;
        public bool EnableDataOutput { get; set; }

        public SequenceExtended()
        { }

        public SequenceExtended(Sequence seq)
        {
            Sequence = seq;
            Name = seq.Name;

            if (seq.Spec is null)
            {
                seq.Analyze();
            }

            Spec = seq.Spec;

            foreach (var signalpath in seq.Spec.Limit)
            {
                foreach (var meas in signalpath)
                {
                    foreach (var rs in meas)
                    {
                        DataExportConfig dec = new DataExportConfig();
                        dec.SignalPath = signalpath.Element.Name;
                        dec.Measurement = meas.Element.Name;
                        dec.Result = rs.Element.Name;

                        DataExportConfigs.Add(dec);
                    }
                }
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Name = reader.GetAttribute("name");
            AuxOut = reader.GetAttribute(nameof(AuxOut).ToLower());
            dBrG = reader.GetAttribute(nameof(dBrG).ToLower());
            MicCal = reader.GetAttribute(nameof(MicCal).ToLower());
            SenseR = reader.GetAttribute(nameof(SenseR).ToLower());
            OutputEq = reader.GetAttribute(nameof(OutputEq).ToLower());

            if(reader.GetAttribute(nameof(EnableReport).ToLower()) is string str0)
            {
                EnableReport = TRUE_STRING.Contains(str0);
            }

            if (reader.GetAttribute(nameof(EnableDataOutput).ToLower()) is string str1)
            {
                EnableDataOutput = TRUE_STRING.Contains(str1);
            }

            reader.Read();
            var str = reader.ReadElementContentAsString();

            if(!string.IsNullOrEmpty(str))
            {
                XDocument doc = XDocument.Parse(str);
                var spec = new TF_Spec();
                Spec = spec.XmlDeserialize(doc.Root) as TF_Spec;

                //foreach (var signalpath in Spec.Limit)
                //{
                //    foreach (var meas in signalpath)
                //    {
                //        foreach (var rs in meas)
                //        {
                //            DataExportConfig dec = new DataExportConfig();
                //            dec.SignalPath = signalpath.Element.Name;
                //            dec.Measurement = meas.Element.Name;
                //            dec.Result = rs.Element.Name;

                //            DataExportConfigs.Add(dec);
                //        }
                //    }
                //}
            }

            reader.ReadStartElement("DataExports");

            XmlSerializer serializer = new XmlSerializer(typeof(DataExportConfig));
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                DataExportConfig dec = serializer.Deserialize(reader) as DataExportConfig;

                DataExportConfigs.Add(dec);
            }

            reader.ReadEndElement(); // end of dataexports

            reader.ReadEndElement(); // end of sequenceextended
            return;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("name", Name);
            if (!string.IsNullOrEmpty(AuxOut))
            {
                //AuxOut = AuxOut.Split(':')[0];
                writer.WriteAttributeString(nameof(AuxOut).ToLower(), AuxOut);
            }
            if (!string.IsNullOrEmpty(dBrG))
            {
                //dBrG = dBrG.Split(':')[0];
                writer.WriteAttributeString(nameof(dBrG).ToLower(), dBrG);
            }
            if (!string.IsNullOrEmpty(MicCal))
            {
                //MicCal = MicCal.Split(':')[0];
                writer.WriteAttributeString(nameof(MicCal).ToLower(), MicCal);
            }
            if (!string.IsNullOrEmpty(SenseR))
            {
                //SenseR = SenseR.Split(':')[0];
                writer.WriteAttributeString(nameof(SenseR).ToLower(), SenseR);
            }
            if (!string.IsNullOrEmpty(OutputEq))
            {
                //OutputEq = OutputEq.Split(':')[0];
                writer.WriteAttributeString(nameof(OutputEq).ToLower(), OutputEq);
            }
            if (!EnableReport)
            {
                writer.WriteAttributeString(nameof(EnableReport).ToLower(), EnableReport.ToString());
            }
            if (EnableDataOutput)
            {
                writer.WriteAttributeString(nameof(EnableDataOutput).ToLower(), EnableDataOutput.ToString());
            }

            writer.WriteElementString("Spec", Spec.XmlSerialize().ToString());

            writer.WriteStartElement("DataExports");
            XmlSerializer serializer = new XmlSerializer(typeof(DataExportConfig));
            foreach (var de in DataExportConfigs)
            {
                if (de.ExportDataSpec_Database == "N/A" || de.ExportDataSpec_SpecSheet == "N/A" || de.ExportDataSpec_Vacs == "N/A") continue;
                serializer.Serialize(writer, de);
            }
            writer.WriteEndElement();
        }
    }

    public class LimitHelper : TF_Limit
    {
        TF_Limit Limit { get; set; }

    }
}
