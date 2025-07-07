using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 619

namespace ApEngine.Base
{
    public class Calib_LoudspeakerProductionTest
    {
        //public static string[] AvailableConfigs = new string[] { "External (1 Ch)", "External (2 Ch)" };
        //public static string[] AvailableChannels = new string[] { "Ch1", "Ch2", "Ch3", "Ch4", "Ch5", "Ch6", "Ch7", "Ch8" };

        /// <summary>
        /// For Identification in Script
        /// </summary>
        public string StepName { get; set; }

        public AudioPrecision.API.LoudspeakerTestConfiguration TestConfiguration { get; set; } //= "External (1 Ch)";
        
        /// <summary>
        /// Calibration Data
        /// </summary>
        public string CorrectionCurve { get; set; }

        /// <summary>
        /// Calibration Data
        /// </summary>
        public double SenseR { get; set; }// = 100;
        /// <summary>
        /// Calibration Data. Value with Unit
        /// </summary>
        public string AmplifierGain { get; set; }
        public AudioPrecision.API.ThieleSmallModelFit ModelFit { get; set; }
        
        

        public AudioPrecision.API.InputChannelIndex Channel { get; set; }
        //public AudioPrecision.API.InputChannelIndex ExternalSenseResistorChannel { get; set; }

        public Calib_LoudspeakerProductionTest() { }

        public static Calib_LoudspeakerProductionTest GetFromHardware()
        {
            try
            {
                Calib_LoudspeakerProductionTest rtn = new Calib_LoudspeakerProductionTest()
                {
                    //Channel = ApxEngine.ApRef.LoudspeakerProductionTest.PrimaryChannel,
                    //ExternalSenseResistorChannel = ApxEngine.ApRef.LoudspeakerProductionTest.ExternalSenseResistorChannel,
                    SenseR = ApxEngine.ApRef.LoudspeakerProductionTest.ExternalSenseResistance,
                    ModelFit = ApxEngine.ApRef.LoudspeakerProductionTest.ModelFit,
                    AmplifierGain = ApxEngine.ApRef.LoudspeakerProductionTest.AmplifierGain.Text,
                };

#if AP8
                if (ApxEngine.ApRef.LoudspeakerProductionTest.Measure == LoudspeakerTestMeasurementType.VdrvrAndVsense)
                {
                    rtn.TestConfiguration = LoudspeakerTestConfiguration.External2Ch;
                }
                else if (ApxEngine.ApRef.LoudspeakerProductionTest.Measure == LoudspeakerTestMeasurementType.VsenseOnly)
                {
                    rtn.TestConfiguration = LoudspeakerTestConfiguration.External1Ch;
                }
#else
                rtn.TestConfiguration = ApxEngine.ApRef.LoudspeakerProductionTest.TestConfiguration;
#endif

                //if (rtn.TestConfiguration == AudioPrecision.API.LoudspeakerTestConfiguration.External1Ch)
                //{
                //    rtn.Channel = ApxEngine.ApRef.LoudspeakerProductionTest.ExternalSenseResistorChannel;
                //}
                //else
                //{
                //    rtn.Channel = ApxEngine.ApRef.LoudspeakerProductionTest.PrimaryChannel;
                //}

                if (rtn.TestConfiguration == AudioPrecision.API.LoudspeakerTestConfiguration.External1Ch)
                {
#if AP8
                    rtn.Channel = ApxEngine.ApRef.LoudspeakerProductionTest.VdrvrChannel;  // Not Verified
#else
                    rtn.Channel = ApxEngine.ApRef.LoudspeakerProductionTest.ExternalSenseResistorChannel;
#endif
                }
                else
                {
#if AP8
                    rtn.Channel = ApxEngine.ApRef.LoudspeakerProductionTest.VsenseChannel;
#else
                    rtn.Channel = ApxEngine.ApRef.LoudspeakerProductionTest.PrimaryChannel;
#endif
                }

                return rtn;
            }
            catch
            {
                Calib_LoudspeakerProductionTest rtn = new Calib_LoudspeakerProductionTest()
                {
                    TestConfiguration = AudioPrecision.API.LoudspeakerTestConfiguration.External2Ch,
                    Channel = AudioPrecision.API.InputChannelIndex.Ch1,
                    //ExternalSenseResistorChannel = ApxEngine.ApRef.LoudspeakerProductionTest.ExternalSenseResistorChannel,
                    SenseR = 0.1,
                    ModelFit = AudioPrecision.API.ThieleSmallModelFit.Standard,
                    AmplifierGain = "0 dB",
                };

                return rtn;
            }
        }
    }
}
