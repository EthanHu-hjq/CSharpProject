using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TestCore;
using TestCore.Data;
using ToucanCore.Abstraction.Engine;
using System.IO.Packaging;
using System.IO.Compression;
using TestCore.Services;
using TestCore.Misc;
using TestCore.MetaData;
using System.Diagnostics;
using ApEngine.Base;
using System.Security.Claims;
using TestCore.Base;
using ApEngine.UIs;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;
using System.Reflection;
using ToucanCore.Abstraction;
using System.Timers;
using AudioPrecision.API;

#pragma warning disable 0619

namespace ApEngine
{
    public partial class ApxEngine : TF_Base, IEngine<Execution, Script>
    {
        public const string ConstName = "Apx Engine";
        public const string STEP_DELIMITER = "-&>";
        public const string MEAS_DELIMITER = "-#>";

        private const string APP_ROOT = "C:\\ProgramData\\APAcoustic";
        private const string APP_LOG = "C:\\ProgramData\\APAcoustic\\log";
        private const string APP_TEMP = "C:\\ProgramData\\APAcoustic\\temp";
        
        //public const string CalibrationBase = "C:\\ProgramData\\APAcoustic\\Calibration";
        //public const string CalibrationBaseEG1 = "C:\\ProgramData\\APAcoustic\\Calibration\\CU\\PRJ\\PRD\\STATION\\SLOT";
        //public const string ReferenceBase = "C:\\ProgramData\\APAcoustic\\Reference";
        public static string CalibrationBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Calibration");
        public static string ReferenceBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Reference");
        public static string VerificationBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Verification");

        public static string HardwareCalibrationPath { get; } = System.IO.Path.Combine(CalibrationBase, $"{Environment.MachineName}{ApCalibrationData.FileExt}");

        #region Static Data
        public static Array ThieleSmallValueList { get; } = Enum.GetValues(typeof(AudioPrecision.API.ThieleSmallParameter));
        private static AudioPrecision.API.IApplication _apref;
        
        public static AudioPrecision.API.IApplication ApRef
        {
            get
            {
                if (_apref is null)
                {
                    _apref = new AudioPrecision.API.APx500(AudioPrecision.API.APxOperatingMode.SequenceMode, false);
                }

                return _apref;
            }
        }

        public static byte AuxControlOutputValue { get; set; } 

        public static bool ApVisible { get => ApRef.Visible; set => ApRef.Visible = value; }

        public static void SetUserDefinedVariable(string key, string val) => ApRef.Variables.SetUserDefinedVariable(key, val);
        #endregion

        private Dictionary<string, object> _Variables = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> Variables { get => _Variables; }

        public string Name { get; set; } = ConstName; // Make it settable for adding version
        public string Version { get; private set; }

        public string UserName { get; set; }

        public string FileFilter => "APx Project|*.approjx";

        public bool IsInitialized { get; protected set; }
        //public bool IsRunning { get; protected set; }
        public bool IsStarted { get; private set; }

        public bool IsForVerification { get; set; }

        public bool BreakOnFirstStep { get; set; }
        public bool BreakOnFailure { get; set; }
        public bool GotoCleanupOnFailure { get; set; }
        public bool DisableResults { get; set; }
        public int ActionOnError { get; set; }

        public IModel Model { get; protected set; }
        public string StationId { get; set; }

        public TF_Result Template { get; private set; }

        private List<Execution> _Executions = new List<Execution>();
        public IReadOnlyCollection<Execution> Executions { get => _Executions; }

        private List<Script> _Scripts = new List<Script>();
        public IReadOnlyCollection<Script> Scripts { get => _Scripts; }

        public static System.Threading.ManualResetEvent Mre_Operation { get; } = new System.Threading.ManualResetEvent(true);
        public ApxEngine()
        {
        }

        public void ListMeasurement()
        {
            for (int i = 0; i < ApRef.Sequence.Count; i++)
            {
                var signalpath = ApRef.Sequence[i];

                var signalname = signalpath.Name;

                for (int j = 0; j < signalpath.Count; j++)
                {
                    var step = signalpath[j];

                    if (step.MeasurementType == AudioPrecision.API.MeasurementType.PassFail)
                    {
                        if (step.Name.StartsWith("POSTREF:"))
                        {
                        }
                    }

                    for (int idx = 0; idx < step.SequenceSteps.ImportResultDataSteps.Count; idx++)
                    {
                        var d = step.SequenceSteps.ImportResultDataSteps[idx].FileName;
                    }
                    

                    if (step.Name.StartsWith("//"))
                    {
                        continue;
                    }

                    if (step.Name.StartsWith("VER:"))
                    { }
                    else if (step.Name.StartsWith("REF:"))
                    {
                    }
                    else
                    {
                        if (step.Name.StartsWith("POSTREF:", true, System.Globalization.CultureInfo.CurrentCulture))
                        {

                        }
                        else
                        {
                        }
                    }

                    //for (int k = 0; k < step.SequenceResults.Count; k++)
                    //{
                    //    var meas = step
                    //}
                }
            }
        }

        //public void RunSequence(out MeterResult[] meters, out XyyResult[] xyys, out XyResult[] xys, out ThieleSmallResult[] tss, out PassFailResult[] pfs, out string ErrorMessage)
        //{
        //    TF_Result result = Template;

        //    result.Begin(DateTime.Now);
        //    ApRef.Sequence.Run();

        //    List<string> ErrorSteps = new List<string>();
        //    ErrorMessage = null;

        //    List<MeterResult> rss_meter = new List<MeterResult>();
        //    List<XyyResult> rss_xyy = new List<XyyResult>();
        //    List<XyResult> rss_xy = new List<XyResult>();
        //    List<ThieleSmallResult> rss_ts = new List<ThieleSmallResult>();
        //    List<PassFailResult> rss_pf = new List<PassFailResult>();
        //    bool IsPassed = ApRef.Sequence.Passed;

        //    for (int i = 0; i < ApRef.Sequence.Count; i++)
        //    {
        //        var signalpath = ApRef.Sequence[i];

        //        var signalname = signalpath.Name;

        //        var item_l1 = result.StepDatas.FirstOrDefault(x=> x.Element.Name == signalname);
        //        item_l1?.Element?.Begin(DateTime.Now);
        //        for (int j = 0; j < signalpath.Count; j++)
        //        {
        //            var step = signalpath[j];

