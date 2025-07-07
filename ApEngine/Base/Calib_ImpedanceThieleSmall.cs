using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

#pragma warning disable 0619

namespace ApEngine.Base
{
    public class Calib_ImpedanceThieleSmall
    {
        /// <summary>
        /// For Identification in Script
        /// </summary>
        public string StepName { get; set; }

        public AudioPrecision.API.ImpedanceConfiguration TestConfiguration { get; set; }
        public AudioPrecision.API.InputChannelIndex Channel { get; set; }
        //public AudioPrecision.API.InputChannelIndex ExternalSenseResistorChannel { get; set; }
        public AudioPrecision.API.ThieleSmallModelFit ModelFit { get; set; }
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

        ///// This should be property of DUT
        //public string DriverDiameter { get; set; }

        public static Calib_ImpedanceThieleSmall GetFromHardware()
        {
            try
            {
                Calib_ImpedanceThieleSmall rtn = new Calib_ImpedanceThieleSmall()
                {


                    //Channel = ApxEngine.ApRef.ImpedanceThieleSmall.PrimaryChannel,
                    //ExternalSenseResistorChannel = ApxEngine.ApRef.ImpedanceThieleSmall.ExternalSenseResistorChannel,
                    SenseR = ApxEngine.ApRef.ImpedanceThieleSmall.ExternalSenseResistance,
                    ModelFit = ApxEngine.ApRef.ImpedanceThieleSmall.ModelFit,
                    AmplifierGain = ApxEngine.ApRef.ImpedanceThieleSmall.AmplifierGain.Text,
                };

#if AP8
                if(ApxEngine.ApRef.ImpedanceThieleSmall.Measure == ImpedanceMeasurementType.VdrvrAndVsense)
                {
                    rtn.TestConfiguration = ImpedanceConfiguration.External2Ch;
                }
                else if(ApxEngine.ApRef.ImpedanceThieleSmall.Measure == ImpedanceMeasurementType.VdrvrOnly)
                {
                    rtn.TestConfiguration = ImpedanceConfiguration.External1Ch;
                }
                else if(ApxEngine.ApRef.ImpedanceThieleSmall.Measure == ImpedanceMeasurementType.VsenseOnly)
                {
                    rtn.TestConfiguration = ImpedanceConfiguration.External1Ch;
                }
#else
                rtn.TestConfiguration = ApxEngine.ApRef.ImpedanceThieleSmall.TestConfiguration;
#endif

                //var d = ApxEngine.ApRef.ImpedanceThieleSmall.ExternalSenseResistorChannel;

                if (rtn.TestConfiguration == AudioPrecision.API.ImpedanceConfiguration.External1Ch)
                {
#if AP8
                    rtn.Channel = ApxEngine.ApRef.ImpedanceThieleSmall.VsenseChannel;
#else
                    rtn.Channel = ApxEngine.ApRef.ImpedanceThieleSmall.ExternalSenseResistorChannel;
#endif
                }
                else
                {
#if AP8
                    rtn.Channel = ApxEngine.ApRef.ImpedanceThieleSmall.VdrvrChannel;
#else
                    rtn.Channel = ApxEngine.ApRef.ImpedanceThieleSmall.PrimaryChannel;
#endif
                }

                return rtn;
            }
            catch
            {
                // the ImpedanceThieleSmall might not be active
                Calib_ImpedanceThieleSmall rtn = new Calib_ImpedanceThieleSmall()
                {
                    TestConfiguration = ImpedanceConfiguration.External1Ch,
                    Channel =  InputChannelIndex.Ch1,
                    //ExternalSenseResistorChannel = ApxEngine.ApRef.ImpedanceThieleSmall.ExternalSenseResistorChannel,
                    SenseR = 0.1,
                    ModelFit = ThieleSmallModelFit.Standard,
                    AmplifierGain = "0 dB",
                };
                return rtn;
            }
        }
    }
}
