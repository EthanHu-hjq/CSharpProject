using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using TestCore;

namespace ToucanCore.Configuration
{
    public class ReferenceConfig : TF_Base, TestCore.IXmlSerializable, ICloneable
    {
        [XmlArray]
        public string[] GoldSamples { get; set; }

        public int RefereceLoop { get; set; }

        /// <summary>
        /// the days that the referenece data is Valid after reference
        /// </summary>
        public int ValidDay { get; set; }

        /// <summary>
        /// the days that prompt warning message before reference expired
        /// </summary>
        public int WarnDay { get; set; }

        public object Clone()
        {
            throw new NotImplementedException();
        }

        public object XmlDeserialize(XElement element)
        {
            throw new NotImplementedException();
        }

        public XElement XmlSerialize()
        {
            throw new NotImplementedException();
        }
    }
}