        //            var stepname = $"{signalname}{STEP_DELIMITER}{step.Name}";

        //            var item_l2 = item_l1.FirstOrDefault(x => x.Element.Name == step.Name);
        //            item_l2?.Element?.Begin(DateTime.Now);
        //            if (step.HasSequenceResults)
        //            {
        //                for (int k = 0; k < step.SequenceResults.Count; k++)
        //                {
        //                    var seqrs = step.SequenceResults[k];
        //                    var item_l3 = item_l2.FirstOrDefault(x => x.Element.Name == seqrs.Name);
        //                    item_l3?.Element?.Begin(DateTime.Now);
        //                    if (seqrs.HasErrorMessage && ErrorMessage is null)
        //                    {
        //                        ErrorMessage = seqrs.ErrorMessage;
        //                    }

        //                    if (seqrs.HasMeterValues)
        //                    {
        //                        MeterResult rs = new MeterResult();

        //                        rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
        //                        rs.Unit = seqrs.MeterUnit;
        //                        rs.PassUlAll = seqrs.PassedUpperLimitCheck;
        //                        rs.PassLlAll = seqrs.PassedLowerLimitCheck;

        //                        if (seqrs.HasErrorMessage)
        //                        {
        //                            ErrorSteps.Add(rs.Name);
        //                        }

        //                        var vals = seqrs.GetMeterValues();
        //                        var uls = seqrs.GetMeterUpperLimitValues();
        //                        var lsl = seqrs.GetMeterLowerLimitValues();
        //                        var names = seqrs.ChannelNames;

        //                        var cnt = Math.Min(Math.Min(Math.Min(seqrs.ChannelCount, vals.Length), uls.Length), lsl.Length);

        //                        for (int chidx = 0; chidx < cnt; chidx++)
        //                        {
        //                            var item_l4 = item_l3.FirstOrDefault(x => x.Element.Name == names[chidx])?.Element as TF_ItemData;
        //                            item_l4.Begin(DateTime.Now);
        //                            MeterData data = new MeterData(
        //                                vals[chidx],
        //                                uls[chidx],
        //                                lsl[chidx],
        //                                seqrs.PassedUpperLimitCheckOnChannel(chidx),
        //                                seqrs.PassedLowerLimitCheckOnChannel(chidx));

        //                            item_l4.Value = vals[chidx];
        //                            item_l4.End(data.PassLl && data.PassUl ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
        //                            item_l4.EndTime = DateTime.Now;
        //                            rs.Datas.Add(data);
        //                        }

        //                        rss_meter.Add(rs);
        //                    }
        //                    else if (seqrs.HasXYYValues)
        //                    {
        //                        XyyResult rs = new XyyResult();
        //                        rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
        //                        rs.XUnit = seqrs.XUnit;
        //                        rs.LeftUnit = seqrs.LeftUnit;
        //                        rs.RightUnit = seqrs.RightUnit;
        //                        rs.PassUlAll = seqrs.PassedUpperLimitCheck;
        //                        rs.PassLlAll = seqrs.PassedLowerLimitCheck;

        //                        if (seqrs.HasErrorMessage)
        //                        {
        //                            ErrorSteps.Add(rs.Name);
        //                        }

        //                        var names = seqrs.ChannelNames;

        //                        for (int chidx = 0; chidx < seqrs.ChannelCount; chidx++)
        //                        {
        //                            var x_l = seqrs.GetXValues(chidx, AudioPrecision.API.VerticalAxis.Left, AudioPrecision.API.SourceDataType.Measured, 0);
        //                            var x_r = seqrs.GetXValues(chidx, AudioPrecision.API.VerticalAxis.Right, AudioPrecision.API.SourceDataType.Measured, 0);

        //                            var y_l = seqrs.GetYValues(chidx, AudioPrecision.API.VerticalAxis.Left, AudioPrecision.API.SourceDataType.Measured, 0);
        //                            var y_r = seqrs.GetYValues(chidx, AudioPrecision.API.VerticalAxis.Right, AudioPrecision.API.SourceDataType.Measured, 0);

        //                            var min = Math.Min(x_l.Length, y_l.Length);

        //                            Point[] data_l = new Point[min];

        //                            for (int idx = 0; idx < min; idx++)
        //                            {
        //                                data_l[idx] = new Point(x_l[idx], y_l[idx]);
        //                            }
                                    
        //                            min = Math.Min(x_r.Length, y_r.Length);
        //                            Point[] data_r = new Point[min];
                                    
        //                            for (int idx = 0; idx < min; idx++)
        //                            {
        //                                data_r[idx] = new Point(x_r[idx], y_r[idx]);
        //                            }

        //                            XyyData data = new XyyData()
        //                            {
        //                                XyValueLeft = data_l,
        //                                XyValueRight = data_r,
        //                                PassLl = seqrs.PassedLowerLimitCheckOnChannel(chidx),
        //                                PassUl = seqrs.PassedUpperLimitCheckOnChannel(chidx),
        //                            };

        //                            var item_l4_left = item_l3.FirstOrDefault(x => x.Element.Name == $"L {names[chidx]}")?.Element as TF_ItemData;

        //                            if (item_l4_left is null)
        //                            {
        //                                Warn($"{item_l4_left.Name} is missing");
        //                            }
        //                            else
        //                            {
        //                                if (item_l4_left.Limit.AdditionInfo is null)
        //                                {
        //                                    TF_Curve cv = item_l4_left.Limit.LSL as TF_Curve;
        //                                    if (cv is null)
        //                                    {
        //                                        cv = item_l4_left.Limit.USL as TF_Curve;
        //                                    }

        //                                    var cvtemplate = new TF_Curve(x_l, y_l, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);

        //                                    TF_Limit templimit = new TF_Limit(item_l4_left.Limit.Name,
        //                                        item_l4_left.Limit.USL,
        //                                        item_l4_left.Limit.LSL,
        //                                        item_l4_left.Limit.Comp,
        //                                        item_l4_left.Limit.Defect,
        //                                        item_l4_left.Limit.Format,
        //                                        item_l4_left.Limit.Unit,
        //                                        item_l4_left.Limit.Skip,
        //                                        item_l4_left.Limit.Sfc);

