using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.HAL
{
    public enum TriggerStartTestType
    {
        None,
        Fixture,
        Keyboard,
        ApxAuxIn,
        External,
    }

    public interface IStartTrigger
    {
        string Name { get; }
        TriggerStartTestType TriggerType { get; }
        string[] TriggerSources { get; }

        string Source { get; set; }

        void Initialize();
        void Clear();

        /// <summary>
        /// the callback when trigger signal happened
        /// </summary>
        EventHandler<DutMessage> StartTrigged { get; set; } // event will enable multiple trigger
        //int StartWaitingOnceTrigger();
        //void StartTriggedRecieve(object sender, TestCore.HAL.DutMessage e);
    }

}
