using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.Engine
{
    public interface IExecutionUISetting
    {
        int SlotRow { get; set; }
        int SlotColumn { get; set; }
        bool ForceFocus { get; set; }
    }
}