        //                                    templimit.AdditionInfo = cvtemplate;
        //                                    templimit.Tag = item_l4_left.Limit;

        //                                    if (item_l4_left.Limit.LSL is TF_Curve cvlsl)
        //                                    {
        //                                        templimit.LSL = cvlsl.Resample(cvtemplate);
        //                                    }
        //                                    if (item_l4_left.Limit.USL is TF_Curve cvusl)
        //                                    {
        //                                        templimit.USL = cvusl.Resample(cvtemplate);
        //                                    }

        //                                    item_l4_left.Limit = templimit;
        //                                }

        //                                item_l4_left.Value = new TF_Curve(x_l, y_l);
        //                            }

        //                            var name_l = seqrs.GetYText(chidx, AudioPrecision.API.VerticalAxis.Left);
        //                            var name_r = seqrs.GetYText(chidx, AudioPrecision.API.VerticalAxis.Right);

        //                            var item_l4_right = item_l3.FirstOrDefault(x => x.Element.Name == $"R {names[chidx]}")?.Element as TF_ItemData;

        //                            if (item_l4_right is null) { }
        //                            else
        //                            {
        //                                if (item_l4_right.Limit.AdditionInfo is null)
        //                                {
        //                                    TF_Curve cv = item_l4_right.Limit.LSL as TF_Curve;
        //                                    if (cv is null)
        //                                    {
        //                                        cv = item_l4_right.Limit.USL as TF_Curve;
        //                                    }

        //                                    var cvtemplate = new TF_Curve(x_l, y_l, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);

        //                                    TF_Limit templimit = new TF_Limit(item_l4_right.Limit.Name,
        //                                        item_l4_right.Limit.USL,
        //                                        item_l4_right.Limit.LSL,
        //                                        item_l4_right.Limit.Comp,
        //                                        item_l4_right.Limit.Defect,
        //                                        item_l4_right.Limit.Format,
        //                                        item_l4_right.Limit.Unit,
        //                                        item_l4_right.Limit.Skip,
        //                                        item_l4_right.Limit.Sfc);
        //                                    templimit.AdditionInfo = cvtemplate;

        //                                    templimit.Tag = item_l4_right.Limit;

        //                                    if (item_l4_right.Limit.LSL is TF_Curve cvlsl)
        //                                    {
        //                                        templimit.LSL = cvlsl.Resample(cvtemplate);
        //                                    }
        //                                    if (item_l4_right.Limit.USL is TF_Curve cvusl)
        //                                    {
        //                                        templimit.USL = cvusl.Resample(cvtemplate);
        //                                    }

        //                                    item_l4_right.Limit = templimit;
        //                                }

        //                                item_l4_right.Value = new TF_Curve(x_r, y_r);
        //                            }

        //                            rs.Datas.Add(data);
        //                        }

        //                        rss_xyy.Add(rs);
        //                    }
        //                    else if (seqrs.HasXYValues)
        //                    {
        //                        XyResult rs = new XyResult();
        //                        rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
        //                        rs.XUnit = seqrs.XUnit;
        //                        rs.YUnit = seqrs.YUnit;
        //                        rs.PassUlAll = seqrs.PassedUpperLimitCheck;
        //                        rs.PassLlAll = seqrs.PassedLowerLimitCheck;

        //                        if (seqrs.HasErrorMessage)
        //                        {
        //                            ErrorSteps.Add(rs.Name);
        //                        }

        //                        AudioPrecision.API.SourceDataType sdt = AudioPrecision.API.SourceDataType.Measured;
        //                        if (!seqrs.HasData(sdt, 0))
        //                        {
        //                            sdt = AudioPrecision.API.SourceDataType.Imported;
        //                            if (!seqrs.HasData(sdt, 0))
        //                            {
        //                                sdt = AudioPrecision.API.SourceDataType.CustomData;
        //                                if (!seqrs.HasData(sdt, 0))
        //                                {
        //                                    continue;
        //                                }
        //                            }
        //                        }

        //                        var names = seqrs.ChannelNames;
        //                        for (int chidx = 0; chidx < seqrs.ChannelCount; chidx++)
        //                        {
        //                            var xd = seqrs.GetXValues(chidx, AudioPrecision.API.VerticalAxis.Left, sdt, 1);
        //                            var yd = seqrs.GetYValues(chidx, AudioPrecision.API.VerticalAxis.Left, sdt, 1);

        //                            var min = Math.Min(xd.Length, yd.Length);

        //                            Point[] pd = new Point[min];

        //                            for (int idx = 0; idx < min; idx++)
        //                            {
        //                                pd[idx] = new Point(xd[idx], yd[idx]);
        //                            }

        //                            XyData data = new XyData()
        //                            {
        //                                XyValue = pd,
        //                                PassLl = seqrs.PassedLowerLimitCheckOnChannel(chidx),
        //                                PassUl = seqrs.PassedUpperLimitCheckOnChannel(chidx),
        //                            };

        //                            var item_l4 = item_l3.FirstOrDefault(x => x.Element.Name == names[chidx])?.Element as TF_ItemData;

        //                            if (item_l4.Limit.AdditionInfo is null)
        //                            {
        //                                TF_Curve cv = item_l4.Limit.LSL as TF_Curve;
        //                                if (cv is null)
        //                                {
        //                                    cv = item_l4.Limit.USL as TF_Curve;
        //                                }

        //                                var cvtemplate = new TF_Curve(xd, yd, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);

        //                                TF_Limit templimit = new TF_Limit(item_l4.Limit.Name,
        //                                        item_l4.Limit.USL,
        //                                        item_l4.Limit.LSL,
        //                                        item_l4.Limit.Comp,
        //                                        item_l4.Limit.Defect,
        //                                        item_l4.Limit.Format,
        //                                        item_l4.Limit.Unit,
        //                                        item_l4.Limit.Skip,
        //                                        item_l4.Limit.Sfc);

        //                                templimit.AdditionInfo = cvtemplate;

        //                                templimit.Tag = item_l4.Limit;

