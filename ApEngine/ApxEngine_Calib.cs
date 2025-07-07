using ApEngine.Base;
using ApEngine.UIs;
using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

#pragma warning disable 0612,0618,0619

namespace ApEngine
{
    public partial class ApxEngine
    {
        public void ExportEquipmentData()
        {
            for (int i = 0; i < ApRef.Sequence.Count; i++)
            {
                var signalpath = ApRef.Sequence[i];

                var signalname = signalpath.Name;
                for (int j = 0; j < signalpath.Count; j++)
                {
                    var step = signalpath[j];

                    var name = step.Name;
                    var type = step.MeasurementType;

                    if (step.Name.StartsWith("//"))
                    {
                        continue;
                    }

                    if (step.MeasurementType == AudioPrecision.API.MeasurementType.SignalPathSetup)
                    {
                        step.Show();
                        var setup = ApRef.SignalPathSetup;

                        var inputeqname = setup.InputEqChannels[0].Eq;
                        setup.InputEqChannels[0].LoadEqFromFile("Slot1_AO_B.csv", false, true);

                        setup.References.AnalogInputReferences.dBSpl1.Value = 0;
                        setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Value = 0;

#if AP8
                        setup.Channels[0].Sensitivity.Value = 0.01;
#else
                        setup.References.AcousticInputReferences.SetSensitivity(1, 0.01);
#endif

                        //var outputeqname = setup.OutputEq.Eq;
                        if (setup.OutputConnector.Type == AudioPrecision.API.OutputConnectorType.AnalogBalanced)
                        {
                        }


                    }
                    else
                    {
                        if (name.StartsWith("REF"))
                        {
                            step.Show();

                            if (step.MeasurementType == AudioPrecision.API.MeasurementType.LoudspeakerProductionTest)
                            {
                                var lsptm = ApRef.LoudspeakerProductionTest;
                                var tempsetup = ApRef.SignalPathSetup;

                                var tempch = tempsetup.InputChannelCount;
#if AP8
                                if (lsptm.Measure == LoudspeakerTestMeasurementType.VsenseOnly)
                                {
                                    //lsptm.LoadAmplifierCorrectionCurveFromFile("", true);  //TODO
                                    var ch = lsptm.VsenseChannel;
                                    var senseR = lsptm.ExternalSenseResistance;
                                    var amplifier = lsptm.AmplifierCorrectionCurve;  //FileName
                                    lsptm.LoadAmplifierCorrectionCurveFromFile("", true);

                                    //lsptm.AmplifierGain.Value = 10.8;
                                    //lsptm.ExternalSenseResistance = 0.1;
                                }
#else
                                if (lsptm.TestConfiguration == AudioPrecision.API.LoudspeakerTestConfiguration.External1Ch)
                                {
                                    //lsptm.LoadAmplifierCorrectionCurveFromFile("", true);  //TODO
                                    var ch = lsptm.ExternalSenseResistorChannel;
                                    var senseR = lsptm.ExternalSenseResistance;
                                    var amplifier = lsptm.AmplifierCorrectionCurve;  //FileName
                                    lsptm.LoadAmplifierCorrectionCurveFromFile("", true);

                                    //lsptm.AmplifierGain.Value = 10.8;
                                    //lsptm.ExternalSenseResistance = 0.1;
                                }
#endif
                            }
                            else if (step.MeasurementType == AudioPrecision.API.MeasurementType.AcousticResponse)
                            {
                                var ar = ApRef.AcousticResponse;
                                var eq = ar.GeneratorWithPilot.EQSettings;
                                var eqstr = eq.ToString();
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Apply the calibration data without match pattern
        /// Only apply the first item of each enumerator
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int ApplyCalibDataWithoutMatch(ApCalibrationData data)
        {
            Info($"Applying Calib Data {data.RelevantDir}");
            try
            {
                for (int i = 0; i < ApRef.Sequence.Count; i++)
                {
                    var signalpath = ApRef.Sequence[i];

                    var signalname = signalpath.Name;
                    for (int j = 0; j < signalpath.Count; j++)
                    {
                        var step = signalpath[j];

                        var name = step.Name;
                        var type = step.MeasurementType;

                        if (step.Name.StartsWith("//"))
                        {
                            continue;
                        }

                        if (step.MeasurementType == MeasurementType.SignalPathSetup)
                        {
                            step.Show();
                            var setup = ApRef.SignalPathSetup;
#if AP8
                            if (setup.Measure == MeasurandType.Acoustic)
                            {
                                int accnt = data.AcousticInputs.Count();
                                if (accnt > 0)
                                {
                                    var channelcnt = setup.InputChannelCount;
                                    if (data.AcousticInputs.FirstOrDefault() is Calib_AcousticInput cal)
                                    {
                                        for (int chidx = 0; chidx < channelcnt; chidx++)
                                        {
                                            //var chname = setup.GetInputChannelName(chidx);
                                            var chname = setup.Channels[chidx].Name;
                                            var ch = setup.InputEqChannels[chidx];

                                            if (cal.Channels.FirstOrDefault(x => chname == x.Channel) is Calib_AcousticInputChannel calibch)
                                            {
                                                //setup.References.AcousticInputReferences.SetSensitivity(chidx, calibch.Sensitivity);
                                                setup.Channels[chidx].Sensitivity.Value = calibch.Sensitivity;
                                            }
                                        }
                                    }
                                }
                            }
#else
                            if (setup.AcousticInput)
                            {
                                int accnt = data.AcousticInputs.Count();
                                if (accnt > 0)
                                {
                                    var channelcnt = setup.InputChannelCount;
                                    if (data.AcousticInputs.FirstOrDefault() is Calib_AcousticInput cal)
                                    {
                                        for (int chidx = 0; chidx < channelcnt; chidx++)
                                        {
                                            var chname = setup.GetInputChannelName(chidx);
                                            var ch = setup.InputEqChannels[chidx];

                                            if (cal.Channels.FirstOrDefault(x => chname == x.Channel) is Calib_AcousticInputChannel calibch)
                                            {
                                                setup.References.AcousticInputReferences.SetSensitivity(chidx, calibch.Sensitivity / Calib_AcousticInputChannel.SensitivityUnitIndex);
                                            }
                                        }
                                    }
                                }
                            }
#endif
                            else
                                    {
                                int accnt = data.AnalogInputs.Count();
                                if (accnt > 0)
                                {
                                    var channelcnt = setup.InputChannelCount;
                                    if (data.AnalogInputs.FirstOrDefault() is Calib_AnalogInput cal)
                                    {
                                        setup.References.AnalogInputReferences.dBSpl1.Text = cal.dBSpl1;
                                        setup.References.AnalogInputReferences.dBSpl2.Text = cal.dBSpl2;
                                        setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text = cal.dBSpl1CalibratorLevel;
                                        setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text = cal.dBSpl2CalibratorLevel;
                                        setup.References.AnalogInputReferences.dBrA.Text = cal.dBrA;
                                        if (!string.IsNullOrEmpty(cal.dBrAOffset)) setup.References.AnalogInputReferences.dBrAOffset.Text = cal.dBrAOffset;
                                        setup.References.AnalogInputReferences.dBrB.Text = cal.dBrB;
                                        if (!string.IsNullOrEmpty(cal.dBrBOffset)) setup.References.AnalogInputReferences.dBrBOffset.Text = cal.dBrBOffset;
                                        if (!string.IsNullOrEmpty(cal.dBm)) setup.References.AnalogInputReferences.dBm.Text = cal.dBm;
                                        if (!string.IsNullOrEmpty(cal.Watts)) setup.References.AnalogInputReferences.Watts.Text = cal.Watts;

                                    }
                                }
                            }

                            if (setup.AcousticOutput)
                            {
                                int accnt = data.AcousticOutputs.Count();
                                if (accnt > 0)
                                {
                                    if (data.AcousticOutputs.FirstOrDefault(x => x.ConnectorType == setup.OutputConnector.Type) is Calib_AcousticOutput cal)
                                    {
                                        if (!double.IsNaN(cal.RefFreq)) setup.References.AcousticOutputReferences.ReferenceFrequency = cal.RefFreq;
                                        if (!double.IsNaN(cal.VoltageRatio)) setup.References.AcousticOutputReferences.VoltageRatio = cal.VoltageRatio;
                                    }
                                }
                            }
                            else
                            {
                                int accnt = data.AnalogOutputs.Count();
                                if (accnt > 0)
                                {
                                    if (data.AnalogOutputs.FirstOrDefault(x => x.ConnectorType == setup.OutputConnector.Type) is Calib_AnalogOutput cal)
                                    {
                                        if (!string.IsNullOrEmpty(cal.dBrG)) setup.References.AnalogOutputReferences.dBrG.Text = cal.dBrG;
                                        if (!string.IsNullOrEmpty(cal.dBm)) setup.References.AnalogOutputReferences.dBm.Text = cal.dBm;
                                        if (!string.IsNullOrEmpty(cal.Watts)) setup.References.AnalogOutputReferences.Watts.Text = cal.Watts;
                                    }
                                }
                            }

                            //int inputeqcnt = data.InputEqDatas.Count();
                            //if (inputeqcnt > 0)
                            //{
                            //    for (int eqidx = 0; eqidx < setup.InputEqChannels.Count; eqidx++)
                            //    {
                            //        var channelcnt = setup.InputChannelCount;
                            //        for (int chidx = 0; chidx < channelcnt; chidx++)
                            //        {
                            //            var inputeqname = setup.InputEqChannels[chidx].Eq;

                            //            if (inputeqname == "None") continue;

                            //            var chname = setup.GetInputChannelName(chidx);

                            //            if (data.InputEqDatas.FirstOrDefault(x => chname == $"Ch{x.Index}") is EqCalibData calibdata)
                            //            {
                            //                var path = System.IO.Path.Combine(data.CurrentDir, calibdata.EqPath);

                            //                if (System.IO.File.Exists(path))
                            //                {
                            //                    setup.InputEqChannels[eqidx].LoadEqFromFile(path, false, true);
                            //                }
                            //            }
                            //            else
                            //            {
                            //                Warn($"Calib Data Input EQ Missed for {setup.InputEqChannels[eqidx].Eq}.");
                            //            }
                            //        }
                            //    }
                            //}

                            var outputeqname = setup.OutputEq.Eq;
                            var outeq = data.OutputEqDatas.FirstOrDefault();

                            if (outeq is null)
                            {
                                setup.OutputEq.Eq = "None";
                                //Warn($"Calib Data Output EQ Missed for {setup.OutputEq.Eq}.");
                            }
                            else
                            {
                                var path = System.IO.Path.Combine(data.RelevantDir, outeq.EqPath);

                                if (System.IO.File.Exists(path))
                                {
                                    setup.OutputEq.LoadEqFromFile(path, false, true);
                                }
                                else
                                {
                                    Warn($"Calib Data Output EQ File does not exist {path}.");
                                }
                            }
                        }
                        else if (step.MeasurementType == MeasurementType.LoudspeakerProductionTest)
                        {
                            step.Show();
                            var loudspeaker = ApRef.LoudspeakerProductionTest;

                            var cal = data.ImpedanceThieleSmalls.FirstOrDefault();

                            if (string.IsNullOrEmpty(cal.CorrectionCurve))
                            {
                                loudspeaker.AmplifierCorrectionCurve = "None";
                            }
                            else
                            {
                                if (System.IO.File.Exists(cal.CorrectionCurve))
                                {
                                    loudspeaker.LoadAmplifierCorrectionCurveFromFile(System.IO.Path.Combine(data.RelevantDir, cal.CorrectionCurve), true);
                                }
                                else
                                {
#if AP8
                                    Warn($"Calib Data Correction Curve Missed for {loudspeaker.VsenseChannel}.");
#else
                                    Warn($"Calib Data Correction Curve Missed for {loudspeaker.ExternalSenseResistorChannel}.");
#endif
                                }
                            }

                            if (!string.IsNullOrEmpty(cal.AmplifierGain)) loudspeaker.AmplifierGain.Text = cal.AmplifierGain;
                            if (!double.IsNaN(cal.SenseR)) loudspeaker.ExternalSenseResistance = cal.SenseR;
                        }
                        else if (step.MeasurementType == MeasurementType.ImpedanceThieleSmall)
                        {
                            step.Show();

                            var imp = ApRef.ImpedanceThieleSmall;
                            if (data.ImpedanceThieleSmalls.FirstOrDefault() is Calib_ImpedanceThieleSmall cal)
                            {

                                if (string.IsNullOrEmpty(cal.CorrectionCurve))
                                {
                                    imp.AmplifierCorrectionCurve = "None";
                                }
                                else
                                {
                                    if (System.IO.File.Exists(cal.CorrectionCurve))
                                    {
                                        imp.LoadAmplifierCorrectionCurveFromFile(System.IO.Path.Combine(data.RelevantDir, cal.CorrectionCurve), true);
                                    }
                                    else
                                    {
#if AP8
                                        Warn($"Calib Data Correction Curve Missed for {imp.VsenseChannel}.");
#else
                                        Warn($"Calib Data Correction Curve Missed for {imp.ExternalSenseResistorChannel}.");
#endif
                                    }
                                }

                                if (!string.IsNullOrEmpty(cal.AmplifierGain)) imp.AmplifierGain.Text = cal.AmplifierGain;
                                if (!double.IsNaN(cal.SenseR)) imp.ExternalSenseResistance = cal.SenseR;
                            }
                        }
                    }
                }
                Info($"Applied Calib Data without match from {data.RelevantDir}");

                //foreach (IProjectItem d in ApRef.AttachedProjectItems)
                //{
                //    //if(d.Name in data.InputEqDatas)
                //    //{ }
                //}

                return 1;
            }
            catch (Exception ex)
            {
                Error(ex);
                return 0;
            }
        }


        public int ApplyCalibData(ApCalibrationData data)
        {
            Info($"Applying Calib Data {data.RelevantDir}");
            try
            {
                for (int i = 0; i < ApRef.Sequence.Count; i++)
                {
                    var signalpath = ApRef.Sequence[i];

                    var signalname = signalpath.Name;
                    for (int j = 0; j < signalpath.Count; j++)
                    {
                        var step = signalpath[j];

                        var name = step.Name;
                        var type = step.MeasurementType;

                        if (step.Name.StartsWith("//"))
                        {
                            continue;
                        }
                        //else if (step.Name.StartsWith("REF:"))
                        //{
                        //    continue;
                        //}

                        //if (!step.Checked) continue;  // the setup might not checked, the reference item will not be checked 

                        if (step.MeasurementType == MeasurementType.SignalPathSetup)
                        {
                            step.Show();
                            var setup = ApRef.SignalPathSetup;
                            switch (setup.InputConnector.Type)
                            {
                                case InputConnectorType.Analog:
                                case InputConnectorType.AnalogBalanced:
                                case InputConnectorType.AnalogUnbalanced:
                                case InputConnectorType.TransducerInterface:
#if AP8
                                    if (setup.Measure == MeasurandType.Acoustic)
                                    {
                                        string unit = "V/Pa";
                                        //string unit = "mV/Pa";
                                        //if (Calib_AcousticInputChannel.SensitivityUnitIndex == 1e9)
                                        //{
                                        //    unit = "nV/Pa";
                                        //}
                                        //else if (Calib_AcousticInputChannel.SensitivityUnitIndex == 1e6)
                                        //{
                                        //    unit = "uV/Pa";
                                        //}
                                        //else if (Calib_AcousticInputChannel.SensitivityUnitIndex == 1)
                                        //{
                                        //    unit = "V/Pa";
                                        //}

                                        int accnt = data.AcousticInputs.Count();
                                        if (accnt > 0)
                                        {
                                            var channelcnt = setup.InputChannelCount;
                                            if (data.AcousticInputs.FirstOrDefault(x => x.ConnectorType == setup.InputConnector.Type) is Calib_AcousticInput cal)
                                            {
                                                for (int chidx = 0; chidx < channelcnt; chidx++)
                                                {
                                                    //var chname = setup.GetInputChannelName(chidx);
                                                    //var chname = setup.Channels[chidx].Name;
                                                    //var ch = setup.InputEqChannels[chidx];

                                                    if (cal.Channels.FirstOrDefault(x => chidx == x.Index) is Calib_AcousticInputChannel calibch)
                                                    {
                                                        //setup.References.AcousticInputReferences.SetSensitivity(chidx, calibch.Sensitivity);

                                                        setup.Channels[chidx].Sensitivity.Unit = unit;
                                                        setup.Channels[chidx].Sensitivity.Value = calibch.Sensitivity / Calib_AcousticInputChannel.SensitivityUnitIndex;
                                                    }
                                                }
                                            }
                                        }
                                    }
#else
                                    if (setup.AcousticInput)
                                    {
                                        int accnt = data.AcousticInputs.Count();
                                        if (accnt > 0)
                                        {
                                            var channelcnt = setup.InputChannelCount;
                                            if (data.AcousticInputs.FirstOrDefault(x => x.ConnectorType == setup.InputConnector.Type) is Calib_AcousticInput cal)
                                            {
                                                for (int chidx = 0; chidx < channelcnt; chidx++)
                                                {
                                                    //var chname = setup.GetInputChannelName(chidx);
                                                    //var ch = setup.InputEqChannels[chidx];

                                                    if (cal.Channels.FirstOrDefault(x => chidx == x.Index) is Calib_AcousticInputChannel calibch)
                                                    {
                                                        setup.References.AcousticInputReferences.SetSensitivity(chidx, calibch.Sensitivity / Calib_AcousticInputChannel.SensitivityUnitIndex);
                                                    }
                                                }
                                            }
                                        }
                                    }
#endif
                                    else
                                    {
                                        int accnt = data.AnalogInputs.Count();
                                        if (accnt > 0)
                                        {
                                            var channelcnt = setup.InputChannelCount;
                                            if (data.AnalogInputs.FirstOrDefault(x => x.ConnectorType == setup.InputConnector.Type) is Calib_AnalogInput cal)
                                            {
                                                setup.References.AnalogInputReferences.dBSpl1.Text = cal.dBSpl1;
                                                setup.References.AnalogInputReferences.dBSpl2.Text = cal.dBSpl2;
                                                setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text = cal.dBSpl1CalibratorLevel;
                                                setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text = cal.dBSpl2CalibratorLevel;
                                                setup.References.AnalogInputReferences.dBrA.Text = cal.dBrA;
                                                setup.References.AnalogInputReferences.dBrAOffset.Text = cal.dBrAOffset;
                                                setup.References.AnalogInputReferences.dBrB.Text = cal.dBrB;
                                                setup.References.AnalogInputReferences.dBrBOffset.Text = cal.dBrBOffset;
                                                setup.References.AnalogInputReferences.dBm.Text = cal.dBm;
                                                setup.References.AnalogInputReferences.Watts.Text = cal.Watts;

                                            }
                                        }
                                    }
                                    break;
                            }

                            switch (setup.OutputConnector.Type)
                            {
                                case OutputConnectorType.AnalogBalanced:
                                case OutputConnectorType.AnalogUnbalanced:
                                    if (setup.AcousticOutput)
                                    {
                                        int accnt = data.AcousticOutputs.Count();
                                        if (accnt > 0)
                                        {
                                            if (data.AcousticOutputs.FirstOrDefault(x => x.ConnectorType == setup.OutputConnector.Type) is Calib_AcousticOutput cal)
                                            {
                                                if(!double.IsNaN(cal.RefFreq)) setup.References.AcousticOutputReferences.ReferenceFrequency = cal.RefFreq;
                                                if (!double.IsNaN(cal.VoltageRatio)) setup.References.AcousticOutputReferences.VoltageRatio = cal.VoltageRatio;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        int accnt = data.AnalogOutputs.Count();
                                        if (accnt > 0)
                                        {
                                            if (data.AnalogOutputs.FirstOrDefault(x => x.ConnectorType == setup.OutputConnector.Type) is Calib_AnalogOutput cal)
                                            {
                                                if (!string.IsNullOrEmpty(cal.dBrG)) setup.References.AnalogOutputReferences.dBrG.Text = cal.dBrG;
                                                if (!string.IsNullOrEmpty(cal.dBm)) setup.References.AnalogOutputReferences.dBm.Text = cal.dBm;
                                                if (!string.IsNullOrEmpty(cal.Watts)) setup.References.AnalogOutputReferences.Watts.Text = cal.Watts;
                                            }
                                        }
                                    }
                                    break;
                            }

                            int inputeqcnt = data.InputEqDatas.Count();
                            if (inputeqcnt > 0)
                            {
                                for (int eqidx = 0; eqidx < setup.InputEqChannels.Count; eqidx++)
                                {
                                    var channelcnt = setup.InputChannelCount;
                                    for (int chidx = 0; chidx < channelcnt; chidx++)
                                    {
                                        var inputeqname = setup.InputEqChannels[chidx].Eq;

                                        if (inputeqname == "None") continue;

#if AP8
                                        //var chname = setup.Channels[chidx].Name;
#else
                                        //var chname = setup.GetInputChannelName(chidx);
#endif
                                        if (data.InputEqDatas.FirstOrDefault(x => eqidx == x.Index && string.Equals(x.EqPath, inputeqname, StringComparison.OrdinalIgnoreCase)) is EqCalibData calibdata)
                                        {
                                            var path = System.IO.Path.Combine(data.RelevantDir, calibdata.EqPath);

                                            if (System.IO.File.Exists(path))
                                            {
                                                setup.InputEqChannels[eqidx].LoadEqFromFile(path, false, true);
                                            }
                                        }
                                        else
                                        {
                                            Warn($"Calib Data Input EQ Missed for {setup.InputEqChannels[eqidx].Eq}.");
                                        }
                                    }
                                }
                            }


                            var outputeqname = setup.OutputEq.Eq;
                            if (outputeqname != "None")
                            {
                                var outeq = data.OutputEqDatas.FirstOrDefault(x=> string.Equals(x.EqPath, outputeqname, StringComparison.OrdinalIgnoreCase));

                                if (outeq is null)
                                {
                                    Warn($"Calib Data Output EQ Missed for {setup.OutputEq.Eq}.");
                                }
                                else
                                {
                                    var path = System.IO.Path.Combine(data.RelevantDir, outeq.EqPath);

                                    if (System.IO.File.Exists(path))
                                    {
                                        setup.OutputEq.LoadEqFromFile(path, false, true);
                                    }
                                }
                            }
                        }
                        else if (step.MeasurementType == MeasurementType.LoudspeakerProductionTest)
                        {
                            step.Show();
                            var loudspeaker = ApRef.LoudspeakerProductionTest;

                            foreach (var cal in data.LoudspeakerProductionTests)
                            {
#if AP8
                                bool issameconfig = false;
                                if (cal.TestConfiguration == LoudspeakerTestConfiguration.External1Ch && loudspeaker.Measure == LoudspeakerTestMeasurementType.VsenseOnly)
                                {
                                    issameconfig = true;
                                }
                                else if (cal.TestConfiguration == LoudspeakerTestConfiguration.External2Ch && loudspeaker.Measure == LoudspeakerTestMeasurementType.VdrvrAndVsense)
                                {
                                    issameconfig = true;
                                }

                                if (issameconfig && cal.ModelFit == loudspeaker.ModelFit)
                                {
                                    if (cal.TestConfiguration == LoudspeakerTestConfiguration.External1Ch && cal.Channel == loudspeaker.VdrvrChannel)  //Not loudspeaker.ExternalSenseResistorChannel
                                    {
                                        if (string.Equals(loudspeaker.AmplifierCorrectionCurve, cal.CorrectionCurve, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var path = System.IO.Path.Combine(data.RelevantDir, cal.CorrectionCurve);
                                            if (System.IO.File.Exists(path))
                                            {
                                                loudspeaker.LoadAmplifierCorrectionCurveFromFile(path, true);
                                            }
                                            else
                                            {
                                                Warn($"Calib Data Correction Curve Missed for {loudspeaker.VdrvrChannel}.");
                                            }
                                        }

                                        loudspeaker.AmplifierGain.Text = cal.AmplifierGain;
                                        loudspeaker.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                    else if (cal.TestConfiguration == LoudspeakerTestConfiguration.External2Ch && cal.Channel == loudspeaker.VdrvrChannel)
                                    {
                                        loudspeaker.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                }
#else
                                if (cal.TestConfiguration == loudspeaker.TestConfiguration && cal.ModelFit == loudspeaker.ModelFit)
                                {
                                    if (cal.TestConfiguration == LoudspeakerTestConfiguration.External1Ch && cal.Channel == loudspeaker.PrimaryChannel)  //Not loudspeaker.ExternalSenseResistorChannel
                                    {
                                        if (string.Equals(loudspeaker.AmplifierCorrectionCurve, cal.CorrectionCurve, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var path = System.IO.Path.Combine(data.RelevantDir, cal.CorrectionCurve);
                                            if (System.IO.File.Exists(path))
                                            {
                                                loudspeaker.LoadAmplifierCorrectionCurveFromFile(path, true);
                                            }
                                            else
                                            {
                                                Warn($"Calib Data Correction Curve Missed for {loudspeaker.PrimaryChannel}.");
                                            }
                                        }

                                        loudspeaker.AmplifierGain.Text = cal.AmplifierGain;
                                        loudspeaker.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                    else if (cal.TestConfiguration == LoudspeakerTestConfiguration.External2Ch && cal.Channel == loudspeaker.PrimaryChannel)
                                    {
                                        loudspeaker.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                }
#endif
                            }
                        }
                        else if (step.MeasurementType == MeasurementType.ImpedanceThieleSmall)
                        {
                            step.Show();

                            var imp = ApRef.ImpedanceThieleSmall;

                            foreach (var cal in data.ImpedanceThieleSmalls)
                            {
#if AP8
                                bool issameconfig = false;
                                if(cal.TestConfiguration == ImpedanceConfiguration.External1Ch && imp.Measure == ImpedanceMeasurementType.VsenseOnly)
                                {
                                    issameconfig = true;
                                }
                                else if (cal.TestConfiguration == ImpedanceConfiguration.External2Ch && imp.Measure == ImpedanceMeasurementType.VdrvrAndVsense)
                                {
                                    issameconfig = true;
                                }

                                if (issameconfig && cal.ModelFit == imp.ModelFit)
                                {
                                    if (cal.TestConfiguration == ImpedanceConfiguration.External1Ch && cal.Channel == imp.VsenseChannel)
                                    {
                                        if (string.Equals(imp.AmplifierCorrectionCurve, cal.CorrectionCurve, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var path = System.IO.Path.Combine(data.RelevantDir, cal.CorrectionCurve);
                                            if (System.IO.File.Exists(cal.CorrectionCurve))
                                            {
                                                imp.LoadAmplifierCorrectionCurveFromFile(path, true);
                                            }
                                            else
                                            {
                                                Warn($"Calib Data Correction Curve Missed for {imp.VsenseChannel}.");
                                            }
                                        }

                                        imp.AmplifierGain.Text = cal.AmplifierGain;
                                        imp.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                    else if (cal.TestConfiguration == ImpedanceConfiguration.External2Ch && cal.Channel == imp.VdrvrChannel)
                                    {
                                        imp.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                }
#else
                                if (cal.TestConfiguration == imp.TestConfiguration && cal.ModelFit == imp.ModelFit)
                                {
                                    if (cal.TestConfiguration == ImpedanceConfiguration.External1Ch && cal.Channel == imp.PrimaryChannel)
                                    {
                                        if (string.Equals(imp.AmplifierCorrectionCurve, cal.CorrectionCurve, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var path = System.IO.Path.Combine(data.RelevantDir, cal.CorrectionCurve);
                                            if (System.IO.File.Exists(cal.CorrectionCurve))
                                            {
                                                imp.LoadAmplifierCorrectionCurveFromFile(path, true);
                                            }
                                            else
                                            {
                                                Warn($"Calib Data Correction Curve Missed for {imp.PrimaryChannel}.");
                                            }
                                        }

                                        imp.AmplifierGain.Text = cal.AmplifierGain;
                                        imp.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                    else if (cal.TestConfiguration == ImpedanceConfiguration.External2Ch && cal.Channel == imp.PrimaryChannel)
                                    {
                                        imp.ExternalSenseResistance = cal.SenseR;
                                        break;
                                    }
                                }
#endif
                            }
                        }
                    }
                }
                Info($"Applied Calib Data {data.RelevantDir}");

                foreach (IProjectItem d in ApRef.AttachedProjectItems)
                {
                    //if(d.Name in data.InputEqDatas)
                    //{ }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Error(ex);
                return 0;
            }
        }

        
        public static void LoadCsvEqTable(string filename, out double[] freqs, out double[] datas, out string unit)
        {
            freqs = null;
            datas = null;
            unit = null;
            using (StreamReader sr = new StreamReader(filename))
            {
                try
                {
                    sr.ReadLine();
                    sr.ReadLine();
                    sr.ReadLine();
                    
                    unit = sr.ReadLine().Split(',')[1];
                    

                    List<double> freq = new List<double>();
                    List<double> data = new List<double>();

                    while (!sr.EndOfStream)
                    {
                        var temp = sr.ReadLine().Split(',');
                        freq.Add(double.Parse(temp[0]));
                        data.Add(double.Parse(temp[1]));
                    }

                    freqs = freq.ToArray();
                    datas = data.ToArray();
                }
                catch
                {
                    throw new InvalidDataException($"EQ {filename} data is invalid");
                }
            }
        }
    }
}
