using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ToucanCore.Engine
{
    public interface IExpirableData
    {
        /// <summary>
        /// Time of Update Data
        /// </summary>
        DateTime UpdateTime { get; }
        
        /// <summary>
        /// Valid Time Span before Data Expired
        /// </summary>
        TimeSpan ValidTime { get; }

        /// <summary>
        /// Warn Time span before Data Expired
        /// </summary>
        TimeSpan WarnTime { get; }

        /// <summary>
        /// the Dir which the Data point to
        /// </summary>
        string RelevantDir { get; }
    }
}