        //                                if (item_l4.Limit.LSL is TF_Curve cvlsl)
        //                                {
        //                                    templimit.LSL = cvlsl.Resample(cvtemplate);
        //                                }
        //                                if (item_l4.Limit.USL is TF_Curve cvusl)
        //                                {
        //                                    templimit.USL = cvusl.Resample(cvtemplate);
        //                                }

        //                                item_l4.Limit = templimit;
        //                            }

        //                            item_l4.Begin(DateTime.Now);
        //                            item_l4.Value = new TF_Curve(xd, yd);
        //                            item_l4.End(data.PassLl & data.PassUl ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
        //                            item_l4.EndTime = DateTime.Now;
        //                            rs.Datas.Add(data);
        //                        }
        //                        rss_xy.Add(rs);
        //                    }
        //                    else if (seqrs.HasThieleSmallValues)
        //                    {
        //                        ThieleSmallResult rs = new ThieleSmallResult();

        //                        rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
        //                        rs.PassUlAll = seqrs.PassedUpperLimitCheck;
        //                        rs.PassLlAll = seqrs.PassedLowerLimitCheck;

        //                        if (seqrs.HasErrorMessage)
        //                        {
        //                            ErrorSteps.Add(rs.Name);
        //                        }

        //                        foreach (AudioPrecision.API.ThieleSmallParameter ts in ThieleSmallValueList)
        //                        {
        //                            ThieleSmallData tsdata = new ThieleSmallData()
        //                            {
        //                                Parameter = ts,
        //                                Value = seqrs.GetThieleSmallValue(ts),
        //                                Unit = seqrs.GetThieleSmallValueText(ts),
        //                                LSL = seqrs.GetThieleSmallLowerLimitValue(ts),
        //                                USL = seqrs.GetThieleSmallUpperLimitValue(ts),
        //                                PassLl = seqrs.PassedThieleSmallLowerLimit(ts),
        //                                PassUl = seqrs.PassedThieleSmallUpperLimit(ts),
        //                            };

        //                            var item_l4 = item_l3?.FirstOrDefault(x => x.Element.Name == ts.ToString());

        //                            if (item_l4 is null)
        //                            {
        //                                Warn($"{ts} is missing");
        //                            }

        //                            rs.Datas.Add(tsdata);
        //                        }
        //                        rss_ts.Add(rs);
        //                    }
        //                    else if (seqrs.HasRawTextResults)
        //                    {
        //                    }
        //                    item_l3?.Element?.End();

        //                    if (item_l3 != null)
        //                    {
        //                        item_l3.Element.EndTime = DateTime.Now;
        //                    }
        //                }
        //                item_l2?.Element?.End();
        //                if (item_l2 != null)
        //                {
        //                    item_l2.Element.EndTime = DateTime.Now;
        //                }
        //            }

        //            item_l1?.Element?.End();
        //            if (item_l1 != null)
        //            {
        //                item_l1.Element.EndTime = DateTime.Now;
        //            }

        //            if (step.MeasurementType == AudioPrecision.API.MeasurementType.PassFail)
        //            {
        //                PassFailResult rs = new PassFailResult()
        //                {
        //                    Name = stepname,
        //                    Result = step.SequenceResults.PassedLimitChecks,
        //                }; 

        //                rss_pf.Add(rs);
        //            }
        //        }
        //    }

        //    result.End();

        //    int RunStepCount = rss_meter.Count + rss_xyy.Count + rss_xy.Count + rss_ts.Count;
        //    meters = rss_meter.ToArray();
        //    xyys = rss_xyy.ToArray();
        //    xys = rss_xy.ToArray();
        //    tss = rss_ts.ToArray();
        //    pfs = rss_pf.ToArray();
        //}

        public void InitWorkbase()
        {
            try
            {
                if (!Directory.Exists(APP_ROOT))
                {
                    Directory.CreateDirectory(APP_ROOT);
                }

                if (!Directory.Exists(APP_LOG))
                {
                    Directory.CreateDirectory(APP_LOG);
                }

                if (!Directory.Exists(CalibrationBase))
                {
                    Directory.CreateDirectory(CalibrationBase);
                }

                if (!Directory.Exists(ReferenceBase))
                {
                    Directory.CreateDirectory(ReferenceBase);
                }

                if (Directory.Exists(APP_TEMP))
                {
                    Directory.Delete(APP_TEMP, true);
                }
                Directory.CreateDirectory(APP_TEMP);
            }
            catch(Exception ex)
            {
                Warn($"Init Workbase Failed. ex: {ex}");
            }
        }

        public void ClearWorkbase()
        {
            try
            {
                Directory.Delete(APP_ROOT, true);
            }
            catch (Exception ex)
            {
                Warn($"Init Workbase Failed. ex: {ex}");
            }
        }

        public int Initialize()
        {
            InitWorkbase();
            IsInitialized = true;
            OnEngineInitialized?.Invoke(this, null);
            Execution.StaticEngine = this;
            return 1;
        }

        public int StartEngine()
        {
            if (IsStarted) return 1;
            if (_apref is null)
            {
                _apref = new AudioPrecision.API.APx500(AudioPrecision.API.APxOperatingMode.SequenceMode, false);
                //_apref = ApxExt.GetRemoteInstance(false, APxOperatingMode.SequenceMode, APxApplicationType.Normal);
#if !DEBUG
                _apref.Visible = false;      
                if (string.IsNullOrEmpty(_apref.ProjectFileName)) _apref.CreateNewProject();  // prevent exists project
#else
                _apref.Visible = true;
#endif
                //TimerMonitor.Elapsed += TimerMonitor_Elapsed;
                //TimerMonitor.Start();
                Version = _apref.Version.SoftwareVersion;
                AuxControlOutputValue = ApRef.AuxControlMonitor.AuxControlOutputValue;
            }
            
            IsStarted = true;
            OnEngineStarted?.Invoke(this, null);
            return 1;
        }

        //private void TimerMonitor_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    if (Mre_Operation.WaitOne())
        //    {
        //        Mre_Operation.Reset();
        //        try
        //        {
        //            if (ApRef.Sequence.Sequences.ActiveSequence != null)
        //            {
        //            }
        //            return;
        //        }
        //        catch (System.Runtime.Remoting.RemotingException ex)
        //        {
        //            var msg = $"Get Active Sequence Failed. Err {ex.Message}";
        //            Warn(msg);
        //        }
        //        finally
        //        {
        //            Mre_Operation.Set();
        //        }
        //    }
        //}

