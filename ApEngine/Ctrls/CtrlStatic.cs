using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApEngine.Ctrls
{
    public static class CtrlStatic
    {
        public static Array InputConnectorTypes = Enum.GetValues(typeof(AudioPrecision.API.InputConnectorType));
        public static Array OutputConnectorTypes = Enum.GetValues(typeof(AudioPrecision.API.OutputConnectorType));
        public static Array EqTypes = Enum.GetValues(typeof(AudioPrecision.API.EQType));
        public static string[] UnitList { get; } = ApxEngine.ApRef.SignalPathSetup.Level.Axis.UnitList;

        public static Array LoudspeakerTestConfigurations = Enum.GetValues(typeof(AudioPrecision.API.LoudspeakerTestConfiguration));
        public static Array ImpedanceConfigurations = Enum.GetValues(typeof(AudioPrecision.API.ImpedanceConfiguration));
        public static Array InputChannels = Enum.GetValues(typeof(AudioPrecision.API.InputChannelIndex));
        public static Array ModelFits = Enum.GetValues(typeof(AudioPrecision.API.ThieleSmallModelFit));
    }
}
