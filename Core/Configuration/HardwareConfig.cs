using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TestCore;
using ToucanCore.Driver;
using ToucanCore.HAL;

namespace ToucanCore.Configuration
{
    //public class HardwareConfig : TF_Base, ICloneable
    //{
    //    public const string DefaultFileName = ".default.hws";
    //    public const string XMLTag = "Hardware";

    //    public static IStartTrigger[] StartTriggers { get; } = new IStartTrigger[] { StartTrigger_None.Instance, StartTrigger_Fixture.Instance, StartTrigger_Keyboard.Instance, StartTrigger_Ap.Instance, StartTrigger_Exteranl.Instance };

    //    public IStartTrigger StartTrigger { get; set; }

    //    public IFixture Fixture { get; set; }

    //    public IRelayArray RelayArray { get; set; }

    //    [XmlIgnore]
    //    public string FilePath { get; private set; }

    //    public List<int> SlotMasks { get; set; } = new List<int>();
    //    public List<int> PreUutRoute { get; set; } = new List<int>();
    //    public List<int> UutIdentifiedRoute { get; set; } = new List<int>();
    //    public List<int> PostUutRoute { get; set; } = new List<int>();
    //    public static IFixture[] Fixtures { get; private set; } = new IFixture[] { new Fixture_None() };
    //    public static ISerialNumberReader[] SerialNumberReaders { get; private set; } = new ISerialNumberReader[] { new SerialNumberReader_None(), new RfidReader_LVHM() };
    //    public static IRelayArray[] RelayArrays { get; private set; } = new IRelayArray[] { new RelayArray_None(), new RelayArray_Proxy() };


    //    public ISerialNumberReader SerialNumberReader { get; set; }

    //    /// <summary>
    //    /// For SerialNumberReader. default is false, trig when DUT ready
    //    /// </summary>
    //    public bool TrigOnDutPresent { get; set; }

    //    public Dictionary<string, string> Registers { get; }

    //    public static void LoadHardwareDrivers(Assembly asm)
    //    {
    //        if (asm != null)
    //        {
    //            List<IFixture> fixtures = Fixtures.ToList();
    //            List<IRelayArray> routes = RelayArrays.ToList();

    //            foreach (var t in asm.ExportedTypes)
    //            {
    //                if (t.IsVisible && !t.IsAbstract && !t.IsInterface)
    //                {
    //                    var intfs = t.GetInterfaces();

    //                    foreach (var intf in intfs)
    //                    {
    //                        if (intf == typeof(IFixture))
    //                        {
    //                            var obj = Activator.CreateInstance(t);
    //                            if (!fixtures.Exists(x => x.GetType() == t))
    //                            {
    //                                fixtures.Add(obj as IFixture);
    //                            }

    //                            if(obj is IRelayArray)
    //                            {
    //                                routes.Add((IRelayArray)obj);
    //                                continue;
    //                            }

    //                        }
    //                        else if (intf == typeof(IRelayArray))
    //                        {
    //                            if (!routes.Exists(x => x.GetType() == t))
    //                            {
    //                                routes.Add(Activator.CreateInstance(t) as IRelayArray);
    //                            }
    //                        }
    //                    }
    //                }
    //            }

    //            Fixtures = fixtures.ToArray();
    //            RelayArrays = routes.ToArray();
    //        }
    //    }

    //    public HardwareConfig()
    //    {
    //        StartTrigger = StartTrigger_None.Instance;
    //    }

    //    public HardwareConfig(XElement element) : this()
    //    {
    //        if (element == null) return;
    //        if (element.Name != XMLTag)
    //        {
    //            throw new InvalidOperationException(string.Format("Xml Tag {0} not correct", element.Name));
    //        }

    //        HardwareConfig config = new HardwareConfig();

    //        var e_starttrigger = element.Element(nameof(StartTrigger));

    //        StartTrigger = StartTriggers.FirstOrDefault(x => x.Name == e_starttrigger.Attribute("name").Value);
    //        if (StartTrigger != null)
    //        {
    //            StartTrigger.Source = e_starttrigger.Attribute("source").Value;
    //        }
    //        else
    //        {
    //            StartTrigger = StartTrigger_None.Instance;
    //        }

    //        var e_fixture = element.Element(nameof(Fixture));
    //        if (e_fixture != null)
    //        {
    //            Fixture = Fixtures.FirstOrDefault(x => x.Model == e_fixture.Attribute("model").Value);
    //            if (Fixture != null)
    //            {
    //                Fixture.Resource = e_fixture.Attribute("resource").Value;
    //                Fixture.AutoDutIn = TRUE_STRING.Contains(e_fixture.Attribute("autodutin")?.Value);
    //                Fixture.AutoDutOut = TRUE_STRING.Contains(e_fixture.Attribute("autodutout")?.Value);
    //            }
    //        }

    //        var e_relayarray = element.Element(nameof(RelayArray));
    //        if (e_relayarray != null)
    //        {
    //            RelayArray = RelayArrays.FirstOrDefault(x => x.Model == e_relayarray.Attribute("model").Value);
    //            if (RelayArray != null)
    //            {
    //                RelayArray.Resource = e_relayarray.Attribute("resource").Value;
    //            }