        //private System.Timers.Timer TimerMonitor { get; } = new System.Timers.Timer(5000);

        public IScript LoadScriptFile(string path)
        {
            Script script = new Script();
            if (script.Open(path) > 0)
            {
                OnScriptOpened?.Invoke(this, script);
            }
            Info($"Open {path}");
            return script;
        }

        public int FormatScript(out string formatlog)
        {
            formatlog = null;
            return 1;
        }

        public int SaveScriptAs(string dest)
        {
            ApRef.SaveProject(dest);
            return 1;
        }

        public IExecution CreateExecution(IScript script, string sequenceName)
        {
            if (Executions.FirstOrDefault(x => x.Name.Equals(sequenceName, StringComparison.OrdinalIgnoreCase)) is Execution exist)
            {
                if (exist.Script == script)
                {
                    seq_pre = script.ActiveSequence as Sequence;
                    ((Script)script)?.Activate(((Sequence)(exist.Sequence)));
                    return exist;
                }
            }

            if (script is Script apscript)
            {
                try
                {
                    if (sequenceName != null)
                    {
                        var seq = script.Sequences.FirstOrDefault(x => x.Name == sequenceName);

                        if (seq is null || ApRef.Sequence.Sequences.ActiveSequence.Name == sequenceName)
                        {

                        }
                        else
                        {
                            ApRef.Sequence.Sequences.Activate(seq.Name);
                        }
                    }
                }
                catch
                {
                    Error($"Activate seq {sequenceName} failed");
                }

                _Executions.Clear();

                var exec = new Execution(apscript);
                exec.ExecutionStarted += Exec_ExecutionStarted;

                _Executions.Add(exec);

                OnExecutionCreated?.Invoke(this, exec);
                return exec;
            }
            else
            {
                throw new InvalidOperationException($"Engine {Name} does not support {script.FilePath} in type {script.GetType()}");
            }
        }

        private void Exec_ExecutionStarted(object sender, EventArgs e)
        {
            OnExecutionStarted?.Invoke(this, sender as IExecution);
        }

        //bool IsExecutionRunning = false;
        public IExecution StartExecution(IScript script, string sequenceName)
        {
            if (Executions.FirstOrDefault(x => x.Name.Equals(sequenceName, StringComparison.OrdinalIgnoreCase)) is Execution exist)
            {
                if (exist.Script == script)
                {
                    seq_pre = script.ActiveSequence as Sequence;
                    ((Script)script)?.Activate(((Sequence)(exist.Sequence)));
                    return exist;
                }
            }

            if (script is Script apscript)
            {
                try
                {
                    if (sequenceName != null)
                    {
                        var seq = script.Sequences.FirstOrDefault(x => x.Name == sequenceName);

                        if (seq is null || ApRef.Sequence.Sequences.ActiveSequence.Name == sequenceName)
                        {

                        }
                        else
                        {
                            ApRef.Sequence.Sequences.Activate(seq.Name);
                        }
                    }
                }
                catch
                {
                    Error($"Activate seq {sequenceName} failed");
                }

                _Executions.Clear();

                var exec = new Execution(apscript);
                _Executions.Add(exec);

                exec.Start();
                OnExecutionStarted?.Invoke(this, exec);
                return exec;
            }
            else
            {
                throw new InvalidOperationException($"Engine {Name} does not support {script.FilePath} in type {script.GetType()}");
            }
        }

        //bool ContinueRun = true;

        public event EventHandler OnEngineInitialized;
        public event EventHandler OnEngineStarted;
        public event EventHandler OnEngineStopped;
        public event EventHandler<IExecution> OnExecutionCreated;
        public event EventHandler<IExecution> OnExecutionStarted;
        public event EventHandler<IExecution> OnExecutionStopped;
        public event EventHandler<Tuple<TF_Result, string>> OnReportGenerated;
        public event EventHandler<IScript> OnScriptOpened;

        public event EventHandler CalibrationExpired;
        public event EventHandler CalibrationExpiring;

        public static TestCore.Services.IToolboxService ToolboxService { get; } //= TestCore.Services.ServiceStatic.ToolboxService();
        public static TestCore.Services.ITimeService TimeService { get; } //= TestCore.Services.ServiceStatic.GetService<TestCore.Services.ITimeService>();
        public bool UiVisible { get => ApRef.Visible; set { ApRef.Visible = value; } }

        public bool IsEditMode { get; set; }

        public int StopExecution(IExecution exec)
        {
            if (exec.Stop() > 0)
            {
                OnExecutionStopped?.Invoke(this, null);
            }

            return 1;
        }

        public int ResumeAll()
        {
            return 1;
        }

        public int TerminateAll()
        {
            return 1;
        }

        public int AbortAll()
        {
            //ContinueRun = false;
            return 1;
        }

        public int StopEngine()
        {
            if (!IsStarted) return 1;

            foreach (var exec in Executions)
            {
                exec.Stop();
            }

#if !DEBUG
            
            if(_apref != null)
            {
                ApRef.CancelOperation();
                ApRef.Exit();
            }
#endif
            OnEngineStopped?.Invoke(this, null);
            return 1;
        }

        public int SetModulePath(string path)
        {
            throw new NotImplementedException();
        }

        public int SetModulePath(RunMode mode, string path)
        {
            throw new NotImplementedException();
        }

        public string GetModulePath()
        {
            throw new NotImplementedException();
        }
        
        public void LoadEquipmentData()
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

                    if (step.MeasurementType == AudioPrecision.API.MeasurementType.PassFail)
                    {
                        if (step.Name.StartsWith("POSTREF:"))
                        {
                        }
                    }

                    for (int idx = 0; idx < step.SequenceSteps.ImportResultDataSteps.Count; idx++)
                    {
                        var d = step.SequenceSteps.ImportResultDataSteps[idx].FileName;
                    }


                    if (step.Name.StartsWith("//"))
                    {
                        continue;
                    }

