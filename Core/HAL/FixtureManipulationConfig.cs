using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using System.Data;

namespace ToucanCore.HAL
{
    public class FixtureManipulationConfig : IXmlSerializable
    {
        [XmlIgnore]
        public string FilePath { get; private set; }

        /// <summary>
        /// Description -> CMD
        /// </summary>
        public Dictionary<string, string> InCommands { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> OutCommands { get; } = new Dictionary<string, string>();

        public FixtureManipulationConfig()
        { }

        public FixtureManipulationConfig(string[] incmddescs, string[] incmds, string[] outcmddescs, string[] outcmds)
        {
            for (int i = 0; i < incmddescs.Length; i++)
            {
                InCommands.Add(incmddescs[i], incmds[i]);
            }

            for (int i = 0; i < outcmddescs.Length; i++)
            {
                OutCommands.Add(outcmddescs[i], outcmds[i]);
            }
        }

        public static FixtureManipulationConfig Load(string filepath)
        {
            using (StreamReader sr = new StreamReader(filepath))
            {
                var rtn = Serializer.Value.Deserialize(sr) as FixtureManipulationConfig;
                rtn.FilePath = filepath;

                return rtn;
            }
        }

        public void Update(FixtureManipulationConfig config)
        {
            foreach(var item in config.InCommands)
            {
                if(InCommands.ContainsKey(item.Key))
                {
                    InCommands[item.Key] = item.Value;
                }
            }

            foreach (var item in config.OutCommands)
            {
                if (OutCommands.ContainsKey(item.Key))
                {
                    OutCommands[item.Key] = item.Value;
                }
            }
        }

        public int SaveAs(string filepath)
        {
            FilePath = filepath;

            Save();

            return 1;
        }

        static Lazy<XmlSerializer> Serializer = new Lazy<XmlSerializer>(() => { return new XmlSerializer(typeof(FixtureManipulationConfig)); });

        public void Save()
        {
            FileInfo fi = new FileInfo(FilePath);

            if (fi.Exists)
            {
                if (fi.IsReadOnly) fi.IsReadOnly = false;
                fi.Delete();
            }

            using (StreamWriter sw = new StreamWriter(FilePath))
            {
                Serializer.Value.Serialize(sw, this);

                sw.Flush();
                sw.Close();
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.Read();

            reader.ReadStartElement("InCmds");
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var opstr = reader.GetAttribute("name");
                var valstr = reader.GetAttribute("value");

                reader.Read();

                if(InCommands.ContainsKey(opstr))
                {
                    InCommands[opstr]=valstr;
                }
                else
                {
                    InCommands.Add(opstr, valstr);
                }
            }
            reader.ReadEndElement();

            reader.ReadStartElement("OutCmds");
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                var opstr = reader.GetAttribute("name");
                var valstr = reader.GetAttribute("value");

                reader.Read();

                if (OutCommands.ContainsKey(opstr))
                {
                    OutCommands[opstr] = valstr;
                }
                else
                {
                    OutCommands.Add(opstr, valstr);
                }
            }
            reader.ReadEndElement();

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("InCmds");
            foreach (var incmd in InCommands)
            {
                writer.WriteStartElement("Cmd");
                writer.WriteAttributeString("name", incmd.Key.ToString());
                writer.WriteAttributeString("value", incmd.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("OutCmds");
            foreach (var incmd in OutCommands)
            {
                writer.WriteStartElement("Cmd");
                writer.WriteAttributeString("name", incmd.Key.ToString());
                writer.WriteAttributeString("value", incmd.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}