    //            var preuut = e_relayarray.Element(nameof(PreUutRoute))?.Elements("Value");

    //            if (preuut.Count() == 0)
    //            {

    //            }
    //            else
    //            {
    //                foreach (var elem in preuut)
    //                {
    //                    PreUutRoute.Add(int.Parse(elem.Value));
    //                }
    //            }

    //            var uutidentified = e_relayarray.Element(nameof(UutIdentifiedRoute))?.Elements("Value");
    //            foreach (var elem in uutidentified)
    //            {
    //                UutIdentifiedRoute.Add(int.Parse(elem.Value));
    //            }

    //            var postuut = e_relayarray.Element(nameof(PostUutRoute))?.Elements("Value");
    //            foreach (var elem in postuut)
    //            {
    //                PostUutRoute.Add(int.Parse(elem.Value));
    //            }

    //            var mask = e_relayarray.Element(nameof(SlotMasks))?.Elements("Value");
    //            if (mask is null)
    //            {
    //                for (int i = 0; i < PreUutRoute.Count; i++)
    //                {
    //                    SlotMasks.Add(0xFFFF);
    //                }
    //            }
    //            else
    //            {
    //                foreach (var elem in mask)
    //                {
    //                    SlotMasks.Add(int.Parse(elem.Value));
    //                }
    //            }
    //        }

    //        var e_snreader = element.Element(nameof(SerialNumberReader));
    //        if (e_snreader != null)
    //        {
    //            SerialNumberReader = SerialNumberReaders.FirstOrDefault(x => x.Model == e_snreader.Attribute("model").Value);
    //            if (SerialNumberReader != null)
    //            {
    //                SerialNumberReader.Resource = e_snreader.Attribute("resource").Value;

    //                if(TRUE_STRING.Contains(e_snreader.Attribute("trigondutpresent")?.Value))
    //                {
    //                    TrigOnDutPresent = true;
    //                }
    //            }
    //        }
    //    }

    //    public XElement XmlSerialize()
    //    {
    //        XElement element = new XElement(XMLTag);

    //        XElement elem;

    //        elem = new XElement(nameof(StartTrigger));
    //        elem.Add(new XAttribute("name", StartTrigger?.Name ?? "None"));
    //        elem.Add(new XAttribute("source", StartTrigger?.Source ?? ""));
    //        element.Add(elem);

    //        if (Fixture != null)
    //        {
    //            elem = new XElement(nameof(Fixture));
    //            elem.Add(new XAttribute("model", Fixture?.Model));
    //            elem.Add(new XAttribute("resource", Fixture?.Resource ?? ""));
    //            elem.Add(new XAttribute("autodutin", Fixture?.AutoDutIn));
    //            elem.Add(new XAttribute("autodutout", Fixture?.AutoDutOut));
    //            element.Add(elem);
    //        }

    //        if (RelayArray != null)
    //        {
    //            elem = new XElement(nameof(RelayArray));
    //            elem.Add(new XAttribute("model", RelayArray?.Model));
    //            elem.Add(new XAttribute("resource", RelayArray?.Resource ?? ""));

    //            var mask = new XElement(nameof(SlotMasks));
    //            foreach (var val in SlotMasks)
    //            {
    //                mask.Add(new XElement("Value", val));
    //            }
    //            elem.Add(mask);

    //            var pre = new XElement(nameof(PreUutRoute));
    //            foreach (var val in PreUutRoute)
    //            {
    //                pre.Add(new XElement("Value", val));
    //            }
    //            elem.Add(pre);

    //            var uutidentified = new XElement(nameof(UutIdentifiedRoute));
    //            foreach (var val in UutIdentifiedRoute)
    //            {
    //                uutidentified.Add(new XElement("Value", val));
    //            }
    //            elem.Add(uutidentified);

    //            var post = new XElement(nameof(PostUutRoute));
    //            foreach (var val in PostUutRoute)
    //            {
    //                post.Add(new XElement("Value", val));
    //            }
    //            elem.Add(post);

    //            element.Add(elem);
    //        }

    //        if(SerialNumberReader != null)
    //        {
    //            elem = new XElement(nameof(SerialNumberReader));
    //            elem.Add(new XAttribute("model", SerialNumberReader?.Model));
    //            elem.Add(new XAttribute("resource", SerialNumberReader?.Resource ?? ""));
    //            if(TrigOnDutPresent) elem.Add(new XAttribute("trigondutpresent", TrigOnDutPresent));
    //            element.Add(elem);
    //        }

    //        return element;
    //    }

    //    public object XmlDeserialize(XElement element)
    //    {
    //        return LoadFromXml(element);
    //    }

    //    public static HardwareConfig LoadFromXml(XElement element)
    //    {
    //        var config = new HardwareConfig(element);

    //        return config;
    //    }

    //    public object Clone()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static HardwareConfig Load(string path)
    //    {
    //        XDocument doc = XDocument.Load(path);

    //        var hc = LoadFromXml(doc.Element(XMLTag));
    //        hc.FilePath = path;
    //        return hc;
    //    }

    //    public void Save(string path)
    //    {
    //        if (path is null) path = FilePath;
    //        XmlSerialize().Save(path);
    //    }
    //}
}