                    if (step.Name.StartsWith("VER:"))
                    { }
                    else if (step.Name.StartsWith("REF:"))
                    {
                        if (step is AudioPrecision.API.IContinuousSweepMeasurementBase csmb) // include IFrequencyResponseMeasurement
                        {
                            csmb.Generator.EQSettings.ImportData("", "", AudioPrecision.API.InputChannelIndex.Ch1);
                        }
                        else if (step is AudioPrecision.API.IContinuousSweepSettingsWithAdditionalAcqTimeBase csswaatb)
                        {
                            csswaatb.Generator.EQSettings.ImportData("", "", AudioPrecision.API.InputChannelIndex.Ch1);
                            csswaatb.Generator.EQSettings.EQTableType = AudioPrecision.API.EQType.Absolute;
                            csswaatb.Generator.EQSettings.LevelUnit = "";
                            csswaatb.Generator.EQSettings.SetEQTable(new double[]{ }, new double[]{});
                        }
                        if (step is AudioPrecision.API.ISteppedFrequencySweepMeasurementBase sfsmb)
                        {
                            sfsmb.Generator.EQSettings.ImportData("", "", AudioPrecision.API.InputChannelIndex.Ch1);
                        }

                        if (step is AudioPrecision.API.IAcousticResponseMeasurement arm)
                        {
                            arm.GeneratorWithPilot.EQSettings.ImportData("", "", AudioPrecision.API.InputChannelIndex.Ch1);
                        }

                        if (step is AudioPrecision.API.IMultitoneAnalyzerMeasurement mam)
                        {
                        }

                        if (step.MeasurementType == AudioPrecision.API.MeasurementType.LoudspeakerProductionTest)
                        {
                            if (step is AudioPrecision.API.ILoudspeakerProductionTestMeasurement lsptm)
                            {
#if AP8
                                if (lsptm.Measure == LoudspeakerTestMeasurementType.VsenseOnly)
                                {
                                    //lsptm.TestConfiguration = AudioPrecision.API.LoudspeakerTestConfiguration.External1Ch;
                                    //lsptm.LoadAmplifierCorrectionCurveFromFile("", true);  //TODO
                                    lsptm.AmplifierGain.Value = 10.8;
                                    lsptm.ExternalSenseResistance = 0.1;
                                }
#else
                                if (lsptm.TestConfiguration == AudioPrecision.API.LoudspeakerTestConfiguration.External1Ch)
                                {
                                    lsptm.TestConfiguration = AudioPrecision.API.LoudspeakerTestConfiguration.External1Ch;
                                    //lsptm.LoadAmplifierCorrectionCurveFromFile("", true);  //TODO
                                    lsptm.AmplifierGain.Value = 10.8;
                                    lsptm.ExternalSenseResistance = 0.1;
                                }
#endif
                            }
                        }

                        

                        if (step is AudioPrecision.API.ISignalPathSetup setup)
                        {
                            setup.References.AnalogInputReferences.dBrA.Value = 0;
                            setup.References.AnalogInputReferences.dBrB.Value = 0;
                            setup.References.AnalogInputReferences.dBrAOffset.Value = 0;
                            setup.References.AnalogInputReferences.dBrBOffset.Value = 0;

                            setup.References.AnalogInputReferences.dBSpl1.Value = 0;
                            setup.References.AnalogInputReferences.dBSpl1.Unit = "";
                            setup.References.AnalogInputReferences.dBSpl1CalibratorLevel.Value = 0;
                            setup.References.AnalogInputReferences.dBSpl2.Value = 0;
                            setup.References.AnalogInputReferences.dBSpl2.Unit = "";
                            setup.References.AnalogInputReferences.dBSpl2CalibratorLevel.Value = 0;

                            setup.References.AnalogOutputReferences.dBm.Value = 0;
                            setup.References.AnalogOutputReferences.dBrG.Value = 0;

                            var outchcnt = setup.OutputChannelCount;
                            var inchcnt = setup.InputChannelCount;

                            var conntype = setup.InputConnector.Type;
                        }
                    }
                    else
                    {
                        if (step.Name.StartsWith("POSTREF:", true, System.Globalization.CultureInfo.CurrentCulture))
                        {

                        }
                        else
                        {
                        }
                    }

