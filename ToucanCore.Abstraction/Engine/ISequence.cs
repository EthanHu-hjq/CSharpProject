using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.Engine
{
    /// <summary>
    /// Test Sequence, different from SequenceCall
    /// </summary>
    public interface ISequence
    {
        /// <summary>
        /// Sequence Name, different from Step Name, for sequence call.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        string Description { get; }
    }
}
