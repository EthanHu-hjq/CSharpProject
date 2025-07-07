using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore;
using ToucanCore.Engine;
using ToucanCore.Abstraction.Engine;
using TestCore.Configuration;

namespace ToucanCore.Misc
{
    [Serializable]
    public class SlotUiConfig //: System.Xml.Serialization.IXmlSerializable
    {
        [XmlElement]
        public ChartItemConfig[] Pins { get; set; }

        //public XmlSchema GetSchema()
        //{
        //    return null;
        //}

        //public void ReadXml(XmlReader reader)
        //{
        //    reader.ReadStartElement();
        //    reader.ReadStartElement("Pins");

        //    while (reader.NodeType != XmlNodeType.EndElement)
        //    {
        //        Nest<string> item = new Nest<string>();
        //        item.ReadXml(reader);
        //        Pins.Add(item);
        //    }

        //    reader.ReadEndElement();
        //    reader.ReadEndElement();
        //}

        //public void WriteXml(XmlWriter writer)
        //{
        //    writer.WriteStartElement("Pins");
            
        //    foreach (var item in Pins)
        //    {
        //        item.WriteXml(writer);
        //    }

        //    writer.WriteEndElement();
        //}

        public static SlotUiConfig GetStationUiConfig(StationConfig station)
        {
            var filename = $"{station?.CustomerName}_{station?.ProductName}_{station?.StationName}.suc";
            var path = System.IO.Path.Combine(TestCore.Services.ServiceStatic.RootDataDir, filename);

            XmlSerializer xml = new XmlSerializer(typeof(SlotUiConfig));
            if (System.IO.File.Exists(path))
            {
                try
                {
                    using (TextReader tw = new StreamReader(path))
                    {
                        return xml.Deserialize(tw) as SlotUiConfig;
                    }
                }
                catch
                {
                    TF_Base.StaticLog($"Load {filename} Failed");
                }
            }
            return null;
        }
    }

    public class ChartItemConfig
    {
        public string Path { get; set; }
    }

    public static class NestHelper
    {
        public static void WriteXml<T>(this Nest<T> nest, XmlWriter writer)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));

            writer.WriteStartElement("Nest");
            writer.WriteStartElement("Element");
            xml.Serialize(writer, nest.Element);
            writer.WriteEndElement();

            if (nest.Children?.Count > 0)
            {
                writer.WriteStartElement("Children");
                foreach(var item in nest)
                {
                    item.WriteXml(writer);
                }
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public static void ReadXml<T>(this Nest<T> nest, XmlReader reader)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));

            reader.ReadStartElement("Nest");

            reader.ReadStartElement("Element");
            nest.Element = (T)xml.Deserialize(reader);
            reader.ReadEndElement();

            try
            {
                reader.ReadStartElement("Children");

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    Nest<T> sub = new Nest<T>();
                    sub.ReadXml(reader);
                    nest.Add(sub);
                }

                reader.ReadEndElement();
            }
            catch
            { 
            }

            reader.ReadEndElement();
        }

        public static List<T> GetPath<T>(this Nest<T> nest)
        {
            List<T> path = new List<T>();
            var temp = nest;
            do
            {
                path.Add(temp.Element);
                temp = temp.Parent;
            }
            while (temp != null);

            return path;
        }
    }
}