                    //for (int k = 0; k < step.SequenceResults.Count; k++)
                    //{
                    //    var meas = step
                    //}
                }
            }
        }

        public int Login(string username, string password)
        {
            UserName = username;
            return 1;
        }

        public int FormatScript()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public int OpenApx()
        {
            if (ApRef is null) return 0;

            try
            {
                ApRef.CancelOperation();

                if (ApRef.IsDemoMode)
                {
                }

                //if (ApRef.Version.SoftwareVersion > "4.4")
                //{

                //}
            }
            catch
            { }

            return 1;
        }
        public void LoadApxProject(string apfile, string apconfig)
        {
            if (System.IO.File.Exists(apfile))
            {
                ApRef.OpenProject(apfile);

                ApRef.SignalMonitorsEnabled = false;

                var calibdatas = ToolboxService.StationInstance?.ApplyEquipmentCalibData(apconfig);
                if (calibdatas != null)
                {
                    foreach (var calib in calibdatas)
                    {
                        if (System.IO.Path.GetExtension(calib) == ApCalibrationData.FileExt)
                        {
                            ApplyCalibData(ApCalibrationData.Load(calib));
                        }
                    }
                }

                _Executions.Clear();
            }
        }

        public void LoadApac(string src, string apprjfile, string configfile, string supportdir)
        {
            throw new NotImplementedException();
        }

        public int GenerateReport(TF_Result rs, string basepath)
        {
            var fname = rs.GenerateReportName("xlsx");
            var path = Path.Combine(basepath, fname);
            try
            {
                ApRef.Sequence.Report.ExportXls(path);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(basepath);
                rs.XmlSerialize().Save(path);
            }
            catch (AudioPrecision.API.APException apex)
            {
                if (fname.Contains(':'))
                {
                    fname = fname.Replace(':', '`');
                    path = Path.Combine(basepath, fname);
                    ApRef.Sequence.Report.ExportXls(path);
                }
                else
                {
                    throw apex;
                }
            }
            OnReportGenerated?.Invoke(this, new Tuple<TF_Result, string>(rs, path));
            return 1;
        }

        internal const string ReferenceSequenceName = "REF";
        internal const string VerificationSequenceName = "VER";
        internal const string DefaultSequenceName = "DUT";
        Sequence seq_pre = null;
        //Reference referenceui;
        public IExecution StartReferenceExecution(IScript script)
        {
            if(Executions.FirstOrDefault(x=> x.Name.Equals(ReferenceSequenceName, StringComparison.OrdinalIgnoreCase)) is Execution exist)
            {
                seq_pre = script.ActiveSequence as Sequence;
                ((Script)script)?.Activate(((Sequence)(exist.Sequence)));

                foreach(var item in ReferenceRecords)
                {
                    item.Value.Clear();
                }

                return exist;
            }

            foreach (Sequence seq in script.Sequences)
            {
                if (seq.Name.Equals(ReferenceSequenceName, StringComparison.OrdinalIgnoreCase))
                {
                    seq_pre = script.ActiveSequence as Sequence;

                    ((Script)script)?.Activate(seq);

                    var exportfiles = ((Script)script).ImportFiles.ToArray();
                    //if (referenceui is null) referenceui = new Reference();
                    //foreach (var file in ((Script)script).ImportFiles) referenceui.ImportFiles.Add(file);  // the Reference must be behind the normal execution;
                    //script.Analyze();

                    script.Spec = script.AnalyzeSpec();

                    //foreach (var file in ((Script)script).ExportFiles) referenceui.ExportFiles.Add(file);
                    var exec = new Execution((Script)script);

                    var refdir = ((Script)script).GetReferenceBase();
                    if (!Directory.Exists(refdir)) Directory.CreateDirectory(refdir);

                    for (int i = 0; i < script.SystemConfig.General.SocketCount; i++)
                    {
                        var refpath = System.IO.Path.Combine(refdir, i.ToString());

                        if (!Directory.Exists(refpath))
                        {
                            Directory.CreateDirectory(refpath);
                        }
                    }

                    ReferenceRecords?.Clear();
                    ReferenceLoop = 1;
                    foreach (var slotrs in exec.Results)
                    {
                        slotrs.IsSFC = false;
                        if (slotrs.AttachProperties.ContainsKey("Tag"))
                        {
                            slotrs.AttachProperties["Tag"] = ApxEngine.ReferenceSequenceName;
                        }
                        else
                        {
                            slotrs.AttachProperties.Add("Tag", ApxEngine.ReferenceSequenceName);
                        }

                        ReferenceRecords.Add(slotrs.SocketId, new List<TF_Result>());
                    }

                    exec.OnTestCompleted += ReferenceExec_OnTestCompleted;
                    exec.ExecutionStopped += (sender, e) =>
                    {
                        try
                        {
                            ApxEngine.Mre_Operation.Set();  // for this call in test process, which lock the ap resource. need to be release the lock
                            ((Script)script)?.Activate(seq_pre);
                        }
                        catch
                        { 
                        }
                    };
                    OnExecutionStarted?.Invoke(this, exec);

                    _Executions.Add(exec);
                    //referenceui.Show();
                    return exec;
                }
            }

            if (seq_pre != null)
            {
                ((Script)script)?.Activate(seq_pre);
            }

            return null;
        }

        private int ReferenceLoop = 1;
        private Dictionary<string, List<TF_Result>> ReferenceRecords = new Dictionary<string, List<TF_Result>>();

        private void ReferenceExec_OnTestCompleted(object sender, TF_Result e)
        {
            if(sender is Execution exec)
            {
                ReferenceRecords[e.SocketId].Add(e.Clone() as TF_Result);

                bool refcomplete = true;
                foreach(var record in ReferenceRecords)
                {
                    refcomplete &= record.Value.Count(x => x.Result == TF_ItemStatus.Passed) >= ReferenceLoop;
                    if (!refcomplete) break;
                }

                if(refcomplete)
                {
                    exec.Stop();
                    //referenceui?.Hide();   // may thread exception
                }
            }
        }

        public IExecution StartVerificationExecution(IScript script)
        {
            if (Executions.FirstOrDefault(x => x.Name.Equals(VerificationSequenceName, StringComparison.OrdinalIgnoreCase)) is Execution exist)
            {
                seq_pre = script.ActiveSequence as Sequence;
                ((Script)script)?.Activate(((Sequence)(exist.Sequence)));

                foreach (var item in ReferenceRecords)
                {
                    item.Value.Clear();
                }

                return exist;
            }

            foreach (Sequence seq in script.Sequences)
            {
                if (seq.Name.Equals(VerificationSequenceName, StringComparison.OrdinalIgnoreCase))
                {
                    seq_pre = script.ActiveSequence as Sequence;
                    ((Script)script)?.Activate(seq);

                    //script.Analyze();
                    script.Spec = script.AnalyzeSpec();

                    var exec = new Execution((Script)script);
                    exec.ExecutionStopped += (sender, e) =>
                    {
                        ApxEngine.Mre_Operation.Set();
                        ((Script)script)?.Activate(seq_pre);
                    };
                    ReferenceRecords.Clear();
                    ReferenceLoop = 1;
                    exec.OnTestCompleted += ReferenceExec_OnTestCompleted;

                    exec.OnTestCompleted += (sender, e) =>
                    {
                        if(e.Result == TF_ItemStatus.Passed)
                        {
                            var verbase = script.GetVerificationBase();
                            if (!Directory.Exists(verbase)) Directory.CreateDirectory(verbase);

                            var verpath = System.IO.Path.Combine(verbase, e.SocketIndex.ToString());
                            if (Directory.Exists(verpath))
                            {
                                Directory.Delete(verpath, true);
                            }
                            Directory.CreateDirectory(verpath);

                            var file = Path.Combine(verpath, e.GenerateReportName(string.Empty));
                            File.Create(file).Close();
                        }
                        // Verification On Check if the file exist
                    };

                    foreach (var slotrs in exec.Results)
                    {
                        slotrs.IsSFC = false;

                        if (slotrs.AttachProperties.ContainsKey("Tag"))
                        {
                            slotrs.AttachProperties["Tag"] = ApxEngine.VerificationSequenceName;
                        }
                        else
                        {
                            slotrs.AttachProperties.Add("Tag", ApxEngine.VerificationSequenceName);
                        }
                        ReferenceRecords.Add(slotrs.SocketId, new List<TF_Result>());
                    }

                    OnExecutionStarted?.Invoke(this, exec);

                    _Executions.Add(exec);

                    return exec;
                }
            }

            if (seq_pre is null)
            {
                ((Script)script)?.Activate(seq_pre);
            }

            return null;
        }

        //ScriptCalibData scr = null;
        public int StartCalibration()
        {
            ApxCalibration calibration = new ApxCalibration(null);
            calibration.ShowDialog();

            //if (Scripts.Count > 0)
            //{

            //    if (MessageBox.Show("Click Yes to Run Calibration for Script, otherwise Run Calibration for Hardware", "Select Calibration Type",
            //        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            //    {
            //        if (ScriptCalibration.Engine is null) ScriptCalibration.Engine = this;
            //        ScriptCalibration script = new ScriptCalibration(Scripts.FirstOrDefault());

            //        script.ShowDialog();
            //    }
            //    else
            //    {
            //        ApxCalibration calibration = new ApxCalibration(null);
            //        calibration.ShowDialog();
            //    }
            //}
            //else
            //{

            //}

            return 1;

            //Info_EquipmentInstance eq = null;
            //Calibration calibration = new Calibration(eq);

            //if (calibration.ShowDialog() == true)
            //{ 

            //}
            //return 0;
        }

        public IScript NewScript(TestCore.Configuration.GlobalConfiguration config = null)
        {
            var script = Script.NewScript(UserName, config);
            _Scripts.Clear();

            _Executions.Clear();

            _Scripts.Add(script);

            return script;
        }

        Timer CalibrationExpiredTimer;

        public int ApplyCalibration()
        {
            var path = ApxEngine.HardwareCalibrationPath;

            if(!File.Exists(path))
            {
                return 1;
            }
            ApCalibrationData calibdata = ApCalibrationData.Load(path);

            var timespan = calibdata.UpdateTime.Add(calibdata.ValidTime).Subtract(DateTime.Now);
            var wt = timespan.Subtract(calibdata.WarnTime);

            if (timespan.TotalMilliseconds <= 0)
            {
                throw new CalibrationDataExpiredException() { CalibrationDataPath = path };
            }
            else
            {
                CalibrationExpiredTimer?.Stop();
                CalibrationExpiredTimer?.Dispose();
                CalibrationExpiredTimer = new Timer();

                if (wt.TotalMilliseconds < 0)    // Time in Warning
                {
                    CalibrationExpiredTimer.Interval = timespan.TotalMilliseconds;
                    CalibrationExpiredTimer.Elapsed += (sender, e) => 
                    {
                        CalibrationExpiredTimer.Stop();
                        CalibrationExpired?.Invoke(this, e);
                    };
                    ApplyCalibData(calibdata);

                    throw new CalibrationDataExpiringWarning() { CalibrationDataPath = path, RemainedHours = timespan.TotalHours };
                }
                else
                {
                    CalibrationExpiredTimer.Interval = wt.TotalMilliseconds;
                    CalibrationExpiredTimer.Elapsed += (sender, e) => 
                    {
                        CalibrationExpiredTimer.Stop();
                        CalibrationExpiring?.Invoke(this, e);
                    };
                }
            }

            var rtn = ApplyCalibData(calibdata);

            return rtn;
        }

        private void CalibrationExpiringTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CalibrationExpired?.Invoke(this, e);
            CalibrationExpiredTimer?.Stop();
        }

        public void ShowEngineSettingDialog()
        {
            Setting setting = new Setting(Scripts.FirstOrDefault());
            setting.ShowDialog();
        }
    }

    public class ApxScriptAnalysisResult
    {
        public DateTime Time { get; set; }

        public ToucanCore.Abstraction.Engine.IScript Script { get; set; }

        public TF_Spec Spec { get; set; }

        public ApxScriptAnalysisResult()
        {
            Spec = new TF_Spec("TYM", "1.0");
        }
    }

    public class MeterResult
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public List<MeterData> Datas { get; } = new List<MeterData>();
        public bool PassUlAll { get; set; }
        public bool PassLlAll { get; set; }
    }

    public struct MeterData
    {
        public double MeterValue { get; set; }
        public double USL { get; set; }
        public double LSL { get; set; }
        public bool PassUl { get; set; }
        public bool PassLl { get; set; }

        public MeterData(double val, double ul, double ll, bool passul, bool passll)
        {
            MeterValue = val;
            USL = ul;
            LSL = ll;
            PassUl = passul;
            PassLl = passll;
        }
    }

    public class XyyResult
    {
        public string Name { get; set; }
        public string XUnit { get; set; }
        public string LeftUnit { get; set; }
        public string RightUnit { get; set; }
        public List<XyyData> Datas { get; } = new List<XyyData>();
        public bool PassUlAll { get; set; }
        public bool PassLlAll { get; set; }
    }

    public struct XyyData
    {
        public Point[] XyValueLeft { get; set; }
        public Point[] XyValueRight { get; set; }
        public bool PassUl { get; set; }
        public bool PassLl { get; set; }
    }

    public class XyResult
    {
        public string Name { get; set; }
        public string XUnit { get; set; }
        public string YUnit { get; set; }
        public List<XyData> Datas { get; } = new List<XyData>();
        public bool PassUlAll { get; set; }
        public bool PassLlAll { get; set; }
    }

    public struct XyData
    {
        public Point[] XyValue { get; set; }
        public bool PassUl { get; set; }
        public bool PassLl { get; set; }
    }


    public struct Point
    {
        public double X { get; }
        public double Y { get; }
        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class ThieleSmallResult
    {
        public string Name { get; set; }
        public List<ThieleSmallData> Datas { get; } = new List<ThieleSmallData>();

        public bool PassUlAll { get; set; }
        public bool PassLlAll { get; set; }
    }

    public struct ThieleSmallData
    {
        public AudioPrecision.API.ThieleSmallParameter Parameter { get; set; }
        public string Unit { get; set; }
        public double Value { get; set; }
        public double USL { get; set; }
        public double LSL { get; set; }
        public bool PassUl { get; set; }
        public bool PassLl { get; set; }
    }

    public class PassFailResult
    {
        public string Name { get; set; }
        public bool Result { get; set; }
    }
}
