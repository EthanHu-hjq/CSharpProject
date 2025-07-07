using NationalInstruments.TestStand.Interop.API;
using NationalInstruments.TestStand.Interop.UI.Ax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using TestCore;
using ToucanCore;
using ToucanCore.UIs;
using Toucan.Ctrls;
using System.Threading;
using System.Net.Mail;
using TestCore.Abstraction.Process;

namespace Toucan
{
    public sealed class TYM_TS_Engine : TestEngine
    {
        internal AxApplicationMgr TS_AppMgr = new AxApplicationMgr();
        private AxSequenceFileViewMgr TS_SeqFileViewMgr = new AxSequenceFileViewMgr();

        public Form Parent { get; }
        public System.ComponentModel.ComponentResourceManager Resources { get; }

        public override bool BreakOnFirstFailure { get => TS_AppMgr.GetEngine().StationOptions.BreakOnStepFailure; set => TS_AppMgr.GetEngine().StationOptions.BreakOnStepFailure = value; }
        public override bool BreakOnFirstStep { get => TS_AppMgr.BreakOnFirstStep; set => TS_AppMgr.BreakOnFirstStep = value; }
        public override bool AlwaysGotoCleanupOnFailure { get => TS_AppMgr.GetEngine().StationOptions.AlwaysGotoCleanupOnFailure; set => TS_AppMgr.GetEngine().StationOptions.AlwaysGotoCleanupOnFailure = value; }
        public override bool DisableResults { get => TS_AppMgr.GetEngine().StationOptions.DisableResults; set => TS_AppMgr.GetEngine().StationOptions.DisableResults = value; }
        public override int ActionOnError { get => (int)TS_AppMgr.GetEngine().StationOptions.RTEOption; set => TS_AppMgr.GetEngine().StationOptions.RTEOption = (RTEOptions)value; }

        public override string Version { get => TS_AppMgr.GetEngine().VersionString; }
        public override string UserName { get; set; }

        SlotInfo_TS[] InternalSlotCtrls;

        public NationalInstruments.TestStand.Interop.API.Execution Execution;
        private NationalInstruments.TestStand.Interop.API.SequenceFile SequenceFile;
        //private NationalInstruments.TestStand.Interop.API.Sequence Sequence;

        //public NamedPipeQueueServer NamedPipeQueueServer { get; private set; }

        private SequenceContext[] SlotSequenceContexts;

        private NamedPipeQueueServer[] SlotBlockQueues;
        private Queue<TF_Result>[] SlotReportQueues;  // For the Report might generate in async

        public bool IsSequentialModel { get; private set; }
        public bool IsParallelModel { get; private set; }
        public bool IsBatchModel { get; private set; }
        public override string FileFilter { get; } = "TestStand Sequence|*.seq";


        public IToolboxService ToolboxService { get; set; }
        public IAuthService AuthService { get; set; }
        public ITimeService TimeService { get; set; }

        IMes MesInstance;

        public TYM_TS_Engine(Form parentform)
        {
            Name = "TestStand";

            Parent = parentform;
            Resources = new System.ComponentModel.ComponentResourceManager(parentform.GetType());

            TS_AppMgr.OcxState = ((System.Windows.Forms.AxHost.State)(Resources.GetObject("axApplicationMgr.OcxState")));
            TS_SeqFileViewMgr.OcxState = ((System.Windows.Forms.AxHost.State)(Resources.GetObject("axSequenceFileViewMgr.OcxState")));
        }

        public override int Initialize()
        {
            if (IsInitialized) return 1;

            TS_AppMgr.BeginInit();
            TS_SeqFileViewMgr.BeginInit();
            Parent.Controls.Add(TS_AppMgr);
            Parent.Controls.Add(TS_SeqFileViewMgr);

            TS_AppMgr.EndInit();
            TS_SeqFileViewMgr.EndInit();

            TS_AppMgr.StartExecution += TS_AppMgr_StartExecution;
            TS_AppMgr.EndExecution += TS_AppMgr_EndExecution;
            TS_AppMgr.AfterUIMessageEvent += TS_AppMgr_AfterUIMessageEvent;

            TS_AppMgr.LoginOnStart = false;
            TS_AppMgr.Start();
            IsInitialized = true;
            return 1;
        }

        private bool Pre_BreakOnFirstStep;
        private bool Pre_BreakOnStepFailure;
        private bool Pre_BreakOnSequenceFailure;
        private bool Pre_DisableResults;
        private bool Pre_AlwaysGotoCleanupOnFailure;
        private NationalInstruments.TestStand.Interop.API.RTEOptions Pre_RTEOptions;
        private bool Pre_BreakOnError;
        //private bool Pre_BreakpointsEnabled;
        private bool Pre_UseStationModel;
        private bool Pre_TracingEnable;

        public override int StartEngine()
        {
            Info("Starting Engine");
            TS_AppMgr.GetEngine().AutoLoginSystemUser = true;

            //TS_AppMgr.LoginOnStart = true;
            TS_AppMgr.Start();

            if (Environment.Is64BitProcess != TS_AppMgr.GetEngine().Is64Bit)
            {
                MessageBox.Show("Current App is x64, but Current Test Engine is not x64. the test might not works");
                Application.Exit();
            }

            TS_AppMgr.GetEngine().StationOptions.AutoLoginSystemUser = true;

            Pre_BreakOnFirstStep = TS_AppMgr.BreakOnFirstStep;

            Pre_BreakOnError = TS_AppMgr.GetEngine().BreakOnRTE;
            Pre_DisableResults = TS_AppMgr.GetEngine().DisableResults;
            Pre_AlwaysGotoCleanupOnFailure = TS_AppMgr.GetEngine().AlwaysGotoCleanupOnFailure;
            Pre_RTEOptions = TS_AppMgr.GetEngine().RTEOption;
            Pre_BreakOnSequenceFailure = TS_AppMgr.GetEngine().StationOptions.BreakOnSequenceFailure;
            Pre_BreakOnStepFailure = TS_AppMgr.GetEngine().StationOptions.BreakOnStepFailure;
            //Pre_BreakpointsEnabled = TS_AppMgr.GetEngine().StationOptions.BreakpointsEnabled;
            Pre_UseStationModel = TS_AppMgr.GetEngine().StationOptions.UseStationModel;
            Pre_TracingEnable = TS_AppMgr.GetEngine().StationOptions.TracingEnabled;

            TS_AppMgr.BreakOnFirstStep = false;
            TS_AppMgr.GetEngine().BreakOnRTE = false;
            TS_AppMgr.GetEngine().StationOptions.BreakOnStepFailure = false;
            TS_AppMgr.GetEngine().StationOptions.BreakOnSequenceFailure = false;
            //TS_AppMgr.GetEngine().StationOptions.BreakpointsEnabled = false;
            TS_AppMgr.GetEngine().StationOptions.TracingEnabled = true;
            //TS_AppMgr.GetEngine().StationOptions.ExecutionMask = 0;

            TS_AppMgr.GetEngine().StationOptions.AlwaysGotoCleanupOnFailure = GlobalConfiguration.Default.General.AutoCleanupWhenFailure;
            TS_AppMgr.GetEngine().StationOptions.DisableResults = false;
            TS_AppMgr.GetEngine().StationOptions.RTEOption = NationalInstruments.TestStand.Interop.API.RTEOptions.RTEOption_Continue;
            TS_AppMgr.GetEngine().StationOptions.UseStationModel = true;

            if (TS_AppMgr.GetEngine().StationOptions.SeqFileVersionAutoIncrementOpt == FileVersionAutoIncrement.FileVersionInc_None)
            {
                TS_AppMgr.GetEngine().StationOptions.SeqFileVersionAutoIncrementOpt = FileVersionAutoIncrement.FileVersionInc_Build;
            }

            TS_ZeroTime = TS_ZeroTime.AddSeconds(TS_AppMgr.GetEngine().SecondsAtStartIn1970UniversalCoordinatedTime + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalSeconds);

            IsReadyToRun = true;
            //if (TS_AppMgr.GetEngine().CurrentUser is null)
            //{
            //    var userobj = TS_AppMgr.GetEngine().GetUser("administrator");
            //    //TS_AppMgr.GetEngine().DisplayLoginDialog("Login", "administrator", "", true, out User userobj);
            //    TS_AppMgr.GetEngine().CurrentUser = userobj;
            //}


            Info("Engine Started");
            return 1;
        }

        public override int StopEngine()
        {
            try
            {
                Info("Stopping Engine");

                foreach (NationalInstruments.TestStand.Interop.API.Execution exec in TS_AppMgr.Executions)
                {
                    exec.GetStates(out ExecutionRunStates s1, out ExecutionTerminationStates t1);
                    Info(string.Format("Pre: {0}, {1}. runstate {2}, termstat {3}", exec.DisplayName, exec.ResultStatus, s1, t1));
                }

                TerminateAll();

                TS_AppMgr.BreakOnFirstStep = Pre_BreakOnFirstStep;

                TS_AppMgr.GetEngine().BreakOnRTE = Pre_BreakOnError;
                TS_AppMgr.GetEngine().DisableResults = Pre_DisableResults;
                TS_AppMgr.GetEngine().AlwaysGotoCleanupOnFailure = Pre_AlwaysGotoCleanupOnFailure;
                TS_AppMgr.GetEngine().RTEOption = Pre_RTEOptions;
                TS_AppMgr.GetEngine().StationOptions.BreakOnSequenceFailure = Pre_BreakOnSequenceFailure;
                TS_AppMgr.GetEngine().StationOptions.BreakOnStepFailure = Pre_BreakOnStepFailure;
                TS_AppMgr.GetEngine().StationOptions.TracingEnabled = Pre_TracingEnable;

                ToucanCore.UIs.TimeoutMessageBox timeoutmsg = new TimeoutMessageBox();
                timeoutmsg.Timeout_ms = 5000;
                timeoutmsg.ElapsedAction = CheckIfAllExecutionStopped;
                timeoutmsg.ShowDialog();

                TS_AppMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_CloseAll, 0);

                if (TS_SeqFileViewMgr?.SequenceFile != null)
                {
                    TS_AppMgr.CloseSequenceFile(TS_SeqFileViewMgr.SequenceFile);
                    TS_AppMgr.GetEngine().ReleaseSequenceFileEx(TS_SeqFileViewMgr.SequenceFile);
                }

                GC.Collect();

                foreach (NationalInstruments.TestStand.Interop.API.Execution exec in TS_AppMgr.Executions)
                {
                    exec.GetStates(out ExecutionRunStates s1, out ExecutionTerminationStates t1);
                    Info(string.Format("Post: {0}, {1}. runstate {2}, termstat {3}", exec.DisplayName, exec.ResultStatus, s1, t1));
                }

                TS_AppMgr?.GetEngine().AbortAll();
                TS_AppMgr?.CloseAllExecutions();  // If there execution not close, it will promote message
                TS_AppMgr?.CloseAllSequenceFiles();
                TS_AppMgr?.GetEngine().UnloadAllModules();
                TS_AppMgr?.Shutdown();
                TS_AppMgr?.Dispose();
                Info("Engine Stopped");

                return 1;
            }
            catch (Exception ex)
            {
                Error(ex);
            }

