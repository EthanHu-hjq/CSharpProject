using AudioPrecision.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Data;
using TestCore;
using ApEngine.Base;
using System.Security.Claims;
using System.Windows.Navigation;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using System.Diagnostics.Eventing.Reader;
using System.Xml.Linq;
using System.Windows.Controls;
using System.Security.Policy;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.Security.Permissions;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace ApEngine
{
    [Serializable]
    public class SignalPath : TF_Base
    {
        public string Name { get; set; }

        [XmlIgnore]
        public AudioPrecision.API.ISignalPath ApSignalPath { get; }

        //public bool IsAcousticOut { get; internal set; }
        //public bool IsAcousticIn { get; internal set; }

        [XmlIgnore]
        public List<string> ImportFiles { get; internal set; } = new List<string>();

        [XmlIgnore]
        public List<string> ExportFiles { get; internal set; } = new List<string>();

        public SignalPathCalibData CalibData { get; set; } = new SignalPathCalibData();

        [XmlIgnore]
        public Nest<TF_Limit> Limits { get; internal set; }

        private readonly static Array ThieleSmallParameters = Enum.GetValues(typeof(ThieleSmallParameter));

        public SignalPath() { }

        public SignalPath(AudioPrecision.API.ISignalPath apsp)
        {
            ApSignalPath = apsp;
            Name = apsp.Name;
            Limits = new Nest<TF_Limit>() { Element = new TF_Limit(Name) };
        }

        public int Analyze()
        {
            List<EqTableFile> tempeqtables = new List<EqTableFile>();
            List<Calib_ImpedanceThieleSmall> tempts = new List<Calib_ImpedanceThieleSmall>();
            List<Calib_LoudspeakerProductionTest> templpt = new List<Calib_LoudspeakerProductionTest>();

            ImportFiles.Clear();
            ExportFiles.Clear();

            for (int j = 0; j < ApSignalPath.Count; j++)
            {
                var step = ApSignalPath[j];

                if (!step.Checked) continue;

                var stepname = step.Name;
                var type = step.MeasurementType;

                for (int idx = 0; idx < step.SequenceSteps.ImportResultDataSteps.Count; idx++)
                {
                    var d = step.SequenceSteps.ImportResultDataSteps[idx].FileName;
                    ImportFiles.Add(d);
                    Info($"Step: {stepname}. Call: {d}");
                }

                for (int idx = 0; idx < step.SequenceSteps.ExportResultDataSteps.Count; idx++)
                {
                    var d = step.SequenceSteps.ExportResultDataSteps[idx].FileName;
                    ExportFiles.Add(d);
                    Info($"Step: {stepname}. Call: {d}");
                }
                
                switch (type)
                {
                    case MeasurementType.SignalPathSetup:
                        step.Show();
                        var setup = ApxEngine.ApRef.SignalPathSetup;
                        if (setup.AcousticOutput)
                        {
                            CalibData.AcousticOutput = new Calib_AcousticOutput()
                            {
                                RefFreq = setup.References.AcousticOutputReferences.ReferenceFrequency,
                                VoltageRatio = setup.References.AcousticOutputReferences.VoltageRatio,
                                ConnectorType = setup.OutputConnector.Type,
                            };
                        }
                        else
                        {
                            CalibData.AnalogOutput = new Calib_AnalogOutput()
                            {
                                dBm = setup.References.AnalogOutputReferences.dBm.Text,
                                dBrG = setup.References.AnalogOutputReferences.dBrG.Text,
                                Watts = setup.References.AnalogOutputReferences.Watts.Text,
                                ConnectorType = setup.OutputConnector.Type,
                            };
                        }

#if AP8
                        if (setup.Measure == MeasurandType.Acoustic)
                        {
                            CalibData.AcousticInput = new Calib_AcousticInput()
                            {
                                ConnectorType = setup.InputConnector.Type,
                            };
                            CalibData.AcousticInput.Level = setup.Channels.CalibratorLevel.Text;
                            CalibData.AcousticInput.Frequency = setup.Channels.CalibratorFrequency;
                            CalibData.AcousticInput.Tolerance = setup.Channels.CalibratorFrequencyTolerance;


                            var chs = new Calib_AcousticInputChannel[setup.Channels.Count];
                            for (int idx = 0; idx < chs.Length; idx++)
                            {
                                chs[idx] = new Calib_AcousticInputChannel()
                                {
                                    Index = idx,
                                    Sensitivity = setup.Channels[idx].Sensitivity.Value,
                                    Sensitivity_Expected = setup.Channels[idx].ExpectedSensitivity.Value,
                                    Sensitivity_Tolerance = setup.Channels[idx].SensitivityTolerance.Value,
                                    SerialNo = setup.Channels[idx].SerialNumber,
                                };
                            }

                            CalibData.AcousticInput.Channels = chs.ToList();
                        }
#else
                        if (setup.AcousticInput)
                        {
                            CalibData.AcousticInput = new Calib_AcousticInput()
                            {
                                ConnectorType = setup.InputConnector.Type,
                                Level = setup.References.AcousticInputReferences.CalibratorLevel.Text,
                                Frequency = setup.References.AcousticInputReferences.CalibratorFrequency,
                                Tolerance = setup.References.AcousticInputReferences.CalibratorFrequencyTolerance,
                            };

                            var chs = new Calib_AcousticInputChannel[setup.References.AcousticInputReferences.Count];
                            for (int idx = 0; idx < chs.Length; idx++)
                            {
                                chs[idx] = new Calib_AcousticInputChannel()
                                {
                                    Index = idx,
                                    Sensitivity = setup.References.AcousticInputReferences.GetSensitivity(idx),
                                    Sensitivity_Expected = setup.References.AcousticInputReferences.GetExpectedSensitivity(idx),
                                    Sensitivity_Tolerance = setup.References.AcousticInputReferences.GetSensitivityTolerance(idx),
                                    SerialNo = setup.References.AcousticInputReferences.GetSerialNum(idx),
                                };
                            }

                            CalibData.AcousticInput.Channels = chs.ToList();
                        }
#endif
                        else
                        {
                            CalibData.AnalogInput = new Calib_AnalogInput()
                            {
                                ConnectorType = setup.InputConnector.Type,
                                dBrA = setup.References.AnalogInputReferences.dBrA.Text,
                                dBrAOffset = setup.References.AnalogInputReferences.dBrAOffset.Text,
                                dBrB = setup.References.AnalogInputReferences.dBrB.Text,
                                dBrBOffset = setup.References.AnalogInputReferences.dBrBOffset.Text,
                                dBSpl1 = setup.References.AnalogInputReferences.dBSpl1.Text,
                                dBSpl1CalibratorLevel = setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Text,
                                dBSpl2 = setup.References.AnalogInputReferences.dBSpl2.Text,
                                dBSpl2CalibratorLevel = setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Text,
                                dBm = setup.References.AnalogInputReferences.dBm.Text,
                                Watts = setup.References.AnalogInputReferences.Watts.Text,
                            };
                        }
                        break;
                    case MeasurementType.LoudspeakerProductionTest:
                        step.Show();
                        //var lspt = meas as AudioPrecision.API.ILoudspeakerProductionTestMeasurement;
                        var lspt = ApxEngine.ApRef.LoudspeakerProductionTest;
#if AP8
                        Calib_LoudspeakerProductionTest calib_lspt = new Calib_LoudspeakerProductionTest()
                        {
                            SenseR = lspt.ExternalSenseResistance,
                            ModelFit = lspt.ModelFit,
                            AmplifierGain = lspt.AmplifierGain.Text,
                            StepName = stepname
                        };

                        if (lspt.Measure == LoudspeakerTestMeasurementType.VdrvrAndVsense)
                        {
                            calib_lspt.TestConfiguration = LoudspeakerTestConfiguration.External2Ch;
                        }
                        else
                        {
                            calib_lspt.TestConfiguration = LoudspeakerTestConfiguration.External1Ch;
                        }

                        if (calib_lspt.TestConfiguration == LoudspeakerTestConfiguration.External1Ch)
                        {
                            calib_lspt.Channel = lspt.VsenseChannel;
                        }
                        else
                        {
                            calib_lspt.Channel = lspt.VdrvrChannel;
                        }
#else
                        Calib_LoudspeakerProductionTest calib_lspt = new Calib_LoudspeakerProductionTest()
                        {
                            TestConfiguration = lspt.TestConfiguration,
                            SenseR = lspt.ExternalSenseResistance,
                            ModelFit = lspt.ModelFit,
                            AmplifierGain = lspt.AmplifierGain.Text,
                            StepName = stepname
                        };

                        if (lspt.TestConfiguration == LoudspeakerTestConfiguration.External1Ch)
                        {
                            calib_lspt.Channel = lspt.ExternalSenseResistorChannel;
                        }
                        else
                        {
                            calib_lspt.Channel = lspt.PrimaryChannel;
                        }
#endif
                        templpt.Add(calib_lspt);
                        break;
                    case MeasurementType.ImpedanceThieleSmall:
                        step.Show();
                        //var ts = meas as AudioPrecision.API.IImpedanceThieleSmallMeasurement;
                        var ts = ApxEngine.ApRef.ImpedanceThieleSmall;
#if AP8
                        var calib_ts = new Calib_ImpedanceThieleSmall()
                        {
                            SenseR = ts.ExternalSenseResistance,
                            ModelFit = ts.ModelFit,
                            AmplifierGain = ts.AmplifierGain.Text,
                            //Channel = ts.PrimaryChannel,
                            //ExternalSenseResistorChannel = ts.ExternalSenseResistorChannel,
                            StepName = stepname,
                        };

                        if (ts.Measure == ImpedanceMeasurementType.VdrvrAndVsense)
                        {
                            calib_ts.TestConfiguration = ImpedanceConfiguration.External2Ch;
                        }
                        else
                        {
                            calib_ts.TestConfiguration = ImpedanceConfiguration.External1Ch;
                        }

                        if (calib_ts.TestConfiguration == ImpedanceConfiguration.External1Ch)
                        {
                            calib_ts.Channel = ts.VsenseChannel;
                        }
                        else
                        {
                            calib_ts.Channel = ts.VdrvrChannel;
                        }
#else
                        var calib_ts = new Calib_ImpedanceThieleSmall()
                        {
                            TestConfiguration = ts.TestConfiguration,
                            SenseR = ts.ExternalSenseResistance,
                            ModelFit = ts.ModelFit,
                            AmplifierGain = ts.AmplifierGain.Text,
                            //Channel = ts.PrimaryChannel,
                            //ExternalSenseResistorChannel = ts.ExternalSenseResistorChannel,
                            StepName = stepname,
                        };

                        if (ts.TestConfiguration == ImpedanceConfiguration.External1Ch)
                        {
                            calib_ts.Channel = ts.ExternalSenseResistorChannel;
                        }
                        else
                        {
                            calib_ts.Channel = ts.PrimaryChannel;
                        }
#endif
                        tempts.Add(calib_ts);
                        break;
                    case MeasurementType.FrequencyResponse:
                    case MeasurementType.ContinuousSweep:
                    case MeasurementType.SteppedFrequencySweep:
                    case MeasurementType.AcousticResponse:
                        step.Show();
                        tempeqtables.Add(new EqTableFile() { StepName = stepname });
                        break;
                    case MeasurementType.MultitoneAnalyzer:
                        step.Show();
                        CalibData.MultitoneAnalyzer.Add(stepname, null);
                        break;

                    default:
                        step.Show();
                        break;
                }

                if (type == MeasurementType.PassFail)
                {
                    Info($"PassFail Item {stepname}");
                    TF_Limit pf = new TF_Limit(stepname, 1, 1, Comparison.EQ, null);

                    Limits.Add(pf);
                }
                else
                {
                    Nest<TestCore.Data.TF_Limit> subitem = new Nest<TestCore.Data.TF_Limit>(new TestCore.Data.TF_Limit(stepname));

                    int graphcnt = ApxEngine.ApRef.ActiveMeasurement.Graphs.Count;
                    //ApxEngine.ApRef.ActiveMeasurement.ClearData();   // if clear data, the specifdatapoint etc channel count will be same as input channel count

                    for (int graphidx = 0; graphidx < graphcnt; graphidx++)
                    {
                        IGraph graph = ApxEngine.ApRef.ActiveMeasurement.Graphs[graphidx];
                        
                        if (!graph.Checked) continue;

                        var gname = graph.Name;
                        Nest<TestCore.Data.TF_Limit> sublimit = new Nest<TestCore.Data.TF_Limit>(new TestCore.Data.TF_Limit(graph.Name));

                        //var snsr = ApxEngine.ApRef.ActiveMeasurement.SequenceMeasurement.SequenceResults[graphidx];
                        //var snsrname = snsr.Name;
                        //Info($"Compare: {graph.Name}, {snsrname}");   // the index of SequenceResults might not be same as Graphs, such as Impulse Response

                        if (graph.Result.IsMeterGraph)
                        {
                            var meter = graph.Result.AsMeterGraph();  // if Clear Data, the count of channel, limit, value will be same as ChannelCount
                            var usls = meter.Limits.Upper.GetValues();
                            var lsls = meter.Limits.Lower.GetValues();

                            var chcnt = meter.ChannelCount;

                            IGraph tracesourcegraph = meter;

                            if (graph.ViewType == MeasurementResultType.MeterStatistics)
                            {
                                // the XY Stat graph will make the tag with x data
                                if (graph.Result.AsStatisticsMeterFromXYResult().Source is IXYGraph xysource)
                                {
                                    var tracestyle = xysource.GetTraceStyles(SourceDataType.Measured, 1, VerticalAxis.Left);
                                    chcnt = xysource.ChannelCount;

                                    for (int chidx = 0; chidx < chcnt; chidx++)
                                    {
                                        //var chname = meter.ChannelNames[chidx];                // The channel name might be updated when test by APx
                                        var chname = tracestyle.GetName(chidx);

                                        var chlsl = lsls[chidx];
                                        var chusl = usls[chidx];

                                        Comparison comp = Comparison.GELE;
                                        if (double.IsNaN(chusl))
                                        {
                                            if (double.IsNaN(chlsl))
                                            {
                                                continue; //igore this item;
                                            }
                                            else
                                            {
                                                comp = Comparison.GE;
                                            }
                                        }
                                        else
                                        {
                                            if (double.IsNaN(chlsl))
                                            {
                                                comp = Comparison.LE;
                                            }
                                            else
                                            {
                                                comp = Comparison.GELE;
                                            }
                                        }

                                        var chlimit = new AP_Limit(chname, chusl, chlsl, comp, null, meter.Axis.Unit) { ChannelIndex = chidx };

                                        sublimit.Add(chlimit);
                                    }
                                }
                                else
                                {
                                    for (int chidx = 0; chidx < chcnt; chidx++)
                                    {
                                        //var chname = meter.ChannelNames[chidx];                // The channel name might be updated when test by APx
                                        var chname = meter.GetTraceName(chidx);

                                        var chlsl = lsls[chidx];
                                        var chusl = usls[chidx];

                                        Comparison comp = Comparison.GELE;
                                        if (double.IsNaN(chusl))
                                        {
                                            if (double.IsNaN(chlsl))
                                            {
                                                continue; //igore this item;
                                            }
                                            else
                                            {
                                                comp = Comparison.GE;
                                            }
                                        }
                                        else
                                        {
                                            if (double.IsNaN(chlsl))
                                            {
                                                comp = Comparison.LE;
                                            }
                                            else
                                            {
                                                comp = Comparison.GELE;
                                            }
                                        }

                                        var chlimit = new AP_Limit(chname, chusl, chlsl, comp, null, meter.Axis.Unit) { ChannelIndex = chidx };

                                        sublimit.Add(chlimit);
                                    }
                                }
                            }
                            else
                            {
                                for (int chidx = 0; chidx < chcnt; chidx++)
                                {
                                    //var chname = meter.ChannelNames[chidx];                // The channel name might be updated when test by APx
                                    var chname = meter.GetTraceName(chidx);

                                    var chlsl = lsls[chidx];
                                    var chusl = usls[chidx];

                                    Comparison comp = Comparison.GELE;
                                    if (double.IsNaN(chusl))
                                    {
                                        if (double.IsNaN(chlsl))
                                        {
                                            continue; //igore this item;
                                        }
                                        else
                                        {
                                            comp = Comparison.GE;
                                        }
                                    }
                                    else
                                    {
                                        if (double.IsNaN(chlsl))
                                        {
                                            comp = Comparison.LE;
                                        }
                                        else
                                        {
                                            comp = Comparison.GELE;
                                        }
                                    }

                                    var chlimit = new AP_Limit(chname, chusl, chlsl, comp, null, meter.Axis.Unit) { ChannelIndex = chidx };

                                    sublimit.Add(chlimit);
                                }
                            }
                            
                        }
                        else if (graph.Result.IsXYGraph) //TODO, For setting Nesting
                        {
                            var xygraph = graph.Result.AsXYGraph();

                            IXYTraceStyleCollection tracestyle = xygraph.GetTraceStyles(SourceDataType.Measured, 1, VerticalAxis.Left); ;
                            var chcnt = xygraph.ChannelCount;
                            if (graph.ViewType == MeasurementResultType.SpecifyXYDataPoints)
                            {
                                if(graph.Result.AsSpecifyDataPointsResult().Source is IXYGraph source)
                                {
                                    tracestyle = source.GetTraceStyles(SourceDataType.Measured, 1, VerticalAxis.Left);
                                    chcnt = source.ChannelCount;
                                }
                            }

                            var isXyy = xygraph.IsRightAxisResultDefined;

                            var usl = xygraph.UpperLimit;
                            var lsl = xygraph.LowerLimit;

                            //var chcnt = xygraph.ChannelCount;  // the ChannelCount might be not same as ChannelNames Length, which will make index exception
                            // this Channel Names length will include the data imported
                            //var chcnt = xygraph.ChannelCount;

                            // the data is just for interaction in UI, no meaning in APIs
                            
                            //var tracestyle = xygraph.GetTraceStyles(SourceDataType.Measured, 1, VerticalAxis.Left);
                            //var chcnt = tracestyle.Count;   // don't know why it is constant 16
                            for (int chidx = 0; chidx < chcnt; chidx++)
                            {
                                //if (chtype == SourceDataType.Imported) continue; // Ignore the import data, for it is consitent, which should not treat as item
                                //if (!xygraph.GetChannelVisible(chidx)) continue; // Channel visible does not work
                                //if (!chvisible) continue;

                                try
                                {
                                    var chname = $"{tracestyle.GetName(chidx)}";
                                    var visible = tracestyle.GetVisible(chidx);
                                    if (visible)
                                    {
                                        var hasusl = usl.HasLimitOnChannel(chidx);
                                        var haslsl = lsl.HasLimitOnChannel(chidx);

                                        double[] chusl_x;
                                        double[] chusl_y;
                                        double[] chlsl_x;
                                        double[] chlsl_y;
                                        if (hasusl)
                                        {
                                            chusl_x = usl.GetXValues(chidx);
                                            chusl_y = usl.GetYValues(chidx);
                                            TestCore.Data.TF_Curve cv_usl = new TestCore.Data.TF_Curve(chusl_x, chusl_y, xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                            if (haslsl)
                                            {
                                                chlsl_x = lsl.GetXValues(chidx);
                                                chlsl_y = lsl.GetYValues(chidx);

                                                TestCore.Data.TF_Curve cv_lsl = new TestCore.Data.TF_Curve(chlsl_x, chlsl_y, xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                                var chlimit = new AP_Limit(chname, cv_usl, cv_lsl, Comparison.GELE, null);
                                                chlimit.ChannelIndex = chidx;
                                                sublimit.Add(chlimit);
                                            }
                                            else
                                            {
                                                var chlimit = new AP_Limit(chname, cv_usl, null, Comparison.LE, null);
                                                chlimit.ChannelIndex = chidx;
                                                sublimit.Add(chlimit);
                                            }
                                        }
                                        else if (haslsl)
                                        {
                                            chlsl_x = lsl.GetXValues(chidx);
                                            chlsl_y = lsl.GetYValues(chidx);

                                            TestCore.Data.TF_Curve cv_lsl = new TestCore.Data.TF_Curve(chlsl_x, chlsl_y, xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                            var chlimit = new AP_Limit(chname, null, cv_lsl, Comparison.GE, null);
                                            chlimit.ChannelIndex = chidx;
                                            sublimit.Add(chlimit);
                                        }
                                        //else  // if add the log item, the seqrs count might be less then the channel, which make the behind item not test
                                        //{
                                        //    TestCore.Data.TF_Curve cv_temp = new TestCore.Data.TF_Curve(new double[0], new double[0], xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                        //    var chlimit = new AP_Limit(chname, null, cv_temp, Comparison.LOG, null);
                                        //    chlimit.ChannelIndex = chidx;
                                        //    sublimit.Add(chlimit);
                                        //}  // for the derived data such as Comparison, the hiden channel mighte be set as visible but with no data when test
                                    }
                                    //else
                                    //{
                                    //    TestCore.Data.TF_Curve cv_temp = new TestCore.Data.TF_Curve(new double[0], new double[0], xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                    //    var chlimit = new TestCore.Data.TF_Limit(chname, null, cv_temp, Comparison.LOG, null);

                                    //    sublimit.Add(chlimit);
                                    //}
                                }
                                catch(IndexOutOfRangeException)
                                {

                                }
                            }

                            if (isXyy)   // Right Axis should be pair with Left data
                            {
                                usl = xygraph.UpperLimitRight;
                                lsl = xygraph.LowerLimitRight;

                                tracestyle = xygraph.GetTraceStyles(SourceDataType.Measured, 1, VerticalAxis.Right);
                                
                                //chcnt = tracestyle.Count;
                                for (int chidx = 0; chidx < chcnt; chidx++)
                                {
                                    try
                                    {
                                        var chname = tracestyle.GetName(chidx);
                                        var visible = tracestyle.GetVisible(chidx);
                                        if (visible)
                                        {
                                            var hasusl = usl.HasLimitOnChannel(chidx);
                                            var haslsl = lsl.HasLimitOnChannel(chidx);

                                            double[] chusl_x;
                                            double[] chusl_y;
                                            double[] chlsl_x;
                                            double[] chlsl_y;
                                            if (hasusl)
                                            {
                                                chusl_x = usl.GetXValues(chidx);
                                                chusl_y = usl.GetYValues(chidx);
                                                TestCore.Data.TF_Curve cv_usl = new TestCore.Data.TF_Curve(chusl_x, chusl_y, xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                                if (haslsl)
                                                {
                                                    chlsl_x = lsl.GetXValues(chidx);
                                                    chlsl_y = lsl.GetYValues(chidx);

                                                    TestCore.Data.TF_Curve cv_lsl = new TestCore.Data.TF_Curve(chlsl_x, chlsl_y, xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                                    var chlimit = new AP_Limit(chname, cv_usl, cv_lsl, Comparison.GELE, null);
                                                    chlimit.ChannelIndex = -chidx-1;
                                                    sublimit.Add(chlimit);
                                                }
                                                else
                                                {
                                                    var chlimit = new AP_Limit(chname, cv_usl, null, Comparison.LE, null);
                                                    chlimit.ChannelIndex = -chidx-1;
                                                    sublimit.Add(chlimit);
                                                }
                                            }
                                            else if (haslsl)
                                            {
                                                chlsl_x = lsl.GetXValues(chidx);
                                                chlsl_y = lsl.GetYValues(chidx);

                                                TestCore.Data.TF_Curve cv_lsl = new TestCore.Data.TF_Curve(chlsl_x, chlsl_y, xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                                var chlimit = new AP_Limit(chname, null, cv_lsl, Comparison.GE, null);
                                                chlimit.ChannelIndex = -chidx-1;
                                                sublimit.Add(chlimit);
                                            }
                                            //else
                                            //{
                                            //    TestCore.Data.TF_Curve cv_temp = new TestCore.Data.TF_Curve(new double[0], new double[0], xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                            //    var chlimit = new AP_Limit(chname, null, cv_temp, Comparison.LOG, null);
                                            //    chlimit.ChannelIndex = chidx;
                                            //    sublimit.Add(chlimit);
                                            //}
                                        }
                                        //else
                                        //{
                                        //    TestCore.Data.TF_Curve cv_temp = new TestCore.Data.TF_Curve(new double[0], new double[0], xygraph.XAxis.Unit, xygraph.YAxis.Unit, xygraph.XAxis.IsLog, xygraph.YAxis.IsLog);

                                        //    var chlimit = new TestCore.Data.TF_Limit(chname, null, cv_temp, Comparison.LOG, null);

                                        //    sublimit.Add(chlimit);
                                        //}
                                    }
                                    catch(IndexOutOfRangeException)
                                    { }
                                }
                            }
                        }
                        else if (graph.Result.IsTabularResult)
                        {
                            //Could not confirm if all TabularResult is ThieleSmall Data
                            var tab = graph.Result.AsTabularResult();
                            var tabrowcnt = tab.RowCount;
                            var tabcolcnt = tab.ColumnCount;

                            try
                            {
                                var textname = System.IO.Path.Combine(Path.GetTempPath(), "TS");
                                tab.SaveReport(textname, ReportExportFormat.Text);

                                var textpath = $"{textname}.csv";

                                using (StreamReader sr = new StreamReader(textpath))  // the Apx add the ext implictly
                                {
                                    sr.ReadLine();
                                    sr.ReadLine();

                                    while (!sr.EndOfStream)
                                    {
                                        var row = sr.ReadLine().Split(',');

                                        if (row.Length >= 4)
                                        {
                                            var name = row[0];
                                            double lsl = double.NaN;
                                            double usl = double.NaN;

                                            if (!string.IsNullOrEmpty(row[2]))
                                            {
                                                if (double.TryParse(row[2], out double temp)) lsl = temp;
                                            }
                                            if (!string.IsNullOrEmpty(row[3]))
                                            {
                                                if (double.TryParse(row[3], out double temp)) usl = temp;
                                            }

                                            if (double.IsNaN(lsl) && double.IsNaN(usl)) { }
                                            else
                                            {
                                                var tslimit = new TestCore.Data.TF_Limit($"{name}", usl, lsl, Comparison.GELE, null, null);
                                                sublimit.Add(tslimit);
                                            }
                                        }
                                    }
                                }

                                File.Delete(textpath);
                            }
                            catch
                            { 
                            }

                            //if (tabcolcnt >= 2)
                            //{
                            //    for (int rowid = 0; rowid < tabrowcnt; rowid++)
                            //    {
                            //        var name = tab.GetValue(rowid, 0);
                            //        var value = tab.GetValue(rowid, 1);
                                    
                            //        string tsunit = null;
                            //        var vstr = value.Split(' ');
                            //        if(vstr.Length > 1)
                            //        {
                            //            tsunit = vstr.Last();
                            //        }

                            //        var tslimit = new TestCore.Data.TF_Limit($"{name}", null, null, Comparison.LOG, null, tsunit);
                            //        sublimit.Add(tslimit);
                            //    }
                            //}
                        }

                        if (sublimit.Count > 0)
                        {
                            subitem.Add(sublimit);
                        }
                    }

                    //foreach (AudioPrecision.API.ISequenceStep d in step.SequenceSteps)
                    //{
                    //    if (d is IImportResultDataStep import)
                    //    {
                    //        ImportFiles.Add(import.FileName);
                    //        //if (import.FileName.Contains(":")) // Abs Path, Do not Reference
                    //        //{

                    //        //}
                    //    }
                    //    else if (d is IExportResultDataStep export)
                    //    {
                    //        ExportFiles.Add(export.FileName);
                    //    }
                    //}

                    if (subitem.Count > 0)
                    {
                        Limits.Add(subitem);
                    }
                }
            }

            if (tempeqtables.Count > 0) CalibData.EqTableFiles = tempeqtables.ToArray();
            if (tempts.Count > 0) CalibData.ImpedanceThieleSmalls = tempts.ToArray();
            if (templpt.Count > 0) CalibData.LoudspeakerProductionTests = templpt.ToArray();

            return 1;
        }
    }

    internal class AP_Limit : TF_Limit
    {

        /// <summary>
        /// AP Measurement Channal Index
        /// If in XYY graph, the left same as XY Graph, and the right axis channle should be -1 - Channal Index
        /// </summary>
        [NonSerialized]
        internal int ChannelIndex;

        #region Constructor

        public AP_Limit(string name, IComparable usl, IComparable lsl, Comparison comp, string defectCode, string unit = null, string format = null, bool skiped = false, bool sfc = false) : base(name, usl, lsl, comp, defectCode, unit, format, skiped, sfc)
        {
        }

        public AP_Limit(string name) : base(name)  { } //null limit, for readability.

        public AP_Limit(string name, double usl, double lsl, Comparison comp, string defectCode, string unit = null, string format = null, bool skiped = false, bool sfc = false) : base(name, (IComparable)usl, (IComparable)lsl, comp, defectCode, unit, format, skiped, sfc) { }

        public AP_Limit(string name, string lsl, Comparison comp, string defectCode, string unit = null, string format = null, bool skiped = false, bool sfc = false) : base(name, null, lsl, comp, defectCode, unit, format, skiped, sfc) { }

        public AP_Limit(string name, bool skip = false) : base(name, skip) { }
        #endregion
    }
}
