using ApEngine.Base;
using AudioPrecision.API;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using TestCore;
using TestCore.Data;
using ToucanCore.Abstraction.HAL;
using ToucanCore.Abstraction;
using ToucanCore.Abstraction.Engine;
using System.Diagnostics.Contracts;

namespace ApEngine
{
    public class Execution : TF_Base, IExecution<Script>
    {
        internal static ApxEngine StaticEngine { get; set; }
        private Execution()
        {
        }

        internal Execution(Script script)
        {
            if (script.Spec is null)
            {
                script.Analyze();
            }

            Template = new TF_Result(script.Spec);
            Script = script;
            Template.SFCsConfig = script.SFCsConfig ?? new TestCore.Configuration.SFCsConfig(false, null, false);
            Template.StationConfig = script.StationConfig ?? new TestCore.Configuration.StationConfig("UKDC", "RD", "PRJ", "PRD", "SPL", "01");
            Template.GeneralConfig = script.SystemConfig?.General ?? new TestCore.Configuration.GeneralConfig();
            SocketCount = script.SystemConfig?.General?.SocketCount ?? 1;
            Template.IsSFC = Template.SFCsConfig.EnableSfc;

            Queue_NewTest = new BlockingQueue<TF_Result>(script.Name, SocketCount);

            //ApxEngine.ApRef.Variables.SetUserDefinedVariable("RefBase", script.GetReferenceBase());

            //var externdata = ApxEngine.ApRef.Variables.GetUserDefinedVariable("SFCs_ExtColumn");
            //if(!string.IsNullOrWhiteSpace(externdata))
            //{
            //    Template.ExtColumns = externdata;
            //}

            var srs = new TF_Result[SocketCount];

            Entrypoint = Sequence = (Sequence)script.ActiveSequence;
            Name = script.ActiveSequence.Name;

            var inherits = ((Sequence)script.ActiveSequence).InheritResults;
            if (inherits != null && inherits.Length == SocketCount)
            {
                Results = ResultsDut = inherits;
            }
            else
            {
                for (int i = 0; i < SocketCount; i++)
                {
                    srs[i] = Template.Clone() as TF_Result;
                    srs[i].SocketIndex = i;
                    //srs[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                    srs[i].Status = TF_TestStatus.IDLE;  // No need initialzed in APx;

                    srs[i].TestEnd += Rs_TestEnd;

                    srs[i].StepDatas.SyncRun(Sequence.Spec.Limit, (x, y) => { if (y is AP_Limit apl) { x.Tag = apl.ChannelIndex; } });
                }

                if (SocketCount < 9)
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        srs[i].SocketId = $"{i+1}";
                    }
                }
                else
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        srs[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                    }
                }