            return 0;
        }
        private Dictionary<string, List<StepAnalysisResult>> StaticAnalyzeResult { get; set; }
        private TF_Spec PersistentSpec { get; set; }
        public override int LoadScriptFile(string path)
        {
            try
            {
                ScriptFilePath = null;
                SequenceFile = TS_AppMgr.GetEngine().GetSequenceFileEx(path);
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                Regex re = new Regex(@"Change\s*the\s*Station\s*Model\s*to\s*'(.+)'");

                var match = re.Match(ex.Message);

                if (match.Success)
                {
                    Warn("Model Setting Exception. Change as Seq Required");

                    SetModulePath(match.Groups[1].Value);
                    SequenceFile = TS_AppMgr.GetEngine().GetSequenceFileEx(path);
                }
                else
                {
                    Error(ex);
                    throw ex;
                }
            }
            TS_SeqFileViewMgr.SequenceFile = SequenceFile;

            var configfile = Path.Combine(Path.GetDirectoryName(path), GlobalConfiguration.DefaultFileName);

            PersistentSpec = null;
            if (File.Exists(configfile))
            {
                GlobalConfiguration.Default.Reload(configfile);
                var config = GlobalConfiguration.Default;

                var spec = Path.Combine(Directory.GetParent(SequenceFile.Path).FullName, TF_Spec.DefaultFileName);

                if (config.General.DisableRemoteReport)
                {
                    ReportService?.StopAsync();
                }
                else
                {
                    ReportService?.StartAsync();
                }

                if (config.General.RestrictLimit)
                {
                    if (File.Exists(spec))
                    {
                        PersistentSpec = TF_Spec.LoadFromXml(spec);
                    }
                    else
                    {
                        if (AuthService?.CurrentAuthType >= AuthType.Engineer)
                        {
                            if (MessageBox.Show("No Spec File Detected. Are you want to Generate One. All history data will be missed.\r\n未检测到Spec文件，是否需要重新生成. 历史数据将会丢失", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                            {
                                var sar = AnalyzeScript();
                                sar.Spec.Author = AuthService.UserName;
                                sar.Spec.Time = TimeService?.CurrentTime ?? DateTime.Now;
                                sar.Spec.Note = "Auto Generate By Toucan";
                                sar.Spec.XmlSerialize().Save(spec);
                                PersistentSpec = sar.Spec;

                                FileInfo fi = new FileInfo(spec);
                                fi.IsReadOnly = true;
                            }
                            else
                            {
                                throw new FileNotFoundException($"Spec file not found. Please contact with Engineer");
                            }
                        }
                        else
                        {
                            throw new FileNotFoundException($"Spec file not found. Please contact with Engineer");
                        }
                    }
                }

                IsOriginalModel = false;
                CustomizeInputSn = config.General.CustomizeInputSn;
                StaticAnalyzeResult = StaticAnalyzeSeqFile(SequenceFile);

                var fatal = false;
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("==== Static Sequence Analysis Report ====");
                foreach (var item in StaticAnalyzeResult)
                {
                    sb.AppendLine($"== Seq: {item.Key} ==");
                    foreach (var errors in item.Value)
                    {
                        if (errors.StepErrors.Count == 0)
                        {
                        }
                        else
                        {
                            var tempsb = new StringBuilder();
                            foreach (var err in errors.StepErrors)
                            {
                                switch (err.Key)
                                {
                                    case StepFormatError.DuplicatedItemName:
                                    case StepFormatError.IllegalStepName:
                                    case StepFormatError.DefectCodeConfliction:
                                    case StepFormatError.RecurisiveCall:
                                        tempsb.Append($" Fatal-> {err.Key}, ");
                                        fatal = true;
                                        break;
                                    case StepFormatError.ActionWithJudgement:
                                    case StepFormatError.ConditionalItem:
                                    case StepFormatError.DynamicLimit:
                                        tempsb.Append($" Warning->{err.Key}, ");
                                        break;
                                }
                            }

                            var tempstr = tempsb.ToString();

                            if (string.IsNullOrEmpty(tempstr)) continue;

                            sb.AppendLine($"  |- {errors.StepName}: {tempstr}");
                        }
                    }
                }

                if (fatal)
                {
                    var msg = sb.ToString();
                    MessageBox.Show(msg, "Illegal Script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    throw new InvalidDataException("Illegal Script");
                }

                SequenceFileFormated = false;
            }
            else
            {
                IsOriginalModel = true;
                PersistentSpec = null;
            }

            ScriptFilePath = path;

            return 1;
        }

        bool SequenceFileFormated = false;

        public override ScriptAnalysisResult AnalyzeScript()
        {
            try
            {
                var sar = new ScriptAnalysisResult()
                {
                    ScriptName = "sar",//fiseq.Name,
                    ScriptVersion = SequenceFile.AsPropertyObjectFile().Version,
                    Time = DateTime.Now,//fiseq.LastWriteTime,
                };

                var mainseq = SequenceFile.GetSequenceByName("MainSequence");

                SeqToData(mainseq, sar.Spec.Limit, true);

                List<KeyValuePair<string, StepFormatError>> errors = new List<KeyValuePair<string, StepFormatError>>();
                ScriptAnalysisResult.VerifyLimit(sar.Spec.Limit, errors);

                if (!IsOriginalModel)
                {
                    if (errors.Count > 0)
                    {
                        StringBuilder sb = new StringBuilder();

                        foreach (var error in errors)
                        {
                            sb.AppendLine(string.Format("{0}\t{1, 24}", error.Key, error.Value));
                        }

                        var msg = sb.ToString();

                        MessageBox.Show(msg, "Illegal Script", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw new InvalidDataException("Illegal Script");
                    }
                }

                sar.GenerateTemplate(PersistentSpec);

                return sar;
            }
            catch (Exception ex)
            {
                PersistentSpec = null;
                Warn(ex);
                throw ex;  // throw it for notify the asyn caller
            }
        }

        private void SeqToData(NationalInstruments.TestStand.Interop.API.Sequence seq, Nest<TF_Limit> ndata, bool ismes = true)
        {
            try
            {
                var cnt = seq.GetNumSteps(StepGroups.StepGroup_Setup);

                for (var i = 0; i < cnt; i++)
                {
                    var step = seq.GetStep(i, StepGroups.StepGroup_Setup);
                    var datas = StepToData(step, ismes);

                    if (datas is null) continue;

                    if (datas.Element.Comp != Comparison.NULL || datas.Count > 0)
                    {
                        ndata.Add(datas);
                    }
                }

                cnt = seq.GetNumSteps(StepGroups.StepGroup_Main);

                for (var i = 0; i < cnt; i++)
                {
                    var step = seq.GetStep(i, StepGroups.StepGroup_Main);
                    var datas = StepToData(step, ismes);

                    if (datas is null) continue;

                    if (datas.Element.Comp != Comparison.NULL || datas.Count > 0)
                    {
                        ndata.Add(datas);
                    }
                }

                cnt = seq.GetNumSteps(StepGroups.StepGroup_Cleanup);

                for (var i = 0; i < cnt; i++)
                {
                    var step = seq.GetStep(i, StepGroups.StepGroup_Cleanup);
                    var datas = StepToData(step, ismes);

                    if (datas is null) continue;

                    if (datas.Element.Comp != Comparison.NULL || datas.Count > 0)
                    {
                        ndata.Add(datas);
                    }
                }
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }

        private const int DefectStart = 100;
        private const int DefectEnd = 999;
        private Regex DefectFormat = new Regex(@"^@?(([\w-_]+)(\d+))");

        private Nest<TF_Limit> StepToData(Step step, bool ismes)
        {
            var type = step.StepType.Name;
            string defectcode = null;

            string comment = step.AsPropertyObject().Comment;
            string skipstr = step.GetRunModeEx();
            bool skip = skipstr == "Skip";

            TF_Limit limit = null;
            Nest<TF_Limit> n_Limit = null;

            Match match = null;

            switch (type)
            {
                case "PassFailTest":
                    match = DefectFormat.Match(comment);
                    if (match.Success)
                    {
                        defectcode = match.Groups[1].Value;
                    }

                    limit = new TF_Limit(step.Name, 1, 1, Comparison.EQ, defectcode, null, null, skip, ismes);
                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    if (step.IsSequenceCall)
                    {
                        var seq = SequenceFile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(seq, n_Limit, ismes);
                    }

                    break;

                case "NumericLimitTest":
                    match = DefectFormat.Match(comment);
                    if (match.Success)
                    {
                        defectcode = match.Groups[1].Value;
                    }

                    var hl = step.AsPropertyObject().GetValNumber("Limits.High", 0);
                    var ll = step.AsPropertyObject().GetValNumber("Limits.Low", 0);
                    var comp = step.AsPropertyObject().GetValString("Comp", 0);
                    var unit = step.AsPropertyObject().GetValString("Result.Units", 0);

                    if (Enum.TryParse(comp, out Comparison comparison))
                    {
                        switch (comparison)
                        {
                            case Comparison.LE:
                            case Comparison.LT:
                                limit = new TF_Limit(step.Name, ll, null, comparison, defectcode, unit, null, skip, ismes);
                                break;

                            default:
                                limit = new TF_Limit(step.Name, hl, ll, comparison, defectcode, unit, null, skip, ismes);
                                break;
                        }
                    }
                    else
                    {
                        limit = new TF_Limit(step.Name, hl, ll, Comparison.LOG, defectcode, unit, null, skip, ismes);
                    }

                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    if (step.IsSequenceCall)
                    {
                        var seq = SequenceFile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(seq, n_Limit, ismes);
                    }
                    break;

                case "StringValueTest":
                    match = DefectFormat.Match(comment);
                    if (match.Success)
                    {
                        defectcode = match.Groups[1].Value;
                    }

                    var is_expr = step.AsPropertyObject().GetValBoolean("Limits.UseStringExpr", 0);

                    if (is_expr)
                    {
                        var ll_str = step.AsPropertyObject().GetValString("Limits.StringExpr", 0);
                        limit = new TF_Limit(step.Name, ll_str, Comparison.LOG, defectcode, null, null, skip, ismes);
                    }
                    else
                    {
                        var ll_str = step.AsPropertyObject().GetValString("Limits.String", 0);
                        limit = new TF_Limit(step.Name, ll_str, Comparison.MATCH, defectcode, null, null, skip, ismes);
                    }

                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    if (step.IsSequenceCall)
                    {
                        var seq = SequenceFile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(seq, n_Limit, ismes);
                    }
                    break;

                case "NI_MultipleNumericLimitTest":
                    //match = DefectFormat.Match(comment);
                    //string prefix = null;
                    //int serial = 0;
                    string subdefect = null;
                    //if (match.Success)
                    //{
                    //    defectcode = match.Groups[1].Value;
                    //    var submatch = Regex.Match(defectcode, @"^(\D+)(\d+)$");
                    //    prefix = submatch.Groups[1].Value;
                    //    serial = int.Parse(submatch.Groups[2].Value);
                    //}

                    var meas_array = step.AsPropertyObject().GetValVariant("Result.Measurement", 0) as Array;

                    limit = new TF_Limit(step.Name);
                    n_Limit = new Nest<TF_Limit>() { Element = limit };

                    foreach (PropertyObject meas in meas_array)
                    {
                        var hl_multi = meas.GetValNumber("Limits.High", 0);
                        var ll_multi = meas.GetValNumber("Limits.Low", 0);
                        var comp_multi = meas.GetValString("Comp", 0);
                        var unit_multi = meas.GetValString("Units", 0);

                        //if (prefix != null)
                        //{
                        //    subdefect = $"{prefix}{serial}";
                        //    serial++;
                        //}

                        if (Enum.TryParse(comp_multi, out Comparison comparison_0))
                        {
                            switch (comparison_0)
                            {
                                case Comparison.LE:
                                case Comparison.LT:
                                    var limit_multi = new TF_Limit(meas.Name, ll_multi, null, comparison_0, subdefect, unit_multi, null, skip, ismes);
                                    n_Limit.Add(limit_multi);
                                    break;

                                default:
                                    var limit_multi0 = new TF_Limit(meas.Name, hl_multi, ll_multi, comparison_0, subdefect, unit_multi, null, skip, ismes);
                                    n_Limit.Add(limit_multi0);
                                    break;
                            }
                        }
                        else
                        {
                            var limit_multi = new TF_Limit(meas.Name, hl_multi, ll_multi, Comparison.LOG, subdefect, unit_multi, null, skip, ismes);
                            n_Limit.Add(limit_multi);
                        }

                        // add duplication detected if necessary
                    }

                    if (step.IsSequenceCall)
                    {
                        var seq = SequenceFile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(seq, n_Limit, ismes);
                    }
                    break;

                case "SequenceCall":
                    limit = new TF_Limit(step.Name);

                    n_Limit = new Nest<TF_Limit>() { Element = limit };
                    if (step.IsSequenceCall)
                    {
                        var seq = SequenceFile.GetSequenceByName(step.Module.AsPropertyObject().GetValString("SeqName", 0));

                        SeqToData(seq, n_Limit, ismes);
                    }
                    break;
            }

            return n_Limit;
        }

        /// <summary>
        /// Analysis Test Report.
        /// </summary>
        /// <param name="step"></param>
        /// <param name="stepdatas"></param>
        private void StepRecordToResult(PropertyObject step, Nest<TF_StepData> stepdatas)
        {
            try
            {
                var steptype = step.GetValString("TS.StepType", 0);
                var stepname = step.GetValString("TS.StepName", 0);
                var stepstatus = step.GetValString("Status", 0);

                Nest<TF_StepData> stepdata = null;
                switch (steptype)
                {
                    case "PassFailTest":
                        stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                        if (stepstatus == "Skipped")
                        {
                            stepdata.Element.Result = TF_ItemStatus.NotTested;
                        }
                        else
                        {
                            stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                            stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));
                            if (stepdata.Element is TF_ItemData itemdata)
                            {
                                var rs_boolean = step.GetValBoolean("PassFail", 0);
                                itemdata.Value = rs_boolean ? 1 : 0;

                                if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                                {
                                    stepdata.Element.Result = itemstatus;
                                }
                            }

                            if (step.Exists("TS.SequenceCall.ResultList", 0))
                            {
                                var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                foreach (var substepobj in substeps)
                                {
                                    if (substepobj is PropertyObject substep)
                                    {
                                        StepRecordToResult(substep, stepdata);
                                    }
                                }
                            }
                        }

                        break;

                    case "NumericLimitTest":
                        stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                        if (stepstatus == "Skipped")
                        {
                            stepdata.Element.Result = TF_ItemStatus.NotTested;
                        }
                        else
                        {
                            stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                            stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));
                            if (stepdata.Element is TF_ItemData itemdata)
                            {
                                itemdata.Value = step.GetValNumber("Numeric", 0);

                                if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                                {
                                    stepdata.Element.Result = itemstatus;
                                }
                            }

                            if (step.Exists("TS.SequenceCall.ResultList", 0))
                            {
                                var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                foreach (var substepobj in substeps)
                                {
                                    if (substepobj is PropertyObject substep)
                                    {
                                        StepRecordToResult(substep, stepdata);
                                    }
                                }
                            }
                        }

                        break;
                    case "StringValueTest":
                        stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                        if (stepstatus == "Skipped")
                        {
                            stepdata.Element.Result = TF_ItemStatus.NotTested;
                        }
                        else
                        {
                            stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                            stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));
                            if (stepdata.Element is TF_ItemData itemdata)
                            {
                                itemdata.Value = step.GetValString("String", 0);

                                if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                                {
                                    stepdata.Element.Result = itemstatus;
                                }
                            }

                            if (step.Exists("TS.SequenceCall.ResultList", 0))
                            {
                                var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                foreach (var substepobj in substeps)
                                {
                                    if (substepobj is PropertyObject substep)
                                    {
                                        StepRecordToResult(substep, stepdata);
                                    }
                                }
                            }
                        }
                        break;
                    case "NI_MultipleNumericLimitTest":
                        stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                        if (stepstatus == "Skipped")
                        {
                            stepdata.Element.Result = TF_ItemStatus.NotTested;
                        }
                        else
                        {
                            stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                            stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));

                            var measures = step.GetValVariant("Measurement", 0) as object[];
                            foreach (var measobj in measures)
                            {
                                if (measobj is PropertyObject meas)
                                {
                                    if (stepdata.FirstOrDefault(x => x.Element.Name == meas.Name)?.Element is TF_ItemData subitem)
                                    {
                                        subitem.StartTime = stepdata.Element.StartTime;
                                        subitem.EndTime = stepdata.Element.EndTime;
                                        subitem.Value = meas.GetValNumber("Data", 0);

                                        var substatus = meas.GetValString("Status", 0);

                                        if (Enum.TryParse(substatus, out TF_ItemStatus subitemstatus))
                                        {
                                            subitem.Result = subitemstatus;
                                        }
                                    }
                                }
                            }

                            if (step.Exists("TS.SequenceCall.ResultList", 0))
                            {
                                var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                foreach (var substepobj in substeps)
                                {
                                    if (substepobj is PropertyObject substep)
                                    {
                                        StepRecordToResult(substep, stepdata);
                                    }
                                }
                            }

                            if (stepdata.Element is TF_ItemData itemdata)
                            {
                                if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                                {
                                    stepdata.Element.Result = itemstatus;
                                }
                            }
                        }

