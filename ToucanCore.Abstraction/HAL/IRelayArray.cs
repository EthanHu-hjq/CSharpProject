using System;
using System.Collections.Generic;
using System.Text;

namespace ToucanCore.Abstraction.HAL
{
    public interface IRelayArray : IHardware
    {
        int ChannelCount { get; }
        int SetRelay(bool state, params int[] channels);
        int SetRelay(int channleindex, bool state);
        int SetRelay(int value, int mask = 0xffff);
        int Reset();

        int OnValue_Stored { get; }
        int OffValue_Stored { get; }
    }
}
