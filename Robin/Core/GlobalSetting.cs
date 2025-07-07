using ApEngine;
using ApEngine.Base;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore.Base;

namespace Robin.Core
{
    public class GlobalGroupSetting : IXmlSerializable
    {
        public const string XMLTAG = "GlobalSetting";
        public Dictionary<GlobalDefinitionGroupName, IGroupItem> GlobalDefinitionGroups { get; } = new Dictionary<GlobalDefinitionGroupName, IGroupItem>();

        public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>();

        //public string APxLocation { get; set; }

        public ApCalibrationData HardwareCalibrationData { get; } = new ApCalibrationData();

        public GlobalGroupSetting()
        {
        }

        public static GlobalGroupSetting GetDefault()
        {
            GlobalGroupSetting obj = new GlobalGroupSetting();

            obj.GlobalDefinitionGroups.Add(GlobalDefinitionGroupName.dBrG, new GroupItem<double>() { Note="mVrms"});
            obj.GlobalDefinitionGroups.Add(GlobalDefinitionGroupName.SenseR, new GroupItem<double>() { Note = "Ohm"});
            obj.GlobalDefinitionGroups.Add(GlobalDefinitionGroupName.Mic_Cal, new GroupItem<string>() { Note="File Path" });
            obj.GlobalDefinitionGroups.Add(GlobalDefinitionGroupName.Output_EQ, new GroupItem<string>() { Note="File Path"});
            obj.GlobalDefinitionGroups.Add(GlobalDefinitionGroupName.Export_Data_Specification, new GroupItem<string>() { Note="File Path"});

            obj.Variables.Add("DataFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TYMPTE","Robin"));

            return obj;
        }

        public int Save(string path)
        {
            //if (APxLocation != null)
            //{
            //    var currasm = Assembly.GetAssembly(typeof(AudioPrecision.API.AcqLengthType));
            //    var currver = currasm.GetName().Version.ToString();

            //    var apipath = Path.Combine(Path.GetDirectoryName(APxLocation), "AudioPrecision.API.dll");

            //    FileVersionInfo fi = FileVersionInfo.GetVersionInfo(apipath);

            //    if (fi.ProductVersion != currver)
            //    {
            //        if (MessageBox.Show($"You are SWITCH Audio Precision from {currver} to {fi.ProductVersion}, Are you sure", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            //        {
            //            File.Copy(apipath, Path.Combine(AppContext.BaseDirectory, "AudioPrecision.API.dll"), true);

            //            //File.Copy(apipath, currasm.Location, true);  // the asm is loaded in MSIL
            //        }
            //        else
            //        {
            //            return -1;
            //        }
            //    }
            //}

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using(StreamWriter sw = new StreamWriter(path))
            {
                XmlSerializer xml = new XmlSerializer(GetType());
                xml.Serialize(sw, this);

                sw.Close();
            }

            return 1;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            //APxLocation = reader.GetAttribute("apx");

            reader.Read();

            if(reader.Name == nameof(Variables))
            {
                reader.ReadStartElement(nameof(Variables));
                Variables.Clear();
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    var key = reader.GetAttribute("key");
                    var value = reader.GetAttribute("value");

                    Variables.Add(key, value);
                    reader.Read();
                }

                reader.ReadEndElement();
            }

            reader.ReadStartElement(nameof(GlobalDefinitionGroups));

            while(reader.NodeType != XmlNodeType.EndElement)
            {
                var key = reader.GetAttribute("key");
                var type = reader.GetAttribute("type");
                reader.ReadStartElement("Group");
                
                XmlSerializer xml = new XmlSerializer(Type.GetType(type));
                var val = xml.Deserialize(reader) as IGroupItem;

                reader.ReadEndElement();

                if(Enum.TryParse(key, true, out GlobalDefinitionGroupName name))
                {
                    GlobalDefinitionGroups.Add(name, val);
                }
                else
                {
                    
                }
            }

            reader.ReadEndElement();
            reader.ReadEndElement();
            reader.MoveToContent();
        }

        public void WriteXml(XmlWriter writer)
        {
            //if(APxLocation != null)
            //{
            //    writer.WriteAttributeString("apx", APxLocation);
            //}

            writer.WriteStartElement(nameof(Variables));

            foreach(var varitem in Variables)
            {
                writer.WriteStartElement("Item");
                writer.WriteAttributeString("key", varitem.Key);
                writer.WriteAttributeString("value", varitem.Value);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement(nameof(GlobalDefinitionGroups));

            foreach(var group in GlobalDefinitionGroups)
            {
                var type = group.Value.GetType();
                writer.WriteStartElement("Group");
                writer.WriteAttributeString("key", group.Key.ToString());
                writer.WriteAttributeString("type", type.FullName);

                XmlSerializer xml = new XmlSerializer(type);
                xml.Serialize(writer, group.Value);

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        public static GlobalGroupSetting FromFile(string path)
        {
            if(File.Exists(path))
            {
                XmlSerializer xml = new XmlSerializer(typeof(GlobalGroupSetting));
                
                using(StreamReader sr = new StreamReader(path))
                {
                    return xml.Deserialize(sr) as GlobalGroupSetting;
                }
            }
            

            return new GlobalGroupSetting();
        }
    }

    public enum GlobalDefinitionGroupName
    {
        dBrG,
        SenseR,
        Mic_Cal,
        Output_EQ,
        Export_Data_Specification,
    }
}