                        break;
                    case "SequenceCall":
                    case "NI_Wait":
                        stepdata = stepdatas.FirstOrDefault(x => x.Element.Name == stepname);
                        if (stepdata != null)
                        {
                            if (stepstatus == "Skipped")
                            {
                                stepdata.Element.Result = TF_ItemStatus.NotTested;
                            }
                            else
                            {
                                stepdata.Element.StartTime = TS_ZeroTime.AddSeconds(step.GetValNumber("TS.StartTime", 0));
                                stepdata.Element.EndTime = stepdata.Element.StartTime.AddSeconds(step.GetValNumber("TS.TotalTime", 0));

                                if (stepdata.Element is TF_ItemData itemdata)
                                {
                                    if (Enum.TryParse(stepstatus, out TF_ItemStatus itemstatus))
                                    {
                                        stepdata.Element.Result = itemstatus;
                                    }
                                }

                                try
                                {
                                    var substeps = step.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                    foreach (var substepobj in substeps)
                                    {
                                        if (substepobj is PropertyObject substep)
                                        {
                                            StepRecordToResult(substep, stepdata);
                                        }
                                    }
                                }
                                catch (System.Runtime.InteropServices.COMException)
                                {
                                    // Not a data collection
                                }
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Warn(ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// trigged if there is confliction. the message is confliction content
        /// </exception>
        /// <returns></returns>
        public override int FormatScript(out string formatlog)
        {
            if (SequenceFileFormated)
            {
                formatlog = "Already formated";
                return 1;
            }

            StringBuilder log = new StringBuilder();
            log?.AppendLine("======== TYM TestStand Format Result ========");

            int rtn = 1;

            var analysis = StaticAnalyzeSeqFile(SequenceFile);

            int count = DefectEnd - DefectStart;
            var tempnos = new int[count];

            for (int i = 0; i < count; i++)
            {
                tempnos[i] = DefectStart + i;
            }

            var listnos = tempnos.ToList();

            foreach (var ana_seq in analysis)
            {
                foreach (var ana_step in ana_seq.Value)
                {
                    if (ana_step.Limit?.Defect is string defect)
                    {
                        var match = Regex.Match(defect, @"(\d+)$");
                        if (match.Success)
                        {
                            if (int.TryParse(match.Groups[1].Value, out int no))
                            {
                                if (!listnos.Remove(no))
                                {
                                    Warn($"Duplicated Defect Code Detected: {no}");
                                }
                            }
                        }
                    }
                }
            }

            int idx_defect = 0;
            foreach (var ana_seq in analysis)
            {
                bool ismain = ana_seq.Key == "MainSequence";
                int duplicated_idx = 1;
                foreach (var ana_step in ana_seq.Value)
                {
                    if (ana_step.Step is Step step)
                    {
                        if (ana_step.StepErrors.ContainsKey(StepFormatError.NoDefectCode))
                        {

                        }

                        foreach (var error in ana_step.StepErrors)
                        {
                            switch (error.Key)
                            {
                                case StepFormatError.IllegalStepName:
                                    log?.Append($"{error.Key}: {ana_seq.Key}.{step.Name}");
                                    step.Name = step.Name.Replace(",", "_").Replace(";", "_").Replace("\"", "_");
                                    rtn = 0;
                                    log?.AppendLine($" --> {step.Name}");
                                    break;

                                case StepFormatError.NoDefectCode:
                                    if (ismain)
                                    {
                                        ana_step.Limit.Defect = $"{GlobalConfiguration.Default.General.Prefix_DefectCode}{listnos[idx_defect]}";
                                        idx_defect++;

                                        if (string.IsNullOrEmpty(step.AsPropertyObject().Comment))
                                        {
                                            step.AsPropertyObject().Comment = $"@{ana_step.Limit.Defect}";
                                        }
                                        else
                                        {
                                            step.AsPropertyObject().Comment = $"@{ana_step.Limit.Defect}\r\n{step.AsPropertyObject().Comment}";
                                        }

                                        log?.AppendLine($"{error.Key}: {ana_seq.Key}.{step.Name} --> {ana_step.Limit.Defect}");

                                        rtn = 0;
                                    }
                                    break;

                                case StepFormatError.DuplicatedItemName:
                                    log?.Append($"{error.Key}: {ana_seq.Key}.{step.Name}");
                                    step.Name = $"{step.Name}_{duplicated_idx}";
                                    duplicated_idx++;
                                    log?.AppendLine($" --> {step.Name}");
                                    rtn = 0;
                                    break;

                                default:
                                    log?.AppendLine($"{error.Key}: {ana_seq.Key}.{step.Name}. Cannot be resolved");
                                    break;
                            }
                        }
                    }
                }
            }
            SequenceFileFormated = true;

            formatlog = log.ToString();
            Info(formatlog);
            return rtn;
        }

        private Dictionary<string, List<StepAnalysisResult>> StaticAnalyzeSeqFile(SequenceFile seqfile)
        {
            Dictionary<string, List<StepAnalysisResult>> analysis = new Dictionary<string, List<StepAnalysisResult>>();

            for (int i = 0; i < SequenceFile.NumSequences; i++)
            {
                var seq = seqfile.GetSequence(i);
                var analysislist = StaticAnalyzeSeq(seq);

                analysis.Add(seq.Name, analysislist);
            }
            return analysis;
        }

        private List<StepAnalysisResult> StaticAnalyzeSeq(NationalInstruments.TestStand.Interop.API.Sequence seq)
        {
            List<StepAnalysisResult> localanalysis = new List<StepAnalysisResult>();

            var cnt = seq.GetNumSteps(StepGroups.StepGroup_Setup);

            bool inmain = seq.Name == "MainSequence";

            for (var i = 0; i < cnt; i++)
            {
                var step = seq.GetStep(i, StepGroups.StepGroup_Setup);
                localanalysis.Add(StaticAnalyzeStep(step, localanalysis, inmain));
            }

            cnt = seq.GetNumSteps(StepGroups.StepGroup_Main);

            for (var i = 0; i < cnt; i++)
            {
                var step = seq.GetStep(i, StepGroups.StepGroup_Main);
                localanalysis.Add(StaticAnalyzeStep(step, localanalysis, inmain));
            }

            cnt = seq.GetNumSteps(StepGroups.StepGroup_Cleanup);

            for (var i = 0; i < cnt; i++)
            {
                var step = seq.GetStep(i, StepGroups.StepGroup_Cleanup);
                localanalysis.Add(StaticAnalyzeStep(step, localanalysis, inmain));
            }

            return localanalysis;
        }

        private StepAnalysisResult StaticAnalyzeStep(Step step, IReadOnlyCollection<StepAnalysisResult> analysis, bool inmain = true)
        {
            var type = step.StepType.Name;
            string defectcode = null;

            string comment = step.AsPropertyObject().Comment;
            string skipstr = step.GetRunModeEx();
            bool skip = skipstr == "Skip";

            StepAnalysisResult stepanalysis = new StepAnalysisResult()
            {
                StepName = step.Name,
                Step = step,
            };

            switch (type)
            {
                case "PassFailTest":
                case "NumericLimitTest":
                case "StringValueTest":
                    if (inmain)
                    {
                        var match = DefectFormat.Match(comment);
                        if (match.Success)
                        {
                            defectcode = match.Groups[1].Value;
                        }
                        else
                        {
                            stepanalysis.StepErrors.Add(StepFormatError.NoDefectCode, $"");
                        }
                    }

                    if (analysis.FirstOrDefault(x => x.StepName == step.Name) != null)
                    {
                        stepanalysis.StepErrors.Add(StepFormatError.DuplicatedItemName, $"");
                    }

                    if (!string.IsNullOrEmpty(step.Precondition))
                    {
                        stepanalysis.StepErrors.Add(StepFormatError.ConditionalItem, $"");
                    }

                    stepanalysis.Limit = new TF_Limit(step.Name, skip);
                    stepanalysis.Limit.Defect = defectcode;
                    break;

                case "NI_MultipleNumericLimitTest":  // Treate Multiple Numeric Test defect code as Sub Sequence Call
                    if (analysis.FirstOrDefault(x => x.StepName == step.Name) != null)
                    {
                        stepanalysis.StepErrors.Add(StepFormatError.DuplicatedItemName, $"");
                    }

                    if (!string.IsNullOrEmpty(step.Precondition))
                    {
                        stepanalysis.StepErrors.Add(StepFormatError.ConditionalItem, $"");
                    }

                    stepanalysis.Limit = new TF_Limit(step.Name, skip);
                    stepanalysis.Limit.Defect = defectcode;
                    break;

                default:
                    if (step.StatusExpression.ToLower().Contains("fail"))
                    {
                        stepanalysis.StepErrors.Add(StepFormatError.ActionWithJudgement, $"");
                    }

                    //if (step.IsSequenceCall)  // Do not verify the sequence call since it hard to detect if there would be a test item in it
                    //{
                    //    if (analysis.FirstOrDefault(x => x.StepName == step.Name) != null)
                    //    {
                    //        stepanalysis.StepErrors.Add(StepFormatError.DuplicatedItemName, $"");
                    //    }

                    //    stepanalysis.Limit = new TF_Limit(step.Name, skip);
                    //    stepanalysis.Limit.Defect = defectcode;
                    //}
                    break;
            }

            if (step.Name.Contains(",") || step.Name.Contains(";") || step.Name.Contains("\""))
            {
                stepanalysis.StepErrors.Add(StepFormatError.IllegalStepName, $"");
            }

            return stepanalysis;
        }

        private static void GetDefectList(Nest<TF_Limit> limits, Dictionary<string, string> dict, Stack<TF_Limit> callstack)
        {
            callstack.Push(limits.Element);

            var name = string.Join("|", callstack.Select(x => x.Name));
            if (dict.ContainsKey(name))
            {
            }

            dict.Add(name, limits.Element.Defect);

            foreach (var sub in limits)
            {
                GetDefectList(sub, dict, callstack);
            }
        }

        static string[] ErrorCodeSeparators = { "\r", "\n", "," };
        static Regex RE_ErrorCode = new Regex(@"(\w{2})(\w{2}\w{2}\w{1})");

        private NationalInstruments.TestStand.Interop.API.Sequence FetchOrCreateSequence(SequenceFile file, string name)
        {
            NationalInstruments.TestStand.Interop.API.Sequence seq = null;
            if (file.SequenceNameExists(name))
            {
                seq = file.GetSequenceByName(name);
            }
            else
            {
                seq = TS_AppMgr.GetEngine().NewSequence();

                seq.Name = name;
                file.InsertSequenceEx(file.NumSequences, seq);
            }
            return seq;
        }

        public override int StartExecution()
        {
            if (SequenceFile != null)
            {
                if (Execution != null)
                {
                    if (MessageBox.Show("Already Run a Execution, Do you want to Terminate Current One and START a new one", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        FinishTest(-1);

                        TerminateAll();
                        TS_AppMgr.CloseAllExecutions();

                        if (SlotCtrls != null)
                        {
                            foreach (var ctrl in SlotCtrls)
                            {
                                ctrl.Dispose();
                            }
                        }

                        if (SlotBlockQueues != null)
                        {
                            foreach (var queue in SlotBlockQueues)
                            {
                                try
                                {
                                    queue.Close();
                                    queue.Dispose();
                                }
                                catch
                                { }
                            }
                        }

                        if (SlotReportQueues != null)
                        {
                            foreach (var queue in SlotReportQueues)
                            {
                                try
                                {
                                    queue.Clear();
                                }
                                catch
                                { }
                            }
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }

                Results = null;
                IsSequentialModel = IsBatchModel = IsParallelModel = false;
                //Task<ScriptAnalysisResult> task_template = Task.Run(() => AnalyzeScript());
                ResultTemplate = AnalyzeScript().ResultTemplate;

                ResultTemplate.Operator = UserName;

                ResultTemplate.TestGuiVersion = Application.ProductVersion;
                ResultTemplate.TestSoftwareVersion = SequenceFile.AsPropertyObjectFile().Version;

                if (ToolboxService?.Station != null)
                {
                    var cu = ToolboxService.Customers?.FirstOrDefault(x => x.Name == GlobalConfiguration.Default.Station.CustomerName);
                    var prj = cu?.FirstOrDefault(x => x.Name == GlobalConfiguration.Default.Station.ProjectName);
                    var prd = prj?.FirstOrDefault(x => x.Name == GlobalConfiguration.Default.Station.ProductName);
                    var sts = prd?.FirstOrDefault(x => x.Name == GlobalConfiguration.Default.Station.StationName);

                    if (prd != null && sts is null)
                    {
                        var msg = $"当前设置{GlobalConfiguration.Default.Station.CustomerName}->{GlobalConfiguration.Default.Station.ProjectName}->{GlobalConfiguration.Default.Station.ProductName}->{GlobalConfiguration.Default.Station.StationName}与Toolbox中设置{ToolboxService.Customer?.Name}->{ToolboxService.Project?.Name}->{ToolboxService.Product?.Name}->{ToolboxService.Station?.Name}不符，请联系工程师核对确认。点击确认将以Toolbox中设置为准进行测试";
                        if (MessageBox.Show(msg, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
                        {
                            throw new InvalidOperationException(msg);
                        }
                        else
                        {
                            ResultTemplate.StationConfig = new StationConfig(ToolboxService.Root.Name, ToolboxService.Customer.Name, ToolboxService.Project.Name, ToolboxService.Product.Name, ToolboxService.Station.Name, ToolboxService.Station.StationId);
                            ResultTemplate.StationConfig.Location = GlobalConfiguration.Default.Station.Location;
                        }
                    }
                }

                try
                {
                    Execution = TS_SeqFileViewMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_ExecutionEntryPoints_Set, 0).EntryPoint.Run();
                    Info($"Start Execution {Execution.DisplayName}");
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    // For TYM_Batch. to be compatible with default Batch, renamed the Test UUTs, which may trig this issue.
                    Warn(ex);
                    MessageBox.Show($"Start Module Failed, Need to RESTART Toucan. Err {ex.Message}", "Error", MessageBoxButtons.OK);
                    return -1;
                }

                //if (GlobalConfiguration.Default.General.RunMode == RunMode.Sequential || GlobalConfiguration.Default.General.RunMode == RunMode.Default_Sequential)
                //{
                //    SlotCount = 1;
                //}
                //else
                //{
                //    SlotCount = GlobalConfiguration.Default.General.SocketCount;
                //}

                SlotCount = GlobalConfiguration.Default.General.SocketCount;

                Info(GlobalConfiguration.Default.General.SocketCount);

                GC.Collect();

                if (UserName is null)
                {
                    UserName = "TYM_USER";
                }

                //task_template.Wait();

                //if (!task_template.IsCompleted)
                //{
                //    MessageBox.Show($"Analyze Sequence {SequenceFile.Path} Failed.", "Error", MessageBoxButtons.OK);
                //    StopExecution();
                //    return -1;
                //}

                //ResultTemplate = task_template.Result.ResultTemplate;

                var modulefile = GetModulePath();
                if (!IsOriginalModel)
                {
                    Task SfcInitTask = null;
                    if (ResultTemplate.SFCsConfig.EnableSfc)
                    {
                        SfcInitTask = Task.Run(() =>
                        {
                            MesInstance = Mes.MesManager.GetMesInstance(ResultTemplate.StationConfig.Location, ResultTemplate.StationConfig.Vendor);
                            if (ResultTemplate.SFCsConfig.SfcsUploadData)
                            {
                                string data = null;
                                if (ResultTemplate.SFCsConfig.SfcsDataMode.Equals("JDM", StringComparison.OrdinalIgnoreCase))
                                {
                                    ResultTemplate.GenerateSfcHeader_JDM(out data);
                                }
                                else
                                {
                                    ResultTemplate.GenerateSFCHeader(out data);
                                }

                                MesInstance.Initialize(ResultTemplate.SFCsConfig, data);
                                Info($"SFCs_Column: {data}");
                            }
                            else
                            {
                                MesInstance.Initialize(ResultTemplate.SFCsConfig, string.Empty);
                            }
                        }
                        );
                    }

                    //if (GlobalConfiguration.Default.General.RunMode == RunMode.Batch)
                    //{
                    //    if (!modulefile.Contains("Batch"))
                    //    {
                    //        throw new InvalidProgramException($"System.xml is in batch model. TestStand setting is {modulefile}");
                    //    }
                    //}
                    //else if (GlobalConfiguration.Default.General.RunMode == RunMode.Parallel)
                    //{
                    //    if (!modulefile.Contains("Parallel"))
                    //    {
                    //        throw new InvalidProgramException($"System.xml is in parallel model. TestStand setting is {modulefile}");
                    //    }
                    //}
                    //else 
                    if (modulefile.Contains("TYM"))
                    {
                        Warn($"TYM Model detected. {modulefile}");
                        throw new InvalidProgramException($"V0R2 Model Detected. Please Close Toucan and then set appropiate model in TestStand. Current is {modulefile}");
                    }

                    CustomizeInputSn = GlobalConfiguration.Default.General.CustomizeInputSn;
                    try
                    {
                        // Add Default Model Setting
                        NationalInstruments.TestStand.Interop.API.Sequence seq = null;
                        Step step = null;

                        seq = FetchOrCreateSequence(SequenceFile, "ModelOptions");
                        step = TS_AppMgr.GetEngine().NewStep("None Adapter", "Statement");
                        step.Name = "TYM Injection in ModelOptions";
                        step.PostExpression = $"Parameters.ModelOptions.ParallelModel_ShowUUTDlg = False,Parameters.ModelOptions.BringUUTDlgToFrontOnChange = False, Parameters.ModelOptions.NumTestSockets={SlotCount}";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        seq = FetchOrCreateSequence(SequenceFile, "ReportOptions");
                        var rawpath = GlobalConfiguration.Default.General.Raw_ReportPath.Replace("\\", "\\\\\\\\");
                        step = TS_AppMgr.GetEngine().NewStep("None Adapter", "Statement");
                        step.Name = "TYM Injection in ReportOptions";
                        step.PostExpression = $"Parameters.ReportOptions.Format = \"xml\",Parameters.ReportOptions.IncludeArrayMeasurement = 1,Parameters.ReportOptions.NewFileNameForEachUUT = True,Parameters.ReportOptions.NewFileNameForEachTestSocket=True,Parameters.ReportOptions.StoreUUTReportWithBatchReport=False,Parameters.ReportOptions.DirectoryType=\"SpecifyByExpression\",Parameters.ReportOptions.ReportFileBatchModelExpression=Parameters.ReportOptions.ReportFileSequentialModelExpression=Parameters.ReportOptions.ReportFileParallelModelExpression=\"\\\"{rawpath}\\\\\\\\$(UUTPartNum)\\\\\\\\$(FileYear) $(FileMonth) $(FileDay)\\\\\\\\$(UUTStatus)\\\\\\\\$(UUT)_{ResultTemplate.StationConfig.CustomerName}_{ResultTemplate.StationConfig.ProjectName}_{ResultTemplate.StationConfig.StationName}_$(FileYear) $(FileMonth) $(FileDay)_$(FileTime)_$(StationID)_$(TestSocket)_$(UUTStatus).$(FileExtension)\\\"\"";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        seq = FetchOrCreateSequence(SequenceFile, "PreUUTLoop");
                        step = TS_AppMgr.GetEngine().NewStep("None Adapter", "Statement");
                        step.Name = "TYM Injection in PreUUTLoop";
                        step.PostExpression = $"Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.SequentialShowSerialNumber=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.SequentialShowStatus=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.ParallelModelUUTInfoDialog=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.BatchModelShowStatus=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.BatchModelGetNextUUTs=False";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        seq = FetchOrCreateSequence(SequenceFile, "PostMainSequence");
                        step = TS_AppMgr.GetEngine().NewStep("None Adapter", "Statement");
                        step.Name = "TYM Update Defect Code";
                        step.PostExpression = $"Runstate.Thread.PostUIMessageEx(UIMsg_UserMessageBase + 10, RunState.TestSockets.MyIndex, Parameters.UUTStatus, ThisContext, True)";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        if (!CustomizeInputSn)
                        {
                            if (modulefile.Contains("Batch"))
                            //if (GlobalConfiguration.Default.General.RunMode == RunMode.Batch)
                            {
                                seq = FetchOrCreateSequence(SequenceFile, "PreBatch");

                                step = TS_AppMgr.GetEngine().NewStep("None Adapter", "CallExecutable");
                                step.Name = "TYM Injection in PreBatch_Construct Queue";
                                var queuefile = Path.Combine(AppContext.BaseDirectory, "Bin", "QueueClient.exe");
                                step.AsPropertyObject().SetValString("Executable", 0, queuefile);
                                step.AsPropertyObject().SetValString("ExecutableCalled", 0, queuefile);
                                step.AsPropertyObject().SetValString("Arguments", 0, "\"Batch\"");
                                step.AsPropertyObject().SetValString("InitialWindowState", 0, "WINSTATE_HIDDEN");
                                step.IgnoreRTE = true;

                                seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);
                            }
                            else
                            {
                                seq = FetchOrCreateSequence(SequenceFile, "PreUUT");

                                step = TS_AppMgr.GetEngine().NewStep("None Adapter", "CallExecutable");
                                step.Name = "TYM Injection in PreUUT_Construct Queue";
                                var queuefile = Path.Combine(AppContext.BaseDirectory, "Bin", "QueueClient.exe");
                                step.AsPropertyObject().SetValString("Executable", 0, queuefile);
                                step.AsPropertyObject().SetValString("ExecutableCalled", 0, queuefile);
                                step.AsPropertyObject().SetValString("Arguments", 0, "Str(Parameters.UUT.TestSocketIndex)");
                                step.AsPropertyObject().SetValString("InitialWindowState", 0, "WINSTATE_HIDDEN");
                                step.IgnoreRTE = true;

                                seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);
                            }
                        }
                        else
                        {
                            if (modulefile.Contains("Batch"))
                            //if (GlobalConfiguration.Default.General.RunMode == RunMode.Batch)
                            {
                                seq = FetchOrCreateSequence(SequenceFile, "PreBatch");
                            }
                            else
                            {
                                seq = FetchOrCreateSequence(SequenceFile, "PreUUT");
                            }
                        }

                        if (seq.Parameters.Exists("UUT.AdditionalData.Attach", 0))
                        {
                            var props = seq.Parameters.GetSubProperties("UUT.AdditionalData.Attach", 0);

                            foreach (var prop in props)
                            {
                                if (prop.Type.ValueType == PropertyValueTypes.PropValType_String)
                                {
                                    ResultTemplate.AttachProperties.Add(prop.Name, null);
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(ResultTemplate.StationConfig.StationID))
                        {
                            ResultTemplate.StationConfig.StationID = "01";
                        }

                        TS_AppMgr.GetEngine().StationOptions.StationID = TS_AppMgr.GetEngine().StationID = ResultTemplate.StationConfig.StationID;
                    }
                    catch
                    {
                    }

                    if (ResultTemplate.SFCsConfig.EnableSfc)
                    {
                        try
                        {
                            SfcInitTask.Wait(5000);
                            if (!SfcInitTask.IsCompleted)
                            {
                                throw new InvalidOperationException($"Initialize SFCs Failed. Site {ResultTemplate.StationConfig.Location}. Product {ResultTemplate.SFCsConfig.Product}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Warn(ex);
                            throw ex;
                        }
                    }
                }

                Info($"Load Seq {SequenceFile.Path}. IsOriginalModel: {IsOriginalModel}, Model Path: {modulefile}");

                IsRunning = true;

                SetUserName(UserName);
            }
            return 1;
        }

        public override int StopExecution()
        {
            Info("StopExecution");
            Execution?.Terminate();

            TS_SeqFileViewMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_CloseCompletedExecutions, 0);
            TS_AppMgr.CloseAllExecutions();
            TS_AppMgr.GetEngine().UnloadAllModules();
            IsRunning = false;
            Execution = null;
            return 0;
        }

        public override int SetModulePath(string path)
        {
            if (!IsInitialized) return 0;

            if (File.Exists(path))
            {
                TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath = path;
                return 1;
            }
            else
            {
                throw new FileNotFoundException($"Model {path} not exist");
            }
        }

        public override int SaveScriptAs(string dest)
        {
            if (string.IsNullOrEmpty(dest))
            { }

            SequenceFile?.Save(dest);

            return 1;
        }

        public override int SetModulePath(RunMode mode, string path)
        {
            if (!IsInitialized) return 0;

            var ts_majorver = TS_AppMgr.GetEngine().MajorVersion;
            TS_AppMgr.GetEngine().UnloadAllModules();
            switch (mode)
            {
                case RunMode.Normal:
                case RunMode.Sequential:
                case RunMode.Default_Sequential:
                    TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath = Path.Combine(TS_AppMgr.GetEngine().GetTestStandPath(TestStandPaths.TestStandPath_NIComponents), "Models\\TestStandModels\\SequentialModel.seq");
                    break;
                case RunMode.Parallel:
                case RunMode.Default_Parallel:
                    TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath = Path.Combine(TS_AppMgr.GetEngine().GetTestStandPath(TestStandPaths.TestStandPath_NIComponents), "Models\\TestStandModels\\ParallelModel.seq");
                    break;

                case RunMode.Default_Batch:
                    TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath = Path.Combine(TS_AppMgr.GetEngine().GetTestStandPath(TestStandPaths.TestStandPath_NIComponents), "Models\\TestStandModels\\BatchModel.seq");
                    break;

                case RunMode.Customized:
                    try
                    {
                        TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath = path;
                    }
                    catch { }
                    break;
            }

            Info(string.Format("Module Path: {0}", TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath));
            return 1;
        }

        public override string GetModulePath()
        {
            return TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath;
        }


        public override int ResumeAll()
        {
            TS_AppMgr?.Executions.ResumeAll();
            return 1;
        }

        public override int TerminateAll()
        {
            TS_AppMgr?.GetEngine().TerminateAll();
            return 1;
        }

        public override int AbortAll()
        {
            TS_AppMgr?.GetEngine().AbortAll();
            return 1;
        }

        //public void CreateSequence()
        //{
        //    NationalInstruments.TestStand.Interop.API.Step step = TS_AppMgr.GetEngine().NewStep(NationalInstruments.TestStand.Interop.API.AdapterKeyNames.DotNetAdapterKeyname, NationalInstruments.TestStand.Interop.API.StepTypes.StepType_Action);

        //    step.Name = "StepName Example";
        //    step.AsPropertyObject().Comment = "";
        //    string propname = "";
        //    string propvalue = "";
        //    step.AsPropertyObject().EvaluateEx(string.Format("\"{0}={1}\"", propname, propvalue), 0);

        //    TS_AppMgr.Executions[0].GetSequenceFile().GetSequence(0).InsertStep(step, 0, NationalInstruments.TestStand.Interop.API.StepGroups.StepGroup_Main);
        //}

        #region Event

        private void TS_AppMgr_StartExecution(object sender, _ApplicationMgrEvents_StartExecutionEvent e)
        {
            SequenceContext sequence = e.thrd.GetSequenceContext(0, out int frameid);

            Info(sequence.CallStackName);

            try
            {
                string modeltype = string.Empty;

                try
                {
                    modeltype = sequence.Locals.GetValString("ModelData.ModelType", 0);

                    if (modeltype == "Sequential")
                    {
                        IsSequentialModel = true;

                        SlotCount = 1;
                        Results = new TF_Result[SlotCount];
                        SlotCtrls = InternalSlotCtrls = new SlotInfo_TS[SlotCount];
                        SlotSequenceContexts = new SequenceContext[SlotCount];
                        SlotBlockQueues = new NamedPipeQueueServer[SlotCount];
                        SlotReportQueues = new Queue<TF_Result>[SlotCount];
                        for (int i = 0; i < SlotCount; i++)
                        {
                            Results[i] = ResultTemplate.Clone() as TF_Result;
                            Results[i].SocketIndex = i;
                            Results[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                            SlotCtrls[i] = InternalSlotCtrls[i] = new SlotInfo_TS(Results[i])
                            {
                                IsSequentialModel = true
                            };
                            SlotCtrls[i].Index = i;
                            SlotBlockQueues[i] = new NamedPipeQueueServer($"-1");
                            SlotBlockQueues[i].Initialize();
                            SlotReportQueues[i] = new Queue<TF_Result>();
                        }

                        //InternalSlotCtrls[0].TS_Execution = e.exec;
                        InternalSlotCtrls[0].InitialExecution(e.exec);
                        InternalSlotCtrls[0].TS_SequenceContext = sequence;
                        SlotSequenceContexts[0] = sequence;

                        ActionOnExecutionInitialized();
                    }
                    else if (modeltype == "Batch")
                    {
                        IsBatchModel = true;
                        HostSequenceContext = sequence;
                    }
                    else if (modeltype == "Parallel")
                    {
                        IsParallelModel = true;
                        HostSequenceContext = sequence;
                    }
                }
                catch
                {
                    int slotIndex = -1;
                    try
                    {
                        slotIndex = (int)sequence.Parameters.GetValNumber("TestSocket.Index", 0);
                    }
                    catch
                    {
                    }
                    // Only Main Execution contains locals.metadata
                    if (IsBatchModel)
                    {
                        if (slotIndex == 0)
                        {
                            SlotCount = (int)sequence.Parameters.GetValNumber("ModelData.ModelOptions.NumTestSockets", 0);
                            Info($"ModelOptions.NumTestSockets {SlotCount}");

                            Results = new TF_Result[SlotCount];
                            SlotCtrls = InternalSlotCtrls = new SlotInfo_TS[SlotCount];
                            SlotSequenceContexts = new SequenceContext[SlotCount];
                            SlotBlockQueues = new NamedPipeQueueServer[1];
                            SlotReportQueues = new Queue<TF_Result>[SlotCount];
                            for (int i = 0; i < SlotCount; i++)
                            {
                                Results[i] = ResultTemplate.Clone() as TF_Result;
                                Results[i].SocketIndex = i;
                                Results[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                                SlotCtrls[i] = InternalSlotCtrls[i] = new SlotInfo_TS(Results[i])
                                {
                                    IsBatchModel = true,
                                };
                                SlotCtrls[i].Index = i;
                                SlotReportQueues[i] = new Queue<TF_Result>();
                            }
                            SlotBlockQueues[0] = new NamedPipeQueueServer($"Batch");
                            SlotBlockQueues[0].Initialize();

                            ActionOnExecutionInitialized();
                        }

                        var cnt = TS_AppMgr.Executions.Count;

                        if (cnt > 1)
                        {
                            if (TS_AppMgr.Executions[1].DisplayName.Contains("SequenceFileLoad"))
                            {
                                if (cnt > 2)
                                {
                                    //InternalSlotCtrls[cnt - 3].TS_Execution = TS_AppMgr.Executions[cnt - 1];
                                    InternalSlotCtrls[cnt - 3].InitialExecution(TS_AppMgr.Executions[cnt - 1]);
                                    InternalSlotCtrls[cnt - 3].TS_SequenceContext = sequence;
                                    SlotSequenceContexts[cnt - 3] = sequence;
                                }
                            }
                            else
                            {
                                //InternalSlotCtrls[cnt - 2].TS_Execution = TS_AppMgr.Executions[cnt - 1];
                                InternalSlotCtrls[cnt - 2].InitialExecution(TS_AppMgr.Executions[cnt - 1]);
                                InternalSlotCtrls[cnt - 2].TS_SequenceContext = sequence;
                                SlotSequenceContexts[cnt - 2] = sequence;
                            }
                        }
                    }
                    else if (IsParallelModel)
                    {
                        if (slotIndex == 0)
                        {
                            SlotCount = (int)sequence.Parameters.GetValNumber("ModelData.ModelOptions.NumTestSockets", 0);
                            Info($"ModelOptions.NumTestSockets {SlotCount}");

                            Results = new TF_Result[SlotCount];
                            SlotCtrls = InternalSlotCtrls = new SlotInfo_TS[SlotCount];
                            SlotSequenceContexts = new SequenceContext[SlotCount];
                            SlotBlockQueues = new NamedPipeQueueServer[SlotCount];
                            SlotReportQueues = new Queue<TF_Result>[SlotCount];
                            for (int i = 0; i < SlotCount; i++)
                            {
                                Results[i] = ResultTemplate.Clone() as TF_Result;
                                Results[i].SocketIndex = i;
                                Results[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                                SlotCtrls[i] = InternalSlotCtrls[i] = new SlotInfo_TS(Results[i])
                                {
                                    IsParallelModel = true,
                                };
                                SlotCtrls[i].Index = i;
                                SlotBlockQueues[i] = new NamedPipeQueueServer($"{i}");
                                SlotBlockQueues[i].Initialize();
                                SlotReportQueues[i] = new Queue<TF_Result>();
                            }

                            ActionOnExecutionInitialized();
                        }

                        var cnt = TS_AppMgr.Executions.Count;

                        if (cnt > 1)
                        {
                            if (TS_AppMgr.Executions[1].DisplayName.Contains("SequenceFileLoad"))
                            {
                                if (cnt > 2)
                                {
                                    //InternalSlotCtrls[cnt - 3].TS_Execution = TS_AppMgr.Executions[cnt - 1];
                                    InternalSlotCtrls[cnt - 3].InitialExecution(TS_AppMgr.Executions[cnt - 1]);
                                    InternalSlotCtrls[cnt - 3].TS_SequenceContext = sequence;
                                    SlotSequenceContexts[cnt - 3] = sequence;
                                }
                            }
                            else
                            {
                                //InternalSlotCtrls[cnt - 2].TS_Execution = TS_AppMgr.Executions[cnt - 1];
                                InternalSlotCtrls[cnt - 2].InitialExecution(TS_AppMgr.Executions[cnt - 1]);
                                InternalSlotCtrls[cnt - 2].TS_SequenceContext = sequence;
                                SlotSequenceContexts[cnt - 2] = sequence;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"Start Execution Error: {ex}");
            }
        }

        private void TS_AppMgr_EndExecution(object sender, _ApplicationMgrEvents_EndExecutionEvent e)
        {
            Info(string.Format("StopExecution: {0}", e.exec.DisplayName));

            if (e.exec.DisplayName == Execution.DisplayName)
            {
                Info($"Execution {Execution?.DisplayName} Closed");
                TS_SeqFileViewMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_CloseCompletedExecutions, 0);
                TS_AppMgr.GetEngine().UnloadAllModules();
                IsRunning = false;
                Execution = null;
            }

            //if (TS_AppMgr.Executions.NumRunning == 0 && StopEngineWhenAllExecutionCompleted)
            //{
            //    StopEngine();
            //}
        }

        private static DateTime TS_ZeroTime = new DateTime(1970, 1, 1, 0, 0, 0);
        private SequenceContext HostSequenceContext;
        private readonly object writtingTymReportLock = new object();
        private void TS_AppMgr_AfterUIMessageEvent(object sender, _ApplicationMgrEvents_AfterUIMessageEventEvent e)
        {
            string tempstr;

            try
            {
                switch (e.uiMsg.Event)
                {
                    case UIMessageCodes.UIMsg_ReportChanged:
                        if (e.uiMsg.ActiveXData is Report report)
                        {
                            if (IsOriginalModel)
                            { }
                            else if (string.IsNullOrEmpty(report.Location))
                            { }
                            else
                            {
                                var filename = Path.GetFileName(report.Location);
                                foreach (var queue in SlotReportQueues)
                                {
                                    if (queue?.Count > 0)
                                    {
                                        var rs = queue.Peek();   // TODO, Hand the Results in Parallel
                                        if (filename.StartsWith(rs.SerialNumber))
                                        {
                                            rs = queue.Dequeue();
                                            try
                                            {
                                                string dest = report.Location;
                                                try
                                                {
                                                    var reportname = TF_Utility.TsReportNameTransformer(report.Location, rs);  // For support SN contains "_"
                                                    
                                                    dest = Path.Combine(Path.GetDirectoryName(report.Location), reportname);

                                                    File.Move(report.Location, dest);
                                                }
                                                catch (InvalidOperationException ioe)
                                                {
                                                    Warn(ioe);
                                                }

                                                if (!GlobalConfiguration.Default.General.DisableRemoteReport)
                                                {
                                                    rs.RawFile = dest;
                                                    PushReport(rs, dest);
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Warn(ex);
                                                MessageBox.Show($"Handling {rs.SerialNumber} Report Location: {report.Location} error. Err: {ex.Message}");
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        break;

                    case UIMessageCodes.UIMsg_StartExecution:

                        break;

                    // Model Initializing, once start execute seq
                    case UIMessageCodes.UIMsg_ModelState_Initializing:
                        if (e.uiMsg.NumericData >= 0)
                        {
                            Results[(int)e.uiMsg.NumericData].Status = TF_TestStatus.TEST_INIT;

                            if (IsOriginalModel) { }
                            else if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            {
                                try
                                {
                                    if (IsSequentialModel)
                                    {
                                        var additional = tempsc.Caller.Locals.GetPropertyObject("UUT.AdditionalData", 0);
                                        additional.NewSubProperty("GuiVersion", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("SpecVersion", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("IsSFC", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                        additional.NewSubProperty("SFCs_ExtColumn", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("SFCs_ExtValue", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("SFCs_BarcodePart", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("ExtraDefectCode", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("ExtraDefectName", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Customer", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Product", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Station", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Time", PropertyValueTypes.PropValType_String, false, "", 0);

                                        additional.NewSubProperty("Attach", PropertyValueTypes.PropValType_Container, false, "", 0);
                                        var attach = additional.GetPropertyObject("Attach", 0);
                                        foreach (var item in ResultTemplate.AttachProperties)
                                        {
                                            attach.NewSubProperty(item.Key, PropertyValueTypes.PropValType_String, false, "", 0);
                                        }

                                        additional.NewSubProperty("NAS_ExtFile", PropertyValueTypes.PropValType_String, false, "", 0);

                                        //PropFlags_IncludeInReport–(Value: 0x2000) 
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.GuiVersion", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.SpecVersion", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.IsSFC", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.SFCs_ExtColumn", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.SFCs_ExtValue", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.SFCs_BarcodePart", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.ExtraDefectCode", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.ExtraDefectName", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.Customer", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.Product", 0, 0x2000);
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.Station", 0, 0x2000);

                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.GuiVersion", 0, Application.ProductVersion);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.SpecVersion", 0, ResultTemplate.Specification.Version);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Customer", 0, ResultTemplate.StationConfig.CustomerName);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Product", 0, ResultTemplate.StationConfig.ProductName);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Station", 0, ResultTemplate.StationConfig.StationName);


                                    }
                                    else if (IsParallelModel || IsBatchModel)
                                    {
                                        // In Parallel, the UUT will be set to be an alias of Parameters.TestSocket.UUT
                                        var additional = tempsc.Caller.Parameters.GetPropertyObject("TestSocket.UUT.AdditionalData", 0);
                                        additional.NewSubProperty("GuiVersion", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("SpecVersion", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("IsSFC", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                        additional.NewSubProperty("SFCs_ExtColumn", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("SFCs_ExtValue", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("SFCs_BarcodePart", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("ExtraDefectCode", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("ExtraDefectName", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Customer", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Product", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Station", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Time", PropertyValueTypes.PropValType_String, false, "", 0);

                                        additional.NewSubProperty("Attach", PropertyValueTypes.PropValType_Container, false, "", 0);
                                        var attach = additional.GetPropertyObject("Attach", 0);
                                        foreach (var item in ResultTemplate.AttachProperties)
                                        {
                                            attach.NewSubProperty(item.Key, PropertyValueTypes.PropValType_String, false, "", 0);
                                        }

                                        additional.NewSubProperty("NAS_ExtFile", PropertyValueTypes.PropValType_String, false, "", 0);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.GuiVersion", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.SpecVersion", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.IsSFC", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.SFCs_ExtColumn", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.SFCs_ExtValue", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.SFCs_BarcodePart", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.ExtraDefectCode", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.ExtraDefectName", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.Customer", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.Product", 0, 0x2000);
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.Station", 0, 0x2000);

                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.GuiVersion", 0, Application.ProductVersion);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.SpecVersion", 0, ResultTemplate.Specification.Version);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.Customer", 0, ResultTemplate.StationConfig.CustomerName);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.Product", 0, ResultTemplate.StationConfig.ProductName);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.Station", 0, ResultTemplate.StationConfig.StationName);
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }

                        break;

                    // Before PreUUT executed
                    case UIMessageCodes.UIMsg_ModelState_Waiting:
                        if ((int)e.uiMsg.NumericData == -1)  // for batch
                        {
                            foreach (var rs in Results)
                            {
                                if (rs.Status == TF_TestStatus.TEST_INIT)
                                {
                                    rs.Status = TF_TestStatus.IDLE;
                                }
                                else if (rs.Status == TF_TestStatus.TESTING)
                                {
                                    rs.Status = TF_TestStatus.WAIT_DUT;
                                }
                            }
                        }
                        else
                        {
                            var rs = Results[(int)e.uiMsg.NumericData];
                            rs.SerialNumber = null;

                            if (rs.Status == TF_TestStatus.TEST_INIT)
                            {
                                rs.Status = TF_TestStatus.IDLE;
                            }
                            else if (rs.Status == TF_TestStatus.TESTING)  // Handing the test finished
                            {
                                rs.Status = TF_TestStatus.WAIT_DUT;
                            }

                            if (IsOriginalModel)
                            { }
                            else if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            {
                                if (IsSequentialModel || IsParallelModel)
                                {
                                    tempsc.Locals.SetValString("UUT.AdditionalData.SFCs_ExtValue", 0, "");
                                    tempsc.Locals.SetValString("UUT.AdditionalData.SFCs_BarcodePart", 0, "");
                                }
                                //else if (IsParallelModel)
                                //{
                                //    tempsc.Parameters.SetValString("TestSocket.UUT.AdditionalData.SFCs_ExtValue", 0, "");
                                //    tempsc.Parameters.SetValString("TestSocket.UUT.AdditionalData.SFCs_BarcodePart", 0, "");
                                //}

                                //SlotSequenceContexts[(int)e.uiMsg.NumericData] = tempsc;
                                //tempsc.Tracing = false;
                                //if (!IsOriginalModel)
                                //{
                                //    tempsc.Execution.Break();
                                //}
                            }
                        }
                        break;

                    // After UIMsg_ModelState_Identified.
                    case UIMessageCodes.UIMsg_ModelState_BeginTesting:
                        if (e.uiMsg.NumericData >= 0)
                        {
                            int slot = (int)e.uiMsg.NumericData;
                            var rs = Results[slot];

                            if (CustomizeInputSn && rs.ErrorMessage.Code == -23) { }
                            else
                            {
                                Results[(int)e.uiMsg.NumericData].Begin();
                            }
                        }
                        else
                        {
                            // TS Finish Initialized. In ParallelModel, it is trigged after all slot initialied
                            //if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            //{
                            //}
                        }
                        break;

                    //Infoed when Finished PreUUT ,For Customized SN. Update UI SN
                    case UIMessageCodes.UIMsg_ModelState_Identified:
                        if (e.uiMsg.NumericData >= 0)
                        {
                            int slot = (int)e.uiMsg.NumericData;
                            var rs = Results[slot];

                            if (IsOriginalModel)
                            {
                                rs.SerialNumber = e.uiMsg.StringData;
                            }
                            else if (CustomizeInputSn)
                            {
                                rs.SerialNumber = e.uiMsg.StringData;
                                rs.ErrorMessage = default(ErrorMsg);
                                try
                                {
                                    if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                                    {
                                        tempsc.Caller.SequenceErrorMessage = "";
                                    }

                                    if (Results[slot].IsSFC && GlobalConfiguration.Default.SFCs.EnableSfc && rs.SerialNumber?.Length > 2)
                                    {
                                        rs.LineNo = MesInstance?.GetLineNo(rs.SFCsConfig, rs.SerialNumber);
                                        rs.PartNo = MesInstance?.GetPartNo(rs.SFCsConfig, rs.SerialNumber);

                                        rs.StationConfig.LineNo = rs.LineNo;

                                        MesInstance?.CheckStation(rs.SFCsConfig, rs.SerialNumber, rs.LineNo);
                                    }
                                    else
                                    {
                                        rs.LineNo = rs.StationConfig.LineNo = string.Empty;
                                        rs.PartNo = string.Empty;
                                    }

                                    try
                                    {
                                        if (GlobalConfiguration.Default.SFCs.EnableSfc)
                                        {
                                            SlotSequenceContexts[slot].Locals.GetPropertyObject("UUT.AdditionalData", 0).SetValBoolean("IsSFC", 0, Results[slot].IsSFC && Results[slot].SerialNumber.Length > 2);
                                        }

                                        if (IsSequentialModel || IsParallelModel || IsBatchModel)
                                        {
                                            SlotSequenceContexts[slot].Locals.SetValString("UUT.SerialNumber", 0, Results[slot].SerialNumber);
                                            SlotSequenceContexts[slot].Locals.SetValString("UUT.PartNumber", 0, Results[slot].PartNo);
                                            SlotSequenceContexts[slot].Locals.SetValString("UUT.AdditionalData.Time", 0, (TimeService?.CurrentTime ?? DateTime.Now).ToString("yyyyMMddHHmmss"));
                                            SlotSequenceContexts[slot].Locals.SetValString("UUT.AdditionalData.ExtraDefectCode", 0, string.Empty);
                                            SlotSequenceContexts[slot].Locals.SetValString("UUT.AdditionalData.ExtraDefectName", 0, string.Empty);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Error(ex);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    rs.ErrorMessage = new ErrorMsg(-23, ex.Message);
                                    rs.End(TF_TestStatus.ERROR);

                                    Warn($"CustomizeInputSn Start Test Err: {ex}");

                                    if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                                    {
                                        tempsc.Caller.NextStepIndex = tempsc.Caller.Sequence.GetStepIndex("Model Plugins - Post UUT", StepGroups.StepGroup_Main);
                                        tempsc.Caller.SequenceErrorMessage = ex.Message;  // For LVHM, get error msg from PostUUT
                                        tempsc.Caller.SequenceErrorCode = -23;
                                        tempsc.Caller.SequenceErrorOccurred = true;
                                    }
                                }
                            }
                            else
                            {
                                SlotSequenceContexts[slot].Locals.SetValString("UUT.AdditionalData.ExtraDefectCode", 0, string.Empty);
                                SlotSequenceContexts[slot].Locals.SetValString("UUT.AdditionalData.ExtraDefectName", 0, string.Empty);

                                if (IsSequentialModel)
                                {
                                    SlotBlockQueues[slot] = new NamedPipeQueueServer("-1");
                                    SlotBlockQueues[slot].Initialize();
                                }
                                else if (IsParallelModel)
                                {
                                    SlotBlockQueues[slot] = new NamedPipeQueueServer($"{slot}");
                                    SlotBlockQueues[slot].Initialize();
                                }
                            }
                        }
                        else // For Batch
                        {
                            if (IsOriginalModel || CustomizeInputSn) { }
                            else
                            {
                                SlotBlockQueues[0] = new NamedPipeQueueServer("Batch");
                                SlotBlockQueues[0].Initialize();
                            }
                        }

                        break;

                    case UIMessageCodes.UIMsg_Trace:
                        //e.uiMsg.ActiveXData is null in Trace
                        break;

                    // After MainSeq Finished, Before PostUUT
                    //case UIMessageCodes.UIMsg_ModelState_TestingComplete:
                    case UIMessageCodes.UIMsg_UserMessageBase + 10:
                        if (e.uiMsg.NumericData >= 0)
                        {
                            var result = Results[(int)e.uiMsg.NumericData];

                            if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            {
                                if (tempsc.Caller.Sequence.GetStepByName("MainSequence Callback", StepGroups.StepGroup_Main).LastStepResult is PropertyObject rsseqpo)
                                {
                                    var rslist_mainseq = rsseqpo.GetValVariant("TS.SequenceCall.ResultList", 0) as object[];

                                    result.StartTime = TS_ZeroTime.AddSeconds(rsseqpo.GetValNumber("TS.StartTime", 0));
                                    result.EndTime = result.StartTime.AddSeconds(rsseqpo.GetValNumber("TS.TotalTime", 0));

                                    Parallel.ForEach(rslist_mainseq, (object rs) =>
                                    {
                                        if (rs is PropertyObject po)
                                        {
                                            StepRecordToResult(po, result.StepDatas);
                                        }
                                    });

                                    //foreach (var rs in rslist_mainseq)
                                    //{
                                    //    if (rs is PropertyObject po)
                                    //    {
                                    //        StepRecordToResult(po, result.StepDatas);
                                    //    }
                                    //}

                                    var rs_sts = TF_TestStatus.NULL;
                                    switch (e.uiMsg.StringData)
                                    {
                                        case "Failed":
                                            rs_sts = TF_TestStatus.FAILED;
                                            break;

                                        case "Done":
                                        case "Passed":
                                            rs_sts = TF_TestStatus.PASSED;
                                            break;

                                        case "Terminated":
                                            rs_sts = TF_TestStatus.TERMINATED;
                                            break;

                                        case "Error":
                                            result.ErrorMessage = new ErrorMsg((int)rsseqpo.GetValNumber("Error.Code", 0), rsseqpo.GetValString("Error.Msg", 0));
                                            rs_sts = TF_TestStatus.ERROR;
                                            break;

                                        default:
                                            rs_sts = TF_TestStatus.NULL;
                                            tempstr = e.uiMsg.StringData + e.uiMsg.Event.ToString() + e.uiMsg.Execution.DisplayName;
                                            Info(string.Format("Engine On TestingComplete. {0}", tempstr));
                                            break;
                                    }

                                    result.End(rs_sts);

                                    if (rs_sts == TF_TestStatus.FAILED)  // Refresh Defect if set Extra Defect
                                    {
                                        var extra_defectcode = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.ExtraDefectCode", 0);

                                        if (!string.IsNullOrEmpty(extra_defectcode))
                                        {
                                            var extra_defectname = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.ExtraDefectName", 0);

                                            result.Defect.Clear();
                                            result.Defect.Add(new Defect(extra_defectcode, extra_defectname, null));
                                        }
                                        else
                                        {
                                            var defectcode = result.Defect.FirstOrDefault();
                                            tempsc.Caller.Locals.SetValString("UUT.AdditionalData.ExtraDefectCode", 0, defectcode.Code);
                                            tempsc.Caller.Locals.SetValString("UUT.AdditionalData.ExtraDefectName", 0, defectcode.Desc);
                                        }
                                    }

                                    var attachnames = result.AttachProperties.Keys.ToArray();
                                    foreach (var key in attachnames)
                                    {
                                        result.AttachProperties[key] = tempsc.Caller.Locals.GetValString($"UUT.AdditionalData.Attach.{key}", 0);
                                    }

                                    if ((rs_sts == TF_TestStatus.PASSED || rs_sts == TF_TestStatus.FAILED) && !IsOriginalModel)
                                    {
                                        //RunState.Root.Locals.UUT.AdditionalData

                                        var SFCs_BarcodePart = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.SFCs_BarcodePart", 0);
                                        var SFCs_ExtValue = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.SFCs_ExtValue", 0);
                                        var SFCs_ExtColumn = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.SFCs_ExtColumn", 0);

                                        try
                                        {
                                            MesInstance?.CommitMesResult(result, SFCs_BarcodePart, SFCs_ExtColumn, SFCs_ExtValue);
                                        }
                                        catch (Exception ex)
                                        {
                                            result.ErrorMessage = new ErrorMsg(-23, ex.Message);
                                            result.Status = TF_TestStatus.ERROR;
                                        }
                                    }

                                    if (result.SerialNumber.Length > 2)
                                    {
                                        var temprs = (TF_Result)result.Clone();
                                        SlotReportQueues[(int)e.uiMsg.NumericData].Enqueue(temprs);

                                        if (!GlobalConfiguration.Default.General.DisableRemoteReport)
                                        {
                                            var nas_files = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.NAS_ExtFile", 0).Split(';');

                                            foreach (var file in nas_files)
                                            {
                                                if (File.Exists(file))
                                                {
                                                    PushReport(temprs, file);
                                                }
                                            }
                                        }
                                    }

                                    lock (writtingTymReportLock)
                                    {
                                        result.ExportTestDataCSV();
                                        result.ExportTestTimeCsv();
                                    }
                                }
                                else
                                {
                                    Warn("UIMsg_ModelState_TestingComplete with no data");
                                }
                            }
                        }
                        break;

                    //case UIMessageCodes.UIMsg_UserMessageBase + 10:   // Update Defect Code
                    //    //if (e.uiMsg.NumericData >= 0)
                    //    //{
                    //    //    var result = Results[(int)e.uiMsg.NumericData];

                    //    //    if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                    //    //    {
                    //    //        var dd = tempsc.CallStackName;

                    //    //        var rs_sts = result.Status;
                    //    //        if (result.Status == TF_TestStatus.FAILED)  // Refresh Defect if set Extra Defect
                    //    //        {
                    //    //            var vn = "UUT.AdditionalData.ExtraDefectCode";

                    //    //            if (tempsc.Parameters.Exists(vn, 0))
                    //    //            {
                    //    //                var code = tempsc.Parameters.GetPropertyObject(vn, 0);
                    //    //                var currcode = code.GetValString(string.Empty, 0);
                    //    //                //if (string.IsNullOrEmpty(currcode))
                    //    //                //{
                    //    //                //    var defect = result.Defect.FirstOrDefault();

                    //    //                //    code.SetValString(string.Empty, 0, defect.Code);
                    //    //                //    tempsc.Parameters.SetValString("UUT.AdditionalData.ExtraDefectName", 0, defect.Desc);
                    //    //                //}
                    //    //            }
                    //    //        }
                    //    //    }
                    //    //}
                    //    break;

                    case UIMessageCodes.UIMsg_EndExecution:
                        break;

                    case UIMessageCodes.UIMsg_RuntimeError:
                        break;

                    case UIMessageCodes.UIMsg_ModelState_PostProcessingComplete:
                        break;
                }

                if (e.uiMsg.Event != UIMessageCodes.UIMsg_Trace)
                {
                    string p = null;
                    if (e.uiMsg.ActiveXData is SequenceContext seq)
                    {
                        p = seq.SequenceFile.Path;
                    }

                    Info(string.Format("{0} {1} {2} {3}", e.uiMsg.Event, e.uiMsg.StringData, e.uiMsg.NumericData, p));
                }
            }
            catch (Exception ex)
            {
                Warn(string.Format("{0} {1} {2}, Err:{3}", e.uiMsg.Event, e.uiMsg.StringData, e.uiMsg.NumericData, ex));
            }
        }

        public override int SetUserName(string username)
        {
            try
            {
                if (username is null) return 0;

                UserName = username;

                if (TS_AppMgr.GetEngine().Globals.Exists("TS.CurrentUser", 0))
                {
                    TS_AppMgr?.GetEngine().Globals.SetValString("TS.CurrentUser.LoginName", 0, username);
                }
                else
                {
                    // If not login, the TestStand will not create the properties as below
                    TS_AppMgr.GetEngine().Globals.NewSubProperty("TS.CurrentUser.LoginName", PropertyValueTypes.PropValType_String, false, "", 0);
                    TS_AppMgr.GetEngine().Globals.NewSubProperty("TS.CurrentUser.FullName", PropertyValueTypes.PropValType_String, false, "", 0);

                    TS_AppMgr?.GetEngine().Globals.SetValString("TS.CurrentUser.LoginName", 0, username);
                }

                return 1;
            }
            catch (Exception ex)
            {
                Error(ex);
                return -1;
            }
        }
        #endregion

        private readonly object pushingReportLock = new object();
        private void PushReport(TF_Result rs, string dest)
        {
            if (IsForVerification)
            {
                Info("No Push Report. Test For Verification");
            }
            else if (string.IsNullOrEmpty(StationConfig.IpAddress))
            {
                Warn("No Push Report. No netwrok");
            }
            else
            {
                //int stsid = 1;
                //if (int.TryParse(rs.StationConfig.StationID, out int temp))
                //{
                //    stsid = temp;
                //}

                //int slotid = 0;
                //if (int.TryParse(rs.SocketId, out int temp1))
                //{
                //    slotid = temp1;
                //}

                lock (pushingReportLock)
                {
                    Info($"Push {dest} for {rs.SerialNumber}");
                    //Raven.Nas.Logbackup.Upload(dest);

                    ReportService?.Push(rs, dest);

                    Info($"Push {dest} OK");
                }
            }
        }

        public override int ApplySpecification(string specpath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// OriginalModel and CustomizeInputSn will never call this
        /// </summary>
        /// <param name="sn"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public override int StartNewTest(string sn, int slot)
        {
            DateTime t0 = DateTime.Now;

            double TimeOut_WaitReport = 30;

            //Info($"== Debug StartNewTest  Slot {slot}==");
            if (slot >= 0)
            {
                if (Results[slot].Status == TF_TestStatus.WAIT_DUT)
                {
                    while (SlotReportQueues[slot].Count != 0)  // For Prevent the New Test impact the last report SN
                    {
                        System.Threading.Thread.Sleep(50);

                        if (DateTime.Now.Subtract(t0).TotalSeconds > TimeOut_WaitReport)
                        {
                            Results[slot].ErrorMessage = new ErrorMsg(-1, "the Last Test Report has not been generated, Please Check");
                            Results[slot].Status = TF_TestStatus.ERROR;
                            return -1;
                        }
                    }

                    try
                    {
                        try
                        {
                            if (Results[slot].IsSFC && GlobalConfiguration.Default.SFCs.EnableSfc && Results[slot].SerialNumber.Length > 2)
                            {
                                Results[slot].LineNo = MesInstance?.GetLineNo(Results[slot].SFCsConfig, Results[slot].SerialNumber);
                                Results[slot].PartNo = MesInstance?.GetPartNo(Results[slot].SFCsConfig, Results[slot].SerialNumber);

                                Results[slot].StationConfig.LineNo = Results[slot].LineNo;

                                MesInstance?.CheckStation(Results[slot].SFCsConfig, Results[slot].SerialNumber, Results[slot].LineNo);
                            }
                            else
                            {
                                Results[slot].LineNo = Results[slot].StationConfig.LineNo = string.Empty;
                                Results[slot].PartNo = string.Empty;
                            }
                        }
                        catch (Exception ex)
                        {
                            Warn($"StartNewTest Err: {ex}");
                            Results[slot].ErrorMessage = new ErrorMsg(-23, ex.Message);
                            Results[slot].End(TF_TestStatus.ERROR);
                            return -1;
                        }

                        try
                        {
                            if (GlobalConfiguration.Default.SFCs.EnableSfc)
                            {
                                SlotSequenceContexts[slot].Locals.GetPropertyObject("UUT.AdditionalData", 0).SetValBoolean("IsSFC", 0, Results[slot].IsSFC && Results[slot].SerialNumber.Length > 2);
                            }

                            if (IsSequentialModel || IsParallelModel || IsBatchModel)
                            {
                                SlotSequenceContexts[slot].Locals.SetValString("UUT.SerialNumber", 0, Results[slot].SerialNumber);
                                SlotSequenceContexts[slot].Locals.SetValString("UUT.PartNumber", 0, Results[slot].PartNo);
                                SlotSequenceContexts[slot].Locals.SetValString("UUT.AdditionalData.Time", 0, (TimeService?.CurrentTime ?? DateTime.Now).ToString("yyyyMMddHHmmss"));
                            }
                        }
                        catch (Exception ex)
                        {
                            Error(ex);
                        }

                        if (!IsBatchModel)
                        {
                            t0 = DateTime.Now;
                            while(!SlotBlockQueues[slot].IsConnected)
                            {
                                System.Threading.Thread.Sleep(100);
                                if (DateTime.Now.Subtract(t0).TotalSeconds > 30)
                                {
                                    MessageBox.Show($"Not Ready. Please try scan SN Later. SN {sn}, Slot {slot}", "Warn");
                                    return -1;
                                }
                            }

                            SlotBlockQueues[slot].Enqueue($"{slot}:{sn}");
                            try
                            {
                                SlotBlockQueues[slot].Close();
                            }
                            catch
                            {
                            }
                            finally
                            {
                                SlotBlockQueues[slot].Dispose();
                            }
                        }
                    }
                    catch
                    {
                        Warn($"Set SN Failed. {sn}");
                        return -1;
                    }

                    return 1;
                }
                else
                {
                    Warn($"Try Start new test for slot {slot} under {Results[slot].Status}");
                }
            }
            else
            {
                if (IsBatchModel || IsSequentialModel)
                {
                    while (SlotReportQueues[slot].Count != 0)  // For Prevent the New Test impact the last report SN
                    {
                        System.Threading.Thread.Sleep(50);

                        if (DateTime.Now.Subtract(t0).TotalSeconds > TimeOut_WaitReport)
                        {
                            Results[slot].ErrorMessage = new ErrorMsg(-1, "the Last Test Report has not been generated, Please Check");
                            Results[slot].Status = TF_TestStatus.ERROR;
                            return -1;
                        }
                    }

                    t0 = DateTime.Now;

                    while (!SlotBlockQueues[0].IsConnected)
                    {
                        System.Threading.Thread.Sleep(100);
                        if (DateTime.Now.Subtract(t0).TotalSeconds > 30)
                        {
                            MessageBox.Show($"Not Ready. Please try scan SN Later. SN {sn}, Slot {slot}", "Warn");
                            return -1;
                        }
                    }

                    SlotBlockQueues[0].Enqueue(string.Empty);

                    try
                    {
                        SlotBlockQueues[0].Close();

                    }
                    catch
                    { }
                    finally
                    {
                        SlotBlockQueues[0].Dispose();
                    }
                }
            }

            return 0;
        }

        public override int FinishTest(int slot)
        {
            if (IsOriginalModel) return 1;
            if (CustomizeInputSn) return 1;
            if (!IsRunning) return 1;
            if (slot >= 0)
            {
                if (Results[slot].Status == TF_TestStatus.WAIT_DUT)
                {
                    if (IsSequentialModel || IsParallelModel)
                    {
                        SlotSequenceContexts[slot].Parameters.SetValBoolean("ContinueTesting", 0, false);
                    }

                    if (IsBatchModel)
                    {
                        SlotSequenceContexts[slot].Parameters.SetValBoolean("ContinueTesting", 0, false);
                    }
                }
                else
                {
                }
            }
            else
            {
                if (IsBatchModel)
                {
                    HostSequenceContext?.Locals?.SetValBoolean("ModelData.ContinueTesting", 0, false);

                    try
                    {
                        SlotBlockQueues[0].Enqueue($"-1:00");
                        SlotBlockQueues[0].Close();

                    }
                    catch (Exception ex)
                    {
                        Warn(ex);
                    }
                    finally
                    {
                        SlotBlockQueues[0].Dispose();
                    }
                }
                else
                {
                    for (int i = 0; i < SlotCount; i++)
                    {
                        switch (Results[i].Status)
                        {
                            case TF_TestStatus.IDLE:
                            case TF_TestStatus.WAIT_DUT:
                            case TF_TestStatus.PASSED:
                            case TF_TestStatus.FAILED:
                            case TF_TestStatus.NULL:
                            case TF_TestStatus.ERROR:
                            case TF_TestStatus.TERMINATED:
                            case TF_TestStatus.ABORT:
                            case TF_TestStatus.WAIVE:
                                if (IsSequentialModel)
                                {
                                    SlotSequenceContexts[i].Locals.SetValBoolean("ContinueTesting", 0, false);
                                }
                                else if (IsParallelModel)
                                {
                                    SlotSequenceContexts[i].Parameters.SetValBoolean("TestSocket.ContinueTesting", 0, false);
                                }
                                else if (IsBatchModel)
                                {
                                    //SlotSequenceContexts[i].Parameters.SetValBoolean("TestSocket.ContinueTesting", 0, false);
                                }

                                SlotBlockQueues[i].Enqueue($"{slot}:00");
                                try
                                {
                                    SlotBlockQueues[i].Close();

                                }
                                catch (Exception ex)
                                {
                                    Warn(ex);
                                }
                                finally
                                {
                                    SlotBlockQueues[i].Dispose();
                                }

                                break;

                            default:
                                MessageBox.Show($"Slot {i + 1} is {Results[i].Status}, Could not stop");
                                break;
                        }
                    }

                    // The TestStand will continue to run when UI is IDLE, which means loop means meaningless
                    //DateTime t0 = DateTime.Now;
                    //while (IsRunning)
                    //{
                    //    System.Threading.Thread.Sleep(100);

                    //    if (DateTime.Now.Subtract(t0).TotalSeconds > 5)
                    //    {
                    //        return 0;
                    //    }
                    //}
                }
            }

            return 1;
        }

        public bool CheckIfAllExecutionStopped(out string message)
        {
            bool closeable = true;
            message = string.Empty;
            foreach (NationalInstruments.TestStand.Interop.API.Execution exec in TS_AppMgr.Executions)
            {
                exec.GetStates(out ExecutionRunStates runs, out ExecutionTerminationStates terms);

                if (runs == ExecutionRunStates.ExecRunState_Stopped)
                {
                    message += string.Format("{0} Stopped\r\n", exec.DisplayName);
                }
                else
                {
                    closeable = false;
                    message += string.Format("{0} Terminating. Stats: {1}\r\n", exec.DisplayName, terms);
                }
            }

            return closeable;
        }
    }
}