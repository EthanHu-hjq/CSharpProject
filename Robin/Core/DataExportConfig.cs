using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;

namespace Robin.Core
{
    public class DataExportConfig : System.Xml.Serialization.IXmlSerializable
    {
        [XmlIgnore]
        AudioPrecision.API.ISignalPath sp { get; set; }
        [XmlIgnore]
        AudioPrecision.API.IMeasurement Step { get; set; }
        public string SignalPath { get; set; }
        public string Measurement { get; set; }
        public string Result { get; set; }

        public string ExportDataSpec_SpecSheet { get; set; }
        public string ExportDataSpec_Database { get; set; }
        public string ExportDataSpec_Vacs { get; set; }

        public List<string> Channels_SpecSheet { get; set; } = new List<string>();
        public List<string> Channels_Database { get; set; } = new List<string>();
        public List<string> Channels_Vacs { get; set; } = new List<string>();

        public bool ExportAsVacsCartesian { get; set; }
        public bool ExportAsVacsContour { get; set; }

        public void Apply()
        {

        }

        public void Execute()
        {
            ISequenceMeasurement stepmeas = null;

            stepmeas.Show();
            Step = ApEngine.ApxEngine.ApRef.ActiveMeasurement;

            var setting = Step.CreateExportSettings();
            setting.DataType = SourceDataType.Measured;
            setting.SetChannelEnabled(1, true);
            setting.AppendIfExists = false;
            setting.SetResultEnabled("", true);

            Step.ExportData("", AudioPrecision.API.NumberOfGraphPoints.GraphPointsSameAsGraph, false);

            foreach (ISequenceResult rs in stepmeas.SequenceResults)
            {
                rs.ExportData("");
            }

            foreach (ISequenceResultGraph rg in stepmeas.ResultGraphs)
            {
            }

            var exportResult = Step.SequenceMeasurement.SequenceSteps.ExportResultDataSteps.Add();
            exportResult.SheetPerChannel = true;
            exportResult.ExportSpecification = "";
            exportResult.DataType = SourceDataType.Measured;
            exportResult.SetChannelEnabled(1, true);
            exportResult.FileName = "$(ProjectDir)\\$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(SequenceName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_Database.xlsx";
            exportResult.FileName = "$(ProjectDir)\\$(DataFolder)\\$(SUT_Model)\\$(SUT_Model_Option)\\$(SequenceName)\\$(SUT_Model)_$(SUT_Model_Option)_$(SUT_ID)_$(Date)_Specsheet.xlsx";
            foreach (ISequenceStep st in Step.SequenceMeasurement.SequenceSteps)
            {

            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            SignalPath = reader.GetAttribute(nameof(SignalPath));
            Measurement = reader.GetAttribute(nameof(Measurement));
            Result = reader.GetAttribute(nameof(Result));

            var isempty = reader.IsEmptyElement;
            reader.ReadStartElement();

            if (!isempty)
            {
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    var type = reader.GetAttribute("type");
                    var spec = reader.GetAttribute("spec");
                    var chstr = reader.GetAttribute("channels");

                    string[] channels = new string[] { };
                    if (!string.IsNullOrEmpty(chstr))
                    {
                        channels = chstr.Split(',');
                    }

                    switch (type)
                    {
                        case "SpecSheet":
                            ExportDataSpec_SpecSheet = spec;
                            Channels_SpecSheet.Clear();
                            Channels_SpecSheet.AddRange(channels);
                            break;
                        case "Database":
                            ExportDataSpec_Database = spec;
                            Channels_Database.Clear();
                            Channels_Database.AddRange(channels);
                            break;
                        case "Vacs":
                            ExportDataSpec_Vacs = spec;
                            Channels_Vacs.Clear();
                            Channels_Vacs.AddRange(channels);
                            var cartesion = reader.GetAttribute("isCartesian");
                            var contour = reader.GetAttribute("isContour");
                            ExportAsVacsCartesian = bool.Parse(cartesion);
                            ExportAsVacsContour = bool.Parse(contour);
                            break;
                    }

                    reader.ReadStartElement("Item");
                }
                reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(SignalPath), SignalPath);
            writer.WriteAttributeString(nameof(Measurement), Measurement);
            writer.WriteAttributeString(nameof(Result), Result);

            if (string.IsNullOrEmpty(ExportDataSpec_SpecSheet) || Channels_SpecSheet?.Count == 0)
            { }
            else
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("type", "SpecSheet");
                writer.WriteAttributeString("spec", ExportDataSpec_SpecSheet);
                Channels_SpecSheet.Sort();
                writer.WriteAttributeString("channels", string.Join(",", Channels_SpecSheet));
                writer.WriteEndElement();
            }

            if (string.IsNullOrEmpty(ExportDataSpec_Database) || Channels_Database?.Count == 0)
            { }
            else
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("type", "Database");
                writer.WriteAttributeString("spec", ExportDataSpec_Database);
                Channels_Database.Sort();
                writer.WriteAttributeString("channels", string.Join(",", Channels_Database));
                writer.WriteEndElement();
            }

            if (string.IsNullOrEmpty(ExportDataSpec_Vacs) || Channels_Vacs?.Count == 0)
            { }
            else
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("type", "Vacs");
                writer.WriteAttributeString("spec", ExportDataSpec_Vacs);
                Channels_Vacs.Sort();
                writer.WriteAttributeString("channels", string.Join(",", Channels_Vacs));
                writer.WriteAttributeString("isCartesian", ExportAsVacsCartesian.ToString());
                writer.WriteAttributeString("isContour", ExportAsVacsContour.ToString());
                writer.WriteEndElement();
            }
        }
    }

}
