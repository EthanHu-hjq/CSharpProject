using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using TestCore.Base;
using ToucanCore.Abstraction.Engine;

namespace ApEngine.Base
{
    [Serializable]
    public class ApReferenceData : IExpirableData
    {
        public const string FileExt = ".prd";
        public string Version { get; private set; } = "0.1";
        public const string Target = "Apx";

        [XmlIgnore]
        public DateTime UpdateTime { get; set; }
        [XmlAttribute]
        public TimeSpan ValidTime { get; set; }
        [XmlAttribute]
        public TimeSpan WarnTime { get; set; }

        public string RelevantDir { get; set; }

        public int LoopTime { get; set; } = 1;

        public List<string> Samples { get; } = new List<string>();

        [XmlIgnore]
        public string CurrentDir { get; set; } = ApxEngine.ReferenceBase;

        public static ApReferenceData FromFile(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ApReferenceData));

            using (StreamReader sr = new StreamReader(path))
            {
                var obj = serializer.Deserialize(sr) as ApReferenceData;
                obj.CurrentDir = Path.GetDirectoryName(path);
                return obj;
            }
        }

        public int Save(string path)
        {
            File.WriteAllText(path, XmlSerializerHelper.Serialize(this));

            return 1;
        }
    }
}