                Results = ResultsDut = srs;
                ((Sequence)script.ActiveSequence).InheritResults = srs;
            }

            if (SocketCount > 1)
            {
                bool oneapmultislot = true;
                for (int i = 0; i < SocketCount; i++)
                {
                    oneapmultislot &= Script.Sequences.FirstOrDefault(x => string.Equals(x.Name, $"Slot{i}", StringComparison.OrdinalIgnoreCase)) is Sequence;

                    if (!oneapmultislot) break;
                }

                if (oneapmultislot)
                {
                    this.OnPreUUTed += Execution_HandleOneApForMultiSlot;
                }
            }

            Workbase = Directory.GetParent(script.FilePath).FullName;
        }

        private void Execution_HandleOneApForMultiSlot(object sender, TF_Result e)
        {
            if (e.Name == ApxEngine.ReferenceSequenceName)
            {
                if (Script.Sequences.FirstOrDefault(x => string.Equals(x.Name, $"{ApxEngine.ReferenceSequenceName}_Slot{e.SocketIndex}", StringComparison.OrdinalIgnoreCase)) is Sequence seq)
                {
                    seq.ApSequence.Activate();
                }
            }
            else if (e.Name == ApxEngine.VerificationSequenceName)
            {
                if (Script.Sequences.FirstOrDefault(x => string.Equals(x.Name, $"{ApxEngine.VerificationSequenceName}_Slot{e.SocketIndex}", StringComparison.OrdinalIgnoreCase)) is Sequence seq)
                {
                    seq.ApSequence.Activate();
                }
            }
            else if(Script.Sequences.FirstOrDefault(x=> string.Equals(x.Name, $"Slot{e.SocketIndex}", StringComparison.OrdinalIgnoreCase)) is Sequence seq)
            {
                seq.ApSequence.Activate();
            }
        }

        public const string STEP_DELIMITER = "-&>";
        public const string MEAS_DELIMITER = "-#>";

        public IEngine Engine { get; } = StaticEngine;

        public IModel Model { get; }

        public string Name { get; private set; }
        public Script Script { get; private set; }
        public IScript GetScript() { return Script; }
        public ApEngine.Sequence Sequence { get; private set; }

        public int SlotIndex { get; private set; } = 0;
        public bool IsForVerification { get; set; }
        public bool BreakOnFirstStep { get; set; }
        public bool BreakOnFailure { get; set; }
        public bool GotoCleanupOnFailure { get; set; }
        public bool DisableResults { get; set; }
        public int ActionOnError { get; set; }

        public int SocketCount { get; private set; }

        public List<Variable> Variables { get; } = new List<Variable>();

        public TF_Result Template { get; private set; }

        public IReadOnlyList<TF_Result> Results { get; private set; }

        public ModelType ModelType { get; private set; } = ModelType.None;

        public ToucanCore.Abstraction.Engine.ISequence Entrypoint { get; set; }

        public string Workbase { get; set; }

        public event EventHandler ExecutionStarted;
        public event EventHandler ExecutionStopped;
        public event EventHandler<TF_Result> OnPreUUTLoop;
        public event EventHandler<TF_Result> OnPreUUTing;
        public event EventHandler<TF_Result> OnPreUUTed;
        public event EventHandler<TF_Result> OnUutIdentified;
        public event EventHandler<TF_Result> OnUutPassed;
        public event EventHandler<TF_Result> OnUutFailed;
        public event EventHandler<TF_Result> OnError;
        public event EventHandler<TF_Result> OnPostUUTing;
        public event EventHandler<TF_Result> OnPostUUTed;
        public event EventHandler<TF_Result> OnPostUUTLoop;
        public event EventHandler<TF_Result> OnTestCompleted;

        bool ContinueRun = true;
        public bool IsTesting { get; private set; }
        //public System.Threading.ManualResetEvent Mre_NewTest { get; } = new System.Threading.ManualResetEvent(false);
        public BlockingQueue<TF_Result> Queue_NewTest { get; private set; }

        public IReadOnlyList<TF_Result> ResultsRef;
        public IReadOnlyList<TF_Result> ResultsVer;
        public IReadOnlyList<TF_Result> ResultsDut;

        public ExecutionMode ExecutionMode { get; private set; } = ExecutionMode.Normal;

        private Action<Execution> _Execution = (Execution exec) =>
        {
            foreach (var d in exec.Results)
            {
                exec.OnPreUUTLoop?.Invoke(exec, d);
            }

            try
            {
                while (exec.Queue_NewTest.Dequeue(out TF_Result rs))
                {
                    exec.IsTesting = true;
                    if (exec.ContinueRun && rs != null)
                    {
                        exec.SlotIndex = rs.SocketIndex;
                        Run(exec);
                    }
                    else
                    {
                        exec.IsTesting = false;
                        break;
                    }
                }

                //while (exec.Mre_NewTest.WaitOne())
                //{
                //    exec.IsTesting = true;
                //    exec.Mre_NewTest.Reset();
                //    if (exec.ContinueRun)
                //    {
                //        Run(exec);
                //    }
                //    else
                //    {
                //        exec.IsTesting = false;
                //        break;
                //    }
                //}
            }
            finally
            {
                foreach (var d in exec.Results)
                {
                    exec.OnPostUUTLoop?.Invoke(exec, d);
                }
                exec.ExecutionStopped?.Invoke(exec, null);
            }
        };

        public static void Run(Execution exec)
        {
            TF_Result rs = exec.Results[exec.SlotIndex];

            try
            {
                //exec.OnUutIdentified?.Invoke(exec, rs);
                TF_Base.StaticLog("Start Lock Mre_Operation");
                ApxEngine.Mre_Operation.WaitOne();
                ApxEngine.Mre_Operation.Reset();

#if !DEBUG
                        if(ApxEngine.ApRef.IsDemoMode)
                        {
                            if (MessageBox.Show("AP is running in Demo Mode, Do you want check the Connect and Click OK to Continue Test", "Warning", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
                            {
                                throw new InvalidOperationException("Ap Disconnected. Running in Demo Mode");
                            }
                            exec.Warn("Ap Disconnected. Running in Demo Mode");
                        }
#endif
                ApxEngine.ApRef.Variables.SetUserDefinedVariable("IsSfc", (rs.IsSFC && rs.SFCsConfig.EnableSfc && rs.SerialNumber?.Length > 2) ? "1" : "0");
                ApxEngine.ApRef.Variables.SetUserDefinedVariable("SN", rs.SerialNumber);
                ApxEngine.ApRef.Variables.SetUserDefinedVariable("SlotIndex", exec.SlotIndex.ToString());

                //TODO: BeginInvoke might throw exception
                exec.OnPreUUTing?.BeginInvoke(exec, rs, null, null);

                exec.OnPreUUTed?.Invoke(exec, rs);

                exec.ApplyInjectedVariable(rs.SocketIndex);

                try
                {
                    //exec.ApplySignalSwitch(rs.SocketIndex);
                    exec.RunSequence(rs, out MeterResult[] meters, out XyyResult[] xyys, out XyResult[] xys, out ThieleSmallResult[] tss, out PassFailResult[] pfs, out string ErrorMessage);
                }
                catch(APException apex)
                {
                    rs.ErrorMessage = new ErrorMsg((int)ErrorCode.ExecutionOperationError, apex.Message);
                    rs.End(TF_TestStatus.ERROR);
                    exec.Warn(apex);
                    exec?.OnError?.Invoke(exec, rs);
                }

                var task = TaskGenerateReport(exec, rs);
                exec.OnPostUUTing?.BeginInvoke(exec, rs, null, null);            
                exec.OnPostUUTed?.Invoke(exec, rs);
                task?.Wait();
            }
            catch (Exception ex)
            {
                rs.ErrorMessage = new ErrorMsg((int)ErrorCode.ExecutionOperationError, ex.Message);
                rs.End(TF_TestStatus.ERROR);
                exec.Warn(ex);
                exec?.OnError?.Invoke(exec, rs);
            }
            finally
            {
                TF_Base.StaticLog("Start Release Mre_Operation");
                rs.SerialNumber = string.Empty;  // Prevent test with previous sn
                exec.IsTesting = false;
                ApxEngine.Mre_Operation.Set();
            }
        }

        private static Task TaskGenerateReport(Execution exec, TF_Result rs)
        {
            if (exec.Script.SystemConfig?.General?.Raw_ReportPath != null)
            {
                var task = Task.Run(() =>
                {
                    var dir = rs.GenerateLocalReportDir(exec.Script.SystemConfig?.General?.Raw_ReportPath);

                    try
                    {
                        exec.Engine.GenerateReport(rs, dir);
                    }
                    catch
                    {
                        Directory.CreateDirectory(dir);
                        exec.Engine.GenerateReport(rs, dir);
                    }
                });

                return task;
            }
            else
            {
                return null;
            }
        }

        public int Abort()
        {
            throw new NotImplementedException();
        }

        public void Break()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //Mre_NewTest?.Dispose();
            Queue_NewTest.Dispose();

        }

        public void Resume()
        {
            throw new NotImplementedException();
        }

        bool IsStarted = false;
        public int Start()
        {
            if (IsStarted) return 1;

            GotoCleanupOnFailure = Engine.GotoCleanupOnFailure;
            ActionOnError = Engine.ActionOnError;
            BreakOnFirstStep = Engine.BreakOnFirstStep;
            BreakOnFailure = Engine.BreakOnFailure;
            DisableResults = Engine.DisableResults;
            IsForVerification = Engine.IsForVerification;

            Script.Activate(Entrypoint as Sequence);

            _Execution.BeginInvoke(this, null, null);

            Variables.Clear();

            var vns = ApxEngine.ApRef.Variables.GetUserDefinedVariables();
            foreach (var name in vns)
            {
                Variables.Add(new Variable() { Name = name, Type = "String", Value = ApxEngine.ApRef.Variables.GetUserDefinedVariable(name) });
            }

            foreach (var rs in Results)
            {
                OnPreUUTLoop?.Invoke(this, rs);
            }
            ExecutionStarted?.Invoke(this, null);
            
            ContinueRun = true;
            IsStarted = true;
            return 1;
        }

        private void Rs_TestEnd(object sender, EventArgs args)
        {
            if (sender is TF_Result rs)
            {
                OnTestCompleted?.Invoke(this, rs);

                switch (rs.Status)
                {
                    case TF_TestStatus.PASSED:
                    case TF_TestStatus.WAIVE:
                        OnUutPassed?.Invoke(this, rs);
                        break;
                    case TF_TestStatus.FAILED:
                        OnUutFailed?.Invoke(this, rs);
                        break;
                }

                foreach (var item in Variables)
                {
                    item.Value = ApxEngine.ApRef.Variables.GetUserDefinedVariable(item.Name);
                }

                //if(string.IsNullOrEmpty(rs.ExtColumns))
                //{
                //    rs.ExtValues = ApxEngine.ApRef.Variables.GetUserDefinedVariable("SFCs_ExtValue");
                //}
            }
        }

        public int StartNewTest(int slotIndex = 0)
        {
            //SlotIndex = slotIndex;
            TF_Result rs = Results[slotIndex];
            if (string.IsNullOrWhiteSpace(rs.SerialNumber))
            {
                return 0;
            }

            rs.Status = TF_TestStatus.TEST_INIT;  // Init the status, to reset the preview status
            OnUutIdentified?.Invoke(this, rs);

            if (rs.Status == TF_TestStatus.ERROR) return -1;

            //try
            //{ 

            //}
            //catch(Exception ex)
            //{
            //    Warn(ex);
            //    rs.ErrorMessage = new ErrorMsg() { Info = ex.ToString(), Code = (int)ErrorCode.ExecutionOperationError };
            //    rs.Status = TF_TestStatus.ERROR;
            //    return -1;
            //}

            if(Queue_NewTest.Contains(Results[slotIndex]))
            {
                Warn($"Exist Test for {slotIndex}. Action Ignored");
            }
            else
            {
                Queue_NewTest.Enqueue(Results[slotIndex]);
            }

            //Mre_NewTest.Set();

            return 1;
        }

        public void StepIn()
        {
            throw new NotImplementedException();
        }

        public void StepOut()
        {
            throw new NotImplementedException();
        }

        public void StepOver()
        {
            throw new NotImplementedException();
        }

        public int Stop()
        {
            ContinueRun = false;
            //Mre_NewTest.Set();
            Queue_NewTest.Enqueue(null);

            IsStarted = false;
            
            foreach (var rs in Results)
            {
                OnPostUUTLoop?.Invoke(this, rs);
            }

            ExecutionStopped?.Invoke(this, null);  // Ref, Ver depend on this to switch back, this will called when Continue Run False and test finished

            return 1;
        }

        public int Terminate()
        {
            throw new NotImplementedException();
        }

        public void ShowVariables(int slot)
        {
            
        }

        public void RunSequence(TF_Result result, out MeterResult[] meters, out XyyResult[] xyys, out XyResult[] xys, out ThieleSmallResult[] tss, out PassFailResult[] pfs, out string ErrorMessage)
        {
            result.Begin(DateTime.Now);
            ApxEngine.ApRef.Sequence.Run();

            List<string> ErrorSteps = new List<string>();
            ErrorMessage = null;

            List<MeterResult> rss_meter = new List<MeterResult>();
            List<XyyResult> rss_xyy = new List<XyyResult>();
            List<XyResult> rss_xy = new List<XyResult>();
            List<ThieleSmallResult> rss_ts = new List<ThieleSmallResult>();
            List<PassFailResult> rss_pf = new List<PassFailResult>();
            bool IsPassed = ApxEngine.ApRef.Sequence.Passed;
            var seqstatus = ApxEngine.ApRef.Sequence.Status;
            var vv = ApxEngine.ApRef.Sequence.StatusMessage;

            if (seqstatus == SequenceCompletedStatus.SequenceCancelledByUser)
            {
                result.ErrorMessage = new ErrorMsg(-1, "Test Cancelled By User", true);
                result.End(TF_TestStatus.ERROR);
            }
            else if (seqstatus == SequenceCompletedStatus.SequenceAbortedByError)
            {
                result.ErrorMessage = new ErrorMsg(-1, $"Test Abort for {seqstatus}", true);
                result.End(TF_TestStatus.ERROR);
            }
            else if (seqstatus == SequenceCompletedStatus.SequenceCompletedWithErrors)
            {
                result.ErrorMessage = new ErrorMsg(-2, "Sequence Completed With Errors", true);
                result.End(TF_TestStatus.ERROR);           // For Cli Call Exception   
            }
            else
            {
                for (int i = 0; i < ApxEngine.ApRef.Sequence.Count; i++)
                {
                    var signalpath = ApxEngine.ApRef.Sequence[i];

                    var signalname = signalpath.Name;

                    var item_l1 = result.StepDatas.FirstOrDefault(x => x.Element.Name == signalname);

                    if (item_l1 == null) continue;
                    //var seqlimit_l1 = Sequence.Spec.Limit.FirstOrDefault(x => x.Element.Name == signalname);
                    item_l1?.Element?.Begin(DateTime.Now);
                    for (int j = 0; j < signalpath.Count; j++)
                    {
                        var step = signalpath[j];

                        var stepname = $"{signalname}{STEP_DELIMITER}{step.Name}";

                        var item_l2 = item_l1.FirstOrDefault(x => x.Element.Name == step.Name);
                        if (item_l2 is null) continue; // For there are some Unsupport measurement yet. such as TS
                        //var seqlimit_l2 = seqlimit_l1.FirstOrDefault(x => x.Element.Name == step.Name);
                        if (step.HasSequenceResults)
                        {
                            item_l2?.Element?.Begin(DateTime.Now);
                            for (int k = 0; k < step.SequenceResults.Count; k++)
                            {
                                var seqrs = step.SequenceResults[k];

                                var item_l3 = item_l2.FirstOrDefault(x => x.Element.Name == seqrs.Name);

                                if (item_l3 is null)
                                {
                                    if (seqrs.ResultType == MeasurementResultType.AcquiredWaveform && true)
                                    {
                                        var channelcount = seqrs.ChannelCount;
                                        //for (int chidx = 0; chidx < channelcount; chidx++)
                                        //{
                                        //}
                                    }
                                    continue; // there are some measurement without Limits
                                }

                                //var seqlimit_l3 = seqlimit_l2.FirstOrDefault(x => x.Element.Name == seqrs.Name);
                                item_l3.Element?.Begin(DateTime.Now);
                                if (seqrs.HasErrorMessage && ErrorMessage is null)
                                {
                                    ErrorMessage = seqrs.ErrorMessage;
                                }

                                if (seqrs.HasMeterValues)
                                {
                                    MeterResult rs = new MeterResult();

                                    rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
                                    rs.Unit = seqrs.MeterUnit;
                                    rs.PassUlAll = seqrs.PassedUpperLimitCheck;
                                    rs.PassLlAll = seqrs.PassedLowerLimitCheck;

                                    if (seqrs.HasErrorMessage)
                                    {
                                        ErrorSteps.Add(rs.Name);
                                    }

                                    var vals = seqrs.GetMeterValues();
                                    var uls = seqrs.GetMeterUpperLimitValues();
                                    var lsl = seqrs.GetMeterLowerLimitValues();
                                    var names = seqrs.ChannelNames;     // The channel name might be updated when test by APx
                                    var readings = seqrs.GetMeterText();

                                    var cnt = Math.Min(Math.Min(Math.Min(seqrs.ChannelCount, vals.Length), uls.Length), lsl.Length);

                                    for (int chidx = 0; chidx < cnt; chidx++)
                                    {
                                        //if (seqlimit_l3.FirstOrDefault(x => ((AP_Limit)x.Element).ChannelIndex == chidx)?.Element is AP_Limit seqlimit_l4)
                                        if (item_l3.FirstOrDefault(x => x.Element.Tag?.Equals(chidx) == true)?.Element is TF_ItemData item_l4)
                                        {
                                            //item_l4 = item_l3.FirstOrDefault(x => x.Element.Name == seqlimit_l4.Name)?.Element as TF_ItemData;
                                            if (item_l4 is null) continue;

                                            item_l4.Begin(DateTime.Now);
                                            MeterData data = new MeterData(
                                                vals[chidx],
                                                uls[chidx],
                                                lsl[chidx],
                                                seqrs.PassedUpperLimitCheckOnChannel(chidx),
                                                seqrs.PassedLowerLimitCheckOnChannel(chidx));

                                            item_l4.Value = vals[chidx];
                                            // AP will make the NaN item Passed
                                            item_l4.End(data.PassLl && data.PassUl && !double.IsNaN(vals[chidx]) ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                            item_l4.EndTime = DateTime.Now;
                                            rs.Datas.Add(data);
                                        }
                                        else
                                        {
                                            continue;
                                            //if (chidx >= item_l3.Children.Count) break;
                                            //item_l4 = item_l3[chidx].Element as TF_ItemData;    // Ignore the name
                                        }
                                    }

                                    rss_meter.Add(rs);
                                }
                                else if (seqrs.HasXYYValues)
                                {
                                    XyyResult rs = new XyyResult();
                                    rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
                                    rs.XUnit = seqrs.XUnit;
                                    rs.LeftUnit = seqrs.LeftUnit;
                                    rs.RightUnit = seqrs.RightUnit;
                                    rs.PassUlAll = seqrs.PassedUpperLimitCheck;
                                    rs.PassLlAll = seqrs.PassedLowerLimitCheck;

                                    if (seqrs.HasErrorMessage)
                                    {
                                        ErrorSteps.Add(rs.Name);
                                    }

                                    var chcnt = seqrs.ChannelCount;

                                    for (int chidx = 0; chidx < chcnt; chidx++)
                                    {
                                        var hasdata = seqrs.HasData(SourceDataType.Measured, chidx);
                                        if (!hasdata) continue;
                                        var x_l = seqrs.GetXValues(chidx, VerticalAxis.Left, SourceDataType.Measured, 0);
                                        var x_r = seqrs.GetXValues(chidx, VerticalAxis.Right, SourceDataType.Measured, 0);

                                        var y_l = seqrs.GetYValues(chidx, VerticalAxis.Left, SourceDataType.Measured, 0);
                                        var y_r = seqrs.GetYValues(chidx, VerticalAxis.Right, SourceDataType.Measured, 0);

                                        var min = Math.Min(x_l.Length, y_l.Length);

                                        Point[] data_l = new Point[min];

                                        for (int idx = 0; idx < min; idx++)
                                        {
                                            data_l[idx] = new Point(x_l[idx], y_l[idx]);
                                        }

                                        min = Math.Min(x_r.Length, y_r.Length);
                                        Point[] data_r = new Point[min];

                                        for (int idx = 0; idx < min; idx++)
                                        {
                                            data_r[idx] = new Point(x_r[idx], y_r[idx]);
                                        }

                                        XyyData data = new XyyData()
                                        {
                                            XyValueLeft = data_l,
                                            XyValueRight = data_r,
                                            PassLl = seqrs.PassedLowerLimitCheckOnChannel(chidx),
                                            PassUl = seqrs.PassedUpperLimitCheckOnChannel(chidx),
                                        };

                                        //var item_l4_left = item_l3[chidx]?.Element as TF_ItemData;

                                        // What if the channel name changed
                                        // What if the front channel unchecked
                                        if (item_l3.FirstOrDefault(x => x.Element.Tag.Equals(chidx))?.Element is TF_ItemData item_l4_left)
                                        {
                                            TF_Curve cv = item_l4_left.Limit.LSL as TF_Curve;
                                            if (cv is null)
                                            {
                                                cv = item_l4_left.Limit.USL as TF_Curve;
                                            }

                                            if (item_l4_left.Limit.AdditionInfo is null)
                                            {
                                                var cvtemplate = new TF_Curve(x_l, y_l, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);

                                                AP_Limit templimit = new AP_Limit(item_l4_left.Limit.Name,
                                                    item_l4_left.Limit.USL,
                                                    item_l4_left.Limit.LSL,
                                                    item_l4_left.Limit.Comp,
                                                    item_l4_left.Limit.Defect,
                                                    item_l4_left.Limit.Format,
                                                    item_l4_left.Limit.Unit,
                                                    item_l4_left.Limit.Skip,
                                                    item_l4_left.Limit.Sfc);

                                                templimit.AdditionInfo = cvtemplate;
                                                templimit.Tag = item_l4_left.Limit;
                                                templimit.ChannelIndex = ((AP_Limit)item_l4_left.Limit).ChannelIndex;

                                                if (item_l4_left.Limit.LSL is TF_Curve cvlsl)
                                                {
                                                    templimit.LSL = cvlsl.Resample(cvtemplate);
                                                }
                                                if (item_l4_left.Limit.USL is TF_Curve cvusl)
                                                {
                                                    templimit.USL = cvusl.Resample(cvtemplate);
                                                }

                                                item_l4_left.Limit = templimit;
                                            }

                                            item_l4_left.Begin(DateTime.Now);
                                            item_l4_left.Value = new TF_Curve(x_l, y_l, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);
                                            item_l4_left.End(data.PassLl & data.PassUl ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                            item_l4_left.EndTime = DateTime.Now;
                                        }

                                        if (item_l3.FirstOrDefault(x => x.Element.Tag.Equals(-1 - chidx))?.Element is TF_ItemData item_l4_right)
                                        {
                                            TF_Curve cv = item_l4_right.Limit.LSL as TF_Curve;
                                            if (cv is null)
                                            {
                                                cv = item_l4_right.Limit.USL as TF_Curve;
                                            }

                                            if (item_l4_right.Limit.AdditionInfo is null)
                                            {
                                                var cvtemplate = new TF_Curve(x_l, y_l, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);

                                                AP_Limit templimit = new AP_Limit(item_l4_right.Limit.Name,
                                                    item_l4_right.Limit.USL,
                                                    item_l4_right.Limit.LSL,
                                                    item_l4_right.Limit.Comp,
                                                    item_l4_right.Limit.Defect,
                                                    item_l4_right.Limit.Format,
                                                    item_l4_right.Limit.Unit,
                                                    item_l4_right.Limit.Skip,
                                                    item_l4_right.Limit.Sfc);
                                                templimit.AdditionInfo = cvtemplate;

                                                templimit.Tag = item_l4_right.Limit;
                                                templimit.ChannelIndex = ((AP_Limit)item_l4_right.Limit).ChannelIndex;

                                                if (item_l4_right.Limit.LSL is TF_Curve cvlsl)
                                                {
                                                    templimit.LSL = cvlsl.Resample(cvtemplate);
                                                }
                                                if (item_l4_right.Limit.USL is TF_Curve cvusl)
                                                {
                                                    templimit.USL = cvusl.Resample(cvtemplate);
                                                }

                                                item_l4_right.Limit = templimit;
                                            }

                                            item_l4_right.Begin(DateTime.Now);
                                            item_l4_right.Value = new TF_Curve(x_r, y_r, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);
                                            item_l4_right.End(data.PassLl & data.PassUl ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                            item_l4_right.EndTime = DateTime.Now;
                                        }

                                        rs.Datas.Add(data);
                                    }

                                    rss_xyy.Add(rs);
                                }
                                else if (seqrs.HasXYValues)
                                {
                                    XyResult rs = new XyResult();
                                    rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
                                    rs.XUnit = seqrs.XUnit;
                                    rs.YUnit = seqrs.YUnit;
                                    rs.PassUlAll = seqrs.PassedUpperLimitCheck;
                                    rs.PassLlAll = seqrs.PassedLowerLimitCheck;

                                    if (seqrs.HasErrorMessage)
                                    {
                                        ErrorSteps.Add(rs.Name);
                                    }

                                    SourceDataType sdt = SourceDataType.Measured;
                                    if (!seqrs.HasData(sdt, 0))
                                    {
                                        sdt = SourceDataType.Imported;
                                        if (!seqrs.HasData(sdt, 0))
                                        {
                                            sdt = SourceDataType.CustomData;
                                            if (!seqrs.HasData(sdt, 0))
                                            {
                                                continue;
                                            }
                                        }
                                    }

                                    var channelcount = seqrs.ChannelCount;

                                    //if (seqrs.ResultType == MeasurementResultType.AcquiredWaveform)
                                    //{
                                    //    if (Script.SystemConfig.General.EnableAdditionalFileForAll || Script.SystemConfig.General.EnableAdditionalFileOnFailure)
                                    //    {
                                    //        for (int chidx = 0; chidx < channelcount; chidx++)
                                    //        {
                                    //            var xd = seqrs.GetXValues(chidx, VerticalAxis.Left, sdt);
                                    //            var yd = seqrs.GetYValues(chidx, VerticalAxis.Left, sdt);
                                    //        }

                                    //        Engine.GenerateReport()
                                    //    }
                                    //}


                                    // in Comparison derived measurement, the channel will reduced and rename as Trace1 (default name if no change), and following derived measment will update when test.

                                    //this names is not as ChannelNames in signal path, it is the name in tracestyle
                                    var names = seqrs.ChannelNames;   // the channel names might be less than the graph, found in Level and Distortion  // Level and Distortion has no limit
                                                                      // the name is kinds of first Channel Count of Channel Names, which include import data
                                                                      // the names might be more than the item, for the channel could be unchecked
                                    for (int chidx = 0; chidx < channelcount; chidx++)
                                    {
                                        //var item_l4 = item_l3.FirstOrDefault(x => x.Element.Name == names[chidx])?.Element as TF_ItemData;

                                        // What if the channel name changed
                                        // What if the front channel unchecked
                                        if (item_l3.FirstOrDefault(x => x.Element.Tag.Equals(chidx))?.Element is TF_ItemData item_l4)
                                        {
                                            //item_l4 = item_l3.FirstOrDefault(x => x.Element.Name == seqlimit_l4.Name)?.Element as TF_ItemData;

                                            //if (item_l4 is null) continue;

                                            var xd = seqrs.GetXValues(chidx, VerticalAxis.Left, sdt);
                                            var yd = seqrs.GetYValues(chidx, VerticalAxis.Left, sdt);

                                            var min = Math.Min(xd.Length, yd.Length);

                                            Point[] pd = new Point[min];

                                            for (int idx = 0; idx < min; idx++)
                                            {
                                                pd[idx] = new Point(xd[idx], yd[idx]);
                                            }

                                            XyData data = new XyData()
                                            {
                                                XyValue = pd,
                                                PassLl = seqrs.PassedLowerLimitCheckOnChannel(chidx),
                                                PassUl = seqrs.PassedUpperLimitCheckOnChannel(chidx),
                                            };

                                            TF_Curve cv = item_l4.Limit.LSL as TF_Curve;
                                            if (cv is null)
                                            {
                                                cv = item_l4.Limit.USL as TF_Curve;
                                            }

                                            if (item_l4.Limit?.AdditionInfo is null)
                                            {
                                                var cvtemplate = new TF_Curve(xd, yd, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);

                                                AP_Limit templimit = new AP_Limit(item_l4.Limit.Name,
                                                        item_l4.Limit.USL,
                                                        item_l4.Limit.LSL,
                                                        item_l4.Limit.Comp,
                                                        item_l4.Limit.Defect,
                                                        item_l4.Limit.Format,
                                                        item_l4.Limit.Unit,
                                                        item_l4.Limit.Skip,
                                                        item_l4.Limit.Sfc);

                                                templimit.AdditionInfo = cvtemplate;

                                                templimit.Tag = item_l4.Limit;
                                                templimit.ChannelIndex = ((AP_Limit)item_l4.Limit).ChannelIndex;
                                                if (item_l4.Limit.LSL is TF_Curve cvlsl)
                                                {
                                                    templimit.LSL = cvlsl.Resample(cvtemplate);
                                                }
                                                if (item_l4.Limit.USL is TF_Curve cvusl)
                                                {
                                                    templimit.USL = cvusl.Resample(cvtemplate);
                                                }

                                                item_l4.Limit = templimit;
                                            }

                                            item_l4.Begin(DateTime.Now);
                                            item_l4.Value = new TF_Curve(xd, yd, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);
                                            //item_l4.End(data.PassLl & data.PassUl ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                            item_l4.End();  // for the import data will make XyData chidx change, which will get the import data check limit

                                            if (item_l4.Result == TF_ItemStatus.Failed && data.PassLl && data.PassUl)
                                            {
                                                Warn($"Result Conflicted. on {item_l1.Element.Name}|{item_l2.Element.Name}|{item_l3.Element.Name}|{item_l4.Name}");
                                                Debug($"Value: {item_l4.Value}");
                                                Debug($"Limit USL: {item_l4.Limit.USL}");
                                                Debug($"Limit LSL: {item_l4.Limit.LSL}");
                                                Debug($"Limit Tag USL: {((AP_Limit)(item_l4.Limit.Tag))?.USL}");
                                                Debug($"Limit Tag LSL: {((AP_Limit)(item_l4.Limit.Tag))?.LSL}");

                                                item_l4.End(TF_ItemStatus.Passed);
                                            }

                                            item_l4.EndTime = DateTime.Now;
                                            rs.Datas.Add(data);
                                        }
                                        else
                                        {
                                            continue;
                                            //if (chidx >= item_l3.Children.Count) break;
                                            //item_l4 = item_l3[chidx].Element as TF_ItemData;    // Ignore the name
                                        }


                                    }
                                    rss_xy.Add(rs);


                                }
                                else if (seqrs.HasThieleSmallValues)
                                {
                                    ThieleSmallResult rs = new ThieleSmallResult();

                                    rs.Name = $"{stepname}{MEAS_DELIMITER}{seqrs.Name}";
                                    rs.PassUlAll = seqrs.PassedUpperLimitCheck;
                                    rs.PassLlAll = seqrs.PassedLowerLimitCheck;

                                    if (seqrs.HasErrorMessage)
                                    {
                                        ErrorSteps.Add(rs.Name);
                                    }

                                    foreach (ThieleSmallParameter ts in ApxEngine.ThieleSmallValueList)
                                    {
                                        ThieleSmallData tsdata = new ThieleSmallData()
                                        {
                                            Parameter = ts,
                                            Value = seqrs.GetThieleSmallValue(ts),
                                            Unit = seqrs.GetThieleSmallValueText(ts),
                                            LSL = seqrs.GetThieleSmallLowerLimitValue(ts),
                                            USL = seqrs.GetThieleSmallUpperLimitValue(ts),
                                            PassLl = seqrs.PassedThieleSmallLowerLimit(ts),
                                            PassUl = seqrs.PassedThieleSmallUpperLimit(ts),
                                        };

                                        var item_l4 = item_l3?.FirstOrDefault(x => x.Element.Name.Equals(ts.ToString(), StringComparison.OrdinalIgnoreCase))?.Element as TF_ItemData;

                                        if (item_l4 is null)
                                        {
                                            //Warn($"{ts} is missing");
                                        }
                                        else
                                        {
                                            item_l4.Begin(DateTime.Now);
                                            item_l4.Value = tsdata.Value;
                                            item_l4.End(tsdata.PassLl & tsdata.PassUl ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                            item_l4.EndTime = DateTime.Now;
                                        }

                                        rs.Datas.Add(tsdata);
                                    }
                                    rss_ts.Add(rs);
                                }
                                else if (seqrs.HasRawTextResults)
                                {
                                }

                                if (item_l3 != null)
                                {
                                    item_l3.Element?.End(item_l3.Children.All(x => ((TF_ItemData)x.Element).Result == TF_ItemStatus.Passed) ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                    item_l3.Element.EndTime = DateTime.Now;
                                }
                            }

                            if (item_l2 != null)
                            {
                                item_l2?.Element?.End(item_l2.Children.All(x => ((TF_ItemData)x.Element).Result == TF_ItemStatus.Passed) ? TF_ItemStatus.Passed : TF_ItemStatus.Failed);
                                item_l2.Element.EndTime = DateTime.Now;
                            }
                        }

                        if (step.MeasurementType == MeasurementType.PassFail)
                        {
                            item_l2?.Element?.Begin(DateTime.Now);
                            PassFailResult rs = new PassFailResult()
                            {
                                Name = stepname,
                                Result = step.SequenceResults.PassedLimitChecks,
                            };

                            var haserr = step.SequenceResults.HasErrors;

                            if (haserr)
                            {
                                var err = step.SequenceResults.ErrorMessage;
                                item_l2?.Element?.End(TF_ItemStatus.Error);
                                result.ErrorMessage = new ErrorMsg((int)ErrorCode.ExecutionOperationError - 1, $"{item_l2.Element.Name}: {err}", true);
                            }
                            else
                            {
                                if (item_l2.Element is TF_ItemData item)
                                {
                                    item.Value = step.SequenceResults.PassedLimitChecks ? 1 : 0;
                                }

                                item_l2?.Element?.End();
                            }

                            if (item_l2 != null)
                            {
                                item_l2.Element.EndTime = DateTime.Now;
                            }

                            rss_pf.Add(rs);
                        }

                        item_l1?.Element?.End();
                        if (item_l1 != null)
                        {
                            item_l1.Element.EndTime = DateTime.Now;
                        }
                    }
                }

                if (result.StepDatas.LastOrDefault()?.LastOrDefault()?.LastOrDefault()?.FirstOrDefault(x=>x.Element.Result == TF_ItemStatus.NotTested) is Nest<TF_StepData> dumpdata)
                {
                    result.ErrorMessage = new ErrorMsg(-1, $"{dumpdata.Element.Name} dose not test. Please Check AP", true);
                    result.End(TF_TestStatus.ERROR);
                }
                else
                {
                    result.End();
                }
            }

            int RunStepCount = rss_meter.Count + rss_xyy.Count + rss_xy.Count + rss_ts.Count;
            meters = rss_meter.ToArray();
            xyys = rss_xyy.ToArray();
            xys = rss_xy.ToArray();
            tss = rss_ts.ToArray();
            pfs = rss_pf.ToArray();
        }

        public void ApplyInjectedVariable(int slotindex)
        {
            if(Script.InjectedVariableTable?.Count > 0)
            {
                foreach(var item in Script.InjectedVariableTable)
                {
                    ApxEngine.ApRef.Variables.SetUserDefinedVariable(item.Name, item.Value[slotindex]);
                }
            }
        }

        public int EnableSlot(int slotindex, bool status = true)
        {
            //MessageBox.Show($"Enable Slot. {slotindex}, {status}. Not Implement yet");
            //Results[slotindex]
            //throw new NotImplementedException($"Enable Slot. {slotindex}, {status}. Not Implement yet");

            Info("APx Engine does not support Enable Slot yet");

            return 1;
        }

        public void SetVariable(string name, object val)
        {
            ApxEngine.ApRef.Variables.SetUserDefinedVariable(name, val as string);
        }

        public object GetVariable(string name) 
        {
            return ApxEngine.ApRef.Variables.GetUserDefinedVariable(name);
        }

        public int SwitchExecutionMode(ExecutionMode mode)
        {
            switch(mode)
            {
                case ExecutionMode.Reference:
                    Sequence = Script.Sequences.FirstOrDefault(x => x.Name == ApxEngine.ReferenceSequenceName) as Sequence;
                    Script.Activate(Sequence);
                    
                    if(ResultsRef is null)
                    {
                        var rrs = new TF_Result[SocketCount];

                        var temprv = new TF_Result(Script.AnalyzeSpec());
                        temprv.SFCsConfig = Script.SystemConfig?.SFCs ?? new TestCore.Configuration.SFCsConfig(false, null, false);
                        temprv.StationConfig = Script.SystemConfig?.Station ?? new TestCore.Configuration.StationConfig("UKDC", "RD", "PRJ", "PRD", "SPL", "01");
                        temprv.GeneralConfig = Script.SystemConfig?.General ?? new TestCore.Configuration.GeneralConfig();
                        temprv.IsSFC = false;

                        var refbase = Script.GetReferenceBase();
                        if (!Directory.Exists(refbase)) Directory.CreateDirectory(refbase);

                        for (int i = 0; i < SocketCount; i++)
                        {
                            rrs[i] = temprv.Clone() as TF_Result;
                            rrs[i].SocketIndex = i;
                            rrs[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                            rrs[i].Status = TF_TestStatus.IDLE;  // No need initialzed in APx;

                            rrs[i].TestEnd += Rs_TestEnd;
                            rrs[i].Name = "REF";
                            rrs[i].AttachProperties.Add("Type", "REF");

                            rrs[i].TestEnd += Ref_TestEed;
                            rrs[i].StepDatas.SyncRun(Sequence.Spec.Limit, (x, y) => { if (y is AP_Limit apl) { x.Tag = apl.ChannelIndex; } });

                            var refdir = Path.Combine(refbase, SlotIndex.ToString());
                            if (!Directory.Exists(refdir)) Directory.CreateDirectory(refdir);
                        }

                        ResultsRef = rrs;
                    }
                    
                    Results = ResultsRef;

                    break;
                case ExecutionMode.Verification:
                    Sequence = Script.Sequences.FirstOrDefault(x => x.Name == ApxEngine.VerificationSequenceName) as Sequence;
                    Script.Activate(Sequence);

                    if(ResultsVer is null)
                    {
                        var rvs = new TF_Result[SocketCount];

                        var temprv = new TF_Result(Script.AnalyzeSpec());
                        temprv.SFCsConfig = Script.SystemConfig?.SFCs ?? new TestCore.Configuration.SFCsConfig(false, null, false);
                        temprv.StationConfig = Script.SystemConfig?.Station ?? new TestCore.Configuration.StationConfig("UKDC", "RD", "PRJ", "PRD", "SPL", "01");
                        temprv.GeneralConfig = Script.SystemConfig?.General ?? new TestCore.Configuration.GeneralConfig();
                        temprv.IsSFC = false;

                        var verbase = Script.GetVerificationBase();
                        if (!Directory.Exists(verbase)) Directory.CreateDirectory(verbase);

                        for (int i = 0; i < SocketCount; i++)
                        {
                            rvs[i] = temprv.Clone() as TF_Result;
                            rvs[i].SocketIndex = i;
                            rvs[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                            rvs[i].Status = TF_TestStatus.IDLE;  // No need initialzed in APx;

                            rvs[i].TestEnd += Rs_TestEnd;
                            rvs[i].Name = "VER";
                            rvs[i].AttachProperties.Add("Type", "VER");

                            rvs[i].StepDatas.SyncRun(Sequence.Spec.Limit, (x, y) => { if (y is AP_Limit apl) { x.Tag = apl.ChannelIndex; } });

                            var verdir = Path.Combine(verbase, SlotIndex.ToString());
                            if (!Directory.Exists(verdir)) Directory.CreateDirectory(verdir);
                        }

                        ResultsVer = rvs;
                    }

                    Results = ResultsVer;

                    break;
                default:
                    Sequence = Script.Sequences.FirstOrDefault(x => x.Name != ApxEngine.ReferenceSequenceName && x.Name != ApxEngine.VerificationSequenceName) as Sequence;
                    Script.Activate(Sequence);
                    Results = ResultsDut;
                    break;
            }

            ExecutionMode = mode;
            return 1;
        }


        // TODO, Support Loop Ref
        private void Ref_TestEed(object sender, EventArgs args)
        {
            if(sender is TF_Result rs)
            {
                if(rs.Status != TF_TestStatus.PASSED)
                {
                    // Clear Ref Folder
                    var refbase = Script.GetReferenceBase();
                    var refdir = Path.Combine(refbase, SlotIndex.ToString());
                    Directory.Delete(refdir, true);
                    Directory.CreateDirectory(refdir);
                }
            }
        }

        private void Ver_TestEed(object sender, EventArgs args)
        {
            if (sender is TF_Result rs)
            {
                if (rs.Status != TF_TestStatus.PASSED)
                {
                    // Clear Ref Folder
                    var verbase = Script.GetVerificationBase();
                    var verdir = Path.Combine(verbase, SlotIndex.ToString());
                    Directory.Delete(verdir, true);
                    Directory.CreateDirectory(verdir);
                }
            }
        }

        //private void ApplySignalSwitch(int slotindex)
        //{
        //    if (Script.SignalSwitch is null) return;
        //    var cnt = Script.SystemConfig?.General?.SocketCount ?? 1;
        //    if (slotindex < 0 || SlotIndex >= cnt) return;

        //    foreach (var item in Script.SignalSwitch)
        //    {
        //        var signalpath = ApxEngine.ApRef.Sequence.GetSignalPath(item.Name);
        //        if (!signalpath.Checked) continue;
        //        if (Enum.TryParse(item.Value[slotindex], out SingleInputChannelIndex ch))
        //        {
        //            for (int i = 0; i < signalpath.Count; i++)
        //            {
        //                if (signalpath[i].MeasurementType == MeasurementType.SignalPathSetup)
        //                {
        //                    if (signalpath[i].Checked)
        //                    {
        //                        signalpath[i].Show();

        //                        ApxEngine.ApRef.SignalPathSetup.AnalogInput.SingleInputChannel = ch;
        //                    }
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //}
    }
}
