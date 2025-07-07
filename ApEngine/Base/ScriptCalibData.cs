using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore.Base;
using ToucanCore.Abstraction.Engine;

namespace ApEngine.Base
{
    /// <summary>
    /// this calibration data is for script, which should stored into Logic Station Instance
    /// </summary>
    public class ScriptCalibData : IXmlSerializable, IExpirableData
    {
        public const string FileExt = ".pscd";
        public string Version { get; private set; } = "0.1";
        public const string Target = "Apx";
        public string EqType { get; set; }
        public string EqSerialNumber { get; set; }


        [XmlIgnore]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// The Valid Time and Warn Time should be set in Script, otherwise the script can not summarize the info
        /// </summary>
        [XmlAttribute]
        public TimeSpan ValidTime { get; set; }
        [XmlAttribute]
        public TimeSpan WarnTime { get; set; }

        [XmlIgnore]
        public string FilePath { get; private set; }

        public Dictionary<string, SignalPathCalibData> SignalPathCalibDatas { get; set; } = new Dictionary<string, SignalPathCalibData>();

        public string RelevantDir { get; set; }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            bool isEmpty = reader.IsEmptyElement;

            if (isEmpty) return;

            Version = reader.GetAttribute("version");
            var target = reader.GetAttribute("target");
            if (target != Target)
            {
                throw new InvalidOperationException($"Require Target {Target}, get {target} in calib file");
            }

            EqType = reader.GetAttribute("eqtype");
            EqSerialNumber = reader.GetAttribute("eqsn");

            if (DateTime.TryParse(reader.GetAttribute("updatetime"), out DateTime outupdate))
            {
                UpdateTime = outupdate;
            }

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
                var spcd = new XmlSerializer(typeof(SignalPathCalibData));
                reader.ReadStartElement("SignalPathCalibDatas");
                SignalPathCalibData eq;
                do
                {
                    var name = reader.GetAttribute("name");
                    if (name == null) break;
                    reader.ReadStartElement("SignalPath");
                    eq = spcd.Deserialize(reader) as SignalPathCalibData;
                    SignalPathCalibDatas.Add(name, eq);
                    reader.ReadEndElement();
                    //reader.Read();
                }
                while (true);

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

            writer.WriteAttributeString("updatetime", UpdateTime.ToString());
            writer.WriteAttributeString("validtime", ValidTime.ToString());
            writer.WriteAttributeString("warntime", WarnTime.ToString());

            writer.WriteStartElement("SignalPathCalibDatas");
            var spcd = new XmlSerializer(typeof(SignalPathCalibData));
            foreach (var sp in SignalPathCalibDatas)
            {
                writer.WriteStartElement("SignalPath");
                writer.WriteAttributeString("name", sp.Key);
                spcd.Serialize(writer, sp.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        public static ScriptCalibData Load(string path)
        {
            using (StreamReader sr = new StreamReader(path))
            {
                if (XmlSerializerHelper.Deserialize(sr.ReadToEnd(), typeof(ScriptCalibData)) is ScriptCalibData scd)
                {
                    scd.FilePath = path;
                    return scd;
                }

                return null;
            }
        }

        public void Save(string path)
        {
            foreach (var sp in SignalPathCalibDatas)
            {
                if (sp.Value.EqTableFiles != null)
                {
                    foreach (var item in sp.Value.EqTableFiles)
                    {
                        if (item.EqFile?.Contains(":") == true)
                        {
                            var fname = Path.GetFileName(item.EqFile);
                            File.Copy(item.EqFile, Path.Combine(RelevantDir, fname), true);
                            item.EqFile = fname;
                        }
                    }
                }

                if (sp.Value.InputEq.EqPath?.Contains(":") == true)
                {
                    var fname = Path.GetFileName(sp.Value.InputEq.EqPath);
                    File.Copy(sp.Value.InputEq.EqPath, Path.Combine(RelevantDir, fname), true);
                    sp.Value.InputEq.EqPath = fname;
                }

                if (sp.Value.OutputEq.EqPath?.Contains(":") == true)
                {
                    var fname = Path.GetFileName(sp.Value.OutputEq.EqPath);
                    File.Copy(sp.Value.OutputEq.EqPath, Path.Combine(RelevantDir, fname), true);
                    sp.Value.OutputEq.EqPath = fname;
                }

                if (sp.Value.LoudspeakerProductionTests != null)
                {
                    foreach (var item in sp.Value.LoudspeakerProductionTests)
                    {
                        if (item.CorrectionCurve?.Contains(":") == true)
                        {
                            var fname = Path.GetFileName(item.CorrectionCurve);
                            File.Copy(item.CorrectionCurve, Path.Combine(RelevantDir, fname), true);
                            item.CorrectionCurve = fname;
                        }
                    }
                }

                if (sp.Value.ImpedanceThieleSmalls != null)
                {
                    foreach (var item in sp.Value.ImpedanceThieleSmalls)
                    {
                        if (item.CorrectionCurve?.Contains(":") == true)
                        {
                            var fname = Path.GetFileName(item.CorrectionCurve);
                            File.Copy(item.CorrectionCurve, Path.Combine(RelevantDir, fname), true);
                            item.CorrectionCurve = fname;
                        }
                    }
                }
            }

            UpdateTime = DateTime.Now;

            var xml = XmlSerializerHelper.Serialize(this);

            File.WriteAllText(path, xml);
        }

        /// <summary>
        /// Export the Calibration data and appendix files
        /// </summary>
        /// <param name="path"></param>
        public void Export(string path)
        {
            var targetdir = System.IO.Path.GetDirectoryName(path);
            var targetfiledir = System.IO.Path.Combine(targetdir, System.IO.Path.GetFileNameWithoutExtension(path));

            if (!Directory.Exists(targetfiledir))
            {
                Directory.CreateDirectory(targetfiledir);
            }

            foreach (var sp in SignalPathCalibDatas)
            {
                if (sp.Value.EqTableFiles != null)
                {
                    foreach (var item in sp.Value.EqTableFiles)
                    {
                        if (item.EqFile.Contains(":"))
                        {
                            var fname = Path.GetFileName(item.EqFile);
                            File.Copy(item.EqFile, Path.Combine(targetfiledir, fname), true);
                            item.EqFile = fname;
                        }
                        else if (!string.IsNullOrEmpty(item.EqFile))
                        {
                            File.Copy(Path.Combine(RelevantDir, item.EqFile), Path.Combine(targetfiledir, item.EqFile), true);
                        }
                    }
                }

                if (sp.Value.InputEq.EqPath?.Contains(":") == true)
                {
                    var fname = Path.GetFileName(sp.Value.InputEq.EqPath);
                    File.Copy(sp.Value.InputEq.EqPath, Path.Combine(targetfiledir, fname), true);
                    sp.Value.InputEq.EqPath = fname;
                }
                else if (!string.IsNullOrEmpty(sp.Value.InputEq.EqPath))
                {
                    File.Copy(Path.Combine(RelevantDir, sp.Value.InputEq.EqPath), Path.Combine(targetfiledir, sp.Value.InputEq.EqPath), true);
                }

                if (sp.Value.OutputEq.EqPath?.Contains(":") == true)
                {
                    var fname = Path.GetFileName(sp.Value.OutputEq.EqPath);
                    File.Copy(sp.Value.OutputEq.EqPath, Path.Combine(targetfiledir, fname), true);
                    sp.Value.OutputEq.EqPath = fname;
                }
                else if (!string.IsNullOrEmpty(sp.Value.OutputEq.EqPath))
                {
                    File.Copy(Path.Combine(RelevantDir, sp.Value.OutputEq.EqPath), Path.Combine(targetfiledir, sp.Value.OutputEq.EqPath), true);
                }

                if (sp.Value.LoudspeakerProductionTests != null)
                {
                    foreach (var item in sp.Value.LoudspeakerProductionTests)
                    {
                        if (item.CorrectionCurve?.Contains(":") == true)
                        {
                            var fname = Path.GetFileName(item.CorrectionCurve);
                            File.Copy(item.CorrectionCurve, Path.Combine(targetfiledir, fname), true);
                            item.CorrectionCurve = fname;
                        }
                        else if (!string.IsNullOrEmpty(item.CorrectionCurve))
                        {
                            File.Copy(Path.Combine(RelevantDir, item.CorrectionCurve), Path.Combine(targetfiledir, item.CorrectionCurve), true);
                        }
                    }
                }

                if (sp.Value.ImpedanceThieleSmalls != null)
                {
                    foreach (var item in sp.Value.ImpedanceThieleSmalls)
                    {
                        if (item.CorrectionCurve?.Contains(":") == true)
                        {
                            var fname = Path.GetFileName(item.CorrectionCurve);
                            File.Copy(item.CorrectionCurve, Path.Combine(targetfiledir, fname), true);
                            item.CorrectionCurve = fname;
                        }
                        else if (!string.IsNullOrEmpty(item.CorrectionCurve))
                        {
                            File.Copy(Path.Combine(RelevantDir, item.CorrectionCurve), Path.Combine(targetfiledir, item.CorrectionCurve), true);
                        }
                    }
                }
            }

            var xml = XmlSerializerHelper.Serialize(this);

            File.WriteAllText(path, xml);
        }
    }
}
