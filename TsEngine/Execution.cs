using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Data;
using TestCore;
using TsEngine.UIs;
using NationalInstruments.TestStand.Interop.API;
using System.Windows.Forms;
using TestCore.Configuration;
using System.Runtime.CompilerServices;
using NationalInstruments.TestStand.Interop.AdapterAPI;
using ToucanCore.Abstraction.Engine;
using ToucanCore.Abstraction.HAL;

namespace TsEngine
{
    public class Execution : TF_Base, IExecution<Script>
    {
        internal static ToucanCore.Abstraction.Engine.IEngine StaticEngine { get; set; }
        internal NationalInstruments.TestStand.Interop.API.Execution TS_Execution { get; private set; }

        internal Execution(Script script)
        {
            if (script.Spec is null)
            {
                script.Spec = script.AnalyzeSpec();
                //script.Analyze();
            }

            Template = new TF_Result(script.Spec);
            Script = script;
            IsOriginalModel = script.IsOriginalModel;
            Template.SFCsConfig = script.SFCsConfig;
            Template.StationConfig = script.StationConfig;
            Template.GeneralConfig = script.SystemConfig?.General ?? new GeneralConfig();
            Template.IsSFC = script.SystemConfig?.SFCs?.EnableSfc ?? true;
            SocketCount = script.SystemConfig?.General?.SocketCount ?? 1;
            SequenceFile = script._SequenceFile;

            Workbase = Directory.GetParent(script.FilePath).FullName;
            // Do not init the Results for the Socket Count might be reconfig in Script when start Engine
        }

        public ToucanCore.Abstraction.Engine.IEngine Engine { get; } = StaticEngine;

        public IModel Model { get; }

        public string Name { get; private set; }  //TODO, not assigned
        public Script Script { get; private set; }
        public IScript GetScript() { return Script; }

        public ToucanCore.Abstraction.Engine.ISequence Sequence { get; private set; }

        public int SlotIndex { get; private set; } = 0;

        public bool IsForVerification { get; set; }
        public bool BreakOnFirstStep { get; set; }
        public bool BreakOnFailure { get; set; }
        public bool GotoCleanupOnFailure { get; set; }
        public bool DisableResults { get; set; }
        public int ActionOnError { get; set; }
        /// <summary>
        /// Make it setable for the setting might not match to reality
        /// </summary>
        public int SocketCount { get; internal set; }

        public Dictionary<string, object> Variables { get; private set; } = new Dictionary<string, object>();

        public TF_Result Template { get; private set; }

        public IReadOnlyList<TF_Result> Results { get; internal set; }
        public IReadOnlyList<TF_Result> ResultsDut { get; internal set; }
        public IReadOnlyList<TF_Result> ResultsRef { get; internal set; }
        public IReadOnlyList<TF_Result> ResultsVer { get; internal set; }

        public ModelType ModelType { get; private set; } = ModelType.None;

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

        //bool ContinueRun = true;

        internal NamedPipeQueueServer[] SlotBlockQueues;
        internal Queue<TF_Result>[] SlotReportQueues;  // For the Report might generate in async
        public SequenceContext[] SlotSequenceContexts { get; private set; }
        public NationalInstruments.TestStand.Interop.API.Execution[] SlotExecutions { get; internal set; }

        public NationalInstruments.TestStand.Interop.API.Execution HostExecution { get; internal set; }
        public NationalInstruments.TestStand.Interop.API.SequenceContext HostSequenceContext { get; internal set; }

        public bool IsSequentialModel { get; internal set; }
        public bool IsParallelModel { get; internal set; }
        public bool IsBatchModel { get; internal set; }
        //System.Threading.ManualResetEvent mre_NewTest = new System.Threading.ManualResetEvent(false);

        public void TrigPostUUTed(TF_Result e)
        {
            OnPostUUTed?.Invoke(this, e);
        }

        public void TrigPreUUTed(TF_Result e)
        {
            OnPreUUTed?.Invoke(this, e);
        }

        public int Abort()
        {
            TS_Execution.Abort();
            return 1;
        }

        public void Break()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            //mre_NewTest?.Dispose();
        }

        public void Resume()
        {
            TS_Execution.Resume();
        }

        public void StartTrigged(object sender, DutMessage message)
        {
            if (Results[message.SocketIndex].Status == TF_TestStatus.WAIT_DUT)
            {
                if (IsBatchModel || IsSequentialModel)
                {
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
                else
                {
                    int slotIndex = message.SocketIndex;
                    var sn = Results[slotIndex].SerialNumber;
                    SlotBlockQueues[slotIndex].Enqueue($"{slotIndex}:{sn}");
                    try
                    {
                        SlotBlockQueues[slotIndex].Close();

                    }
                    catch
                    { }
                    finally
                    {
                        SlotBlockQueues[slotIndex].Dispose();
                    }
                }
            }
            else
            {
                MessageBox.Show($"Invalid Trig. Status {Results[message.SocketIndex].Status}");
            }
        }

        public string PipeKey { get; } = Guid.NewGuid().ToString();
        public bool IsOriginalModel { get; private set; }
        
        NationalInstruments.TestStand.Interop.API.SequenceFile SequenceFile;
        public int Start()
        {
            if (TS_Execution is null)
            {
                GotoCleanupOnFailure = Engine.GotoCleanupOnFailure;
                ActionOnError = Engine.ActionOnError;
                BreakOnFirstStep = Engine.BreakOnFirstStep;
                BreakOnFailure = Engine.BreakOnFailure;
                DisableResults = Engine.DisableResults;
                IsForVerification = Engine.IsForVerification;

                try
                {
                    TS_Execution = TestStandHelper.TS_SeqFileViewMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_ExecutionEntryPoints_Set, 0).EntryPoint.Run();
                    
                    Info($"Start Execution {TS_Execution.DisplayName}. pipe key {PipeKey}");
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    // For TYM_Batch. to be compatible with default Batch, renamed the Test UUTs, which may trig this issue.
                    Warn(ex);
                    MessageBox.Show($"Start Module Failed, Need to RESTART Toucan. Err {ex.Message}", "Error", MessageBoxButtons.OK);
                    return -1;
                }

                var general = Script.SystemConfig.General ?? GlobalConfiguration.Default.General;

                SocketCount = general.SocketCount;

                GC.Collect();

                var modulefile = TestStandHelper.TS_AppMgr.GetEngine().StationOptions.StationModelSequenceFilePath;
                if (!IsOriginalModel)
                {
                    //Task SfcInitTask = null;
                    //if (Template.SFCsConfig.EnableSfc)
                    //{
                    //    SfcInitTask = Task.Run(() =>
                    //    {
                    //        MesInstance = Mes.MesManager.GetMesInstance(Template.StationConfig.Location, Template.StationConfig.Vendor);
                    //        if (Template.SFCsConfig.SfcsUploadData)
                    //        {
                    //            string data = null;
                    //            if (Template.SFCsConfig.SfcsDataMode.Equals("JDM", StringComparison.OrdinalIgnoreCase))
                    //            {
                    //                Template.GenerateSfcHeader_JDM(out data);
                    //            }
                    //            else
                    //            {
                    //                Template.GenerateSFCHeader(out data);
                    //            }

                    //            MesInstance.Initialize(Template.SFCsConfig, data);
                    //            Info($"SFCs_Column: {data}");
                    //        }
                    //        else
                    //        {
                    //            MesInstance.Initialize(Template.SFCsConfig, string.Empty);
                    //        }
                    //    }
                    //    );
                    //}

                    if (general.RunMode == RunMode.Batch)
                    {
                        if (!modulefile.Contains("Batch"))
                        {
                            throw new InvalidProgramException($"System.xml is in batch model. TestStand setting is {modulefile}");
                        }
                    }
                    else if (general.RunMode == RunMode.Parallel)
                    {
                        if (!modulefile.Contains("Parallel"))
                        {
                            throw new InvalidProgramException($"System.xml is in parallel model. TestStand setting is {modulefile}");
                        }
                    }
                    else if (modulefile.Contains("TYM"))
                    {
                        Warn($"TYM Model detected. {modulefile}");
                        //MessageBox.Show("You Are Using Toucan V0R2 Model, which is not suggested. Please Close Toucan, Swith to original model, and then Restart Toucan\r\n.当前model为V0R2所使用, 请关闭Toucan, 并切换为原生Model然后在重启Toucan");

                        throw new InvalidProgramException($"V0R2 Model Detected. Please Close Toucan and then set appropiate model in TestStand. Current is {modulefile}");
                    }
                    else
                    {
                        if (modulefile.Contains("Batch"))
                        {
                            IsBatchModel = true;
                            IsParallelModel = false;
                            IsSequentialModel = false;
                        }
                        else if (modulefile.Contains("Parallel"))
                        {
                            IsBatchModel = false;
                            IsParallelModel = true;
                            IsSequentialModel = false;
                        }
                        else
                        {
                            IsBatchModel = false;
                            IsParallelModel = false;
                            IsSequentialModel = true;
                        }
                    }

                    try
                    {
                        // Add Default Model Setting
                        NationalInstruments.TestStand.Interop.API.Sequence seq = null;
                        Step step = null;

                        seq = TestStandHelper.FetchOrCreateSequence(SequenceFile, "ModelOptions");
                        step = TestStandHelper.Engine.NewStep("None Adapter", "Statement");
                        step.Name = "TYM Injection in ModelOptions";
                        step.PostExpression = $"Parameters.ModelOptions.ParallelModel_ShowUUTDlg = False,Parameters.ModelOptions.BringUUTDlgToFrontOnChange = False, Parameters.ModelOptions.NumTestSockets={SocketCount}";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        seq = TestStandHelper.FetchOrCreateSequence(SequenceFile, "ReportOptions");
                        var rawpath = general.Raw_ReportPath.Replace("\\", "\\\\\\\\");
                        step = TestStandHelper.Engine.NewStep("None Adapter", "Statement");
                        step.Name = "TYM Injection in ReportOptions";
                        step.PostExpression = $"Parameters.ReportOptions.Format = \"xml\",Parameters.ReportOptions.IncludeArrayMeasurement = 1,Parameters.ReportOptions.NewFileNameForEachUUT = True,Parameters.ReportOptions.NewFileNameForEachTestSocket=True,Parameters.ReportOptions.StoreUUTReportWithBatchReport=False,Parameters.ReportOptions.DirectoryType=\"SpecifyByExpression\",Parameters.ReportOptions.ReportFileBatchModelExpression=Parameters.ReportOptions.ReportFileSequentialModelExpression=Parameters.ReportOptions.ReportFileParallelModelExpression=\"\\\"{rawpath}\\\\\\\\$(UUTPartNum)\\\\\\\\$(FileYear) $(FileMonth) $(FileDay)\\\\\\\\$(UUTStatus)\\\\\\\\$(UUT)_{Template.StationConfig.CustomerName}_{Template.StationConfig.ProjectName}_{Template.StationConfig.StationName}_$(FileYear) $(FileMonth) $(FileDay)_$(FileTime)_$(StationID)_$(TestSocket)_$(UUTStatus).$(FileExtension)\\\"\"";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        seq = TestStandHelper.FetchOrCreateSequence(SequenceFile, "PreUUTLoop");
                        step = TestStandHelper.Engine.NewStep("None Adapter", "Statement");
                        step.Name = "TYM Injection in PreUUTLoop";
                        step.PostExpression = $"Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.SequentialShowSerialNumber=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.SequentialShowStatus=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.ParallelModelUUTInfoDialog=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.BatchModelShowStatus=False,Parameters.ModelPluginConfiguration.RuntimeVariables.ModelDialogsEnabled.BatchModelGetNextUUTs=False";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        seq = TestStandHelper.FetchOrCreateSequence(SequenceFile, "PostMainSequence");
                        step = TestStandHelper.Engine.NewStep("None Adapter", "Statement");
                        step.Name = "TYM Update Defect Code";
                        step.PostExpression = $"Runstate.Thread.PostUIMessageEx(UIMsg_UserMessageBase + 10, RunState.TestSockets.MyIndex, Parameters.UUTStatus, ThisContext, True)";
                        seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                        string preuutstartid = string.Empty;
                        if (!general.CustomizeInputSn)
                        {
                            if (IsBatchModel)
                            {
                                seq = TestStandHelper.FetchOrCreateSequence(SequenceFile, "PreBatch");

                                step = TestStandHelper.Engine.NewStep("None Adapter", "CallExecutable");
                                step.Name = "TYM Injection in PreBatch_Construct Queue";
                                var queuefile = Path.Combine(AppContext.BaseDirectory, "Bin", "QueueClient.exe");
                                step.AsPropertyObject().SetValString("Executable", 0, queuefile);
                                step.AsPropertyObject().SetValString("ExecutableCalled", 0, queuefile);
                                step.AsPropertyObject().SetValString("Arguments", 0, $"\"{PipeKey}_Batch\"");
                                step.AsPropertyObject().SetValString("InitialWindowState", 0, "WINSTATE_HIDDEN");
                                step.IgnoreRTE = true;

                                //TODO, REF,VER

                                seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);
                            }
                            else
                            {
                                seq = TestStandHelper.FetchOrCreateSequence(SequenceFile, "PreUUT");

                                step = TestStandHelper.Engine.NewStep("None Adapter", "CallExecutable");
                                step.Name = "TYM Injection in PreUUT_Construct Queue";
                                var queuefile = Path.Combine(AppContext.BaseDirectory, "Bin", "QueueClient.exe");
                                step.AsPropertyObject().SetValString("Executable", 0, queuefile);
                                step.AsPropertyObject().SetValString("ExecutableCalled", 0, queuefile);
                                step.AsPropertyObject().SetValString("Arguments", 0, $"\"{PipeKey}_\"+Str(Parameters.UUT.TestSocketIndex)");
                                step.AsPropertyObject().SetValString("InitialWindowState", 0, "WINSTATE_HIDDEN");
                                step.IgnoreRTE = true;

                                seq.InsertStep(step, 0, StepGroups.StepGroup_Setup);

                                preuutstartid = step.UniqueStepId;

                                if (seq.Parameters.Exists("UUT.AdditionalData.Attach", 0))
                                {
                                    var props = seq.Parameters.GetSubProperties("UUT.AdditionalData.Attach", 0);

                                    foreach (var prop in props)
                                    {
                                        if (prop.Type.ValueType == PropertyValueTypes.PropValType_String)
                                        {
                                            Template.AttachProperties.Add(prop.Name, null);
                                        }
                                    }
                                }

                                ////TODO, REF,VER
                                //if (Script.SystemConfig.General.ReferencePeriod > 0)
                                //{
                                //    seq.Parameters.NewSubProperty("UUT.AdditionalData.IsRef", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                //    seq.Parameters.NewSubProperty("UUT.AdditionalData.IsVer", PropertyValueTypes.PropValType_Boolean, false, "", 0);

                                //    int startindex = seq.GetNumSteps(StepGroups.StepGroup_Main);
                                //    step = TestStandHelper.Engine.NewStep("Sequence Adapter", "SequenceCall");  // If Ref
                                //    step.Name = "Call Reference";
                                //    step.Precondition = "Parameters.UUT.AdditionalData.IsRef";

                                //    step.CustomActionExpression = "True";
                                //    step.CustomTrueAction = "GoTo";
                                //    step.CustomTrueActionTargetByExpr = $"\"{preuutstartid}\"";  // ID of TYM Injection;

                                //    SequenceCallModule scm = step.Module as SequenceCallModule;
                                //    if (scm != null)
                                //    {
                                //        scm.UseCurrentFile = true;
                                //        scm.SequenceName = TestStandEngine.ReferenceSequenceName;
                                //    }
                                //    seq.InsertStep(step, startindex, StepGroups.StepGroup_Main);

                                //    var refseq = TestStandHelper.FetchOrCreateSequence(SequenceFile, TestStandEngine.ReferenceSequenceName);
                                //    var refnotify = TestStandHelper.Engine.NewStep("None Adapter", "Statement");
                                //    refnotify.Name = "TYM Notify Ref Data";
                                //    refnotify.PostExpression = $"Runstate.Thread.PostUIMessageEx(UIMsg_UserMessageBase + 10, RunState.TestSockets.MyIndex, \"Ref\", ThisContext, True)";

                                //    var refseqcleanupcnt = refseq.GetNumSteps(StepGroups.StepGroup_Cleanup);
                                //    refseq.InsertStep(refnotify, refseqcleanupcnt, StepGroups.StepGroup_Cleanup);
                                //}

                                //if (Script.SystemConfig.General.VerificationPeriod > 0)
                                //{
                                //    int startindex = seq.GetNumSteps(StepGroups.StepGroup_Main);

                                //    seq.Locals.NewSubProperty("IsVer", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                //    step = TestStandHelper.Engine.NewStep("Sequence Adapter", "SequenceCall");  // If Ver
                                //    step.Name = "Call Verification";
                                //    step.Precondition = "Parameters.UUT.AdditionalData.IsVer";

                                //    step.CustomActionExpression = "True";
                                //    step.CustomTrueAction = "GoTo";
                                //    step.CustomTrueActionTargetByExpr = $"\"{preuutstartid}\"";  // ID of TYM Injection;

                                //    SequenceCallModule scm = step.Module as SequenceCallModule;
                                //    if (scm != null)
                                //    {
                                //        scm.UseCurrentFile = true;
                                //        scm.SequenceName = TestStandEngine.VerificationSequenceName;
                                //    }
                                //    seq.InsertStep(step, startindex, StepGroups.StepGroup_Main);

                                //    var verseq = TestStandHelper.FetchOrCreateSequence(SequenceFile, TestStandEngine.VerificationSequenceName);
                                //    var vernotify = TestStandHelper.Engine.NewStep("None Adapter", "Statement");
                                //    vernotify.Name = "TYM Notify Ver Data";
                                //    vernotify.PostExpression = $"Runstate.Thread.PostUIMessageEx(UIMsg_UserMessageBase + 10, RunState.TestSockets.MyIndex, \"Ver\", ThisContext, True)";

                                //    var refseqcleanupcnt = verseq.GetNumSteps(StepGroups.StepGroup_Cleanup);
                                //    verseq.InsertStep(vernotify, refseqcleanupcnt, StepGroups.StepGroup_Cleanup);
                                //}
                            }

                            if(Script.SystemConfig?.General?.ReferencePeriod > 0)
                            {
                                if (!SequenceFile.FileGlobalsDefaultValues.Exists("RefBase", 0))
                                {
                                    SequenceFile.FileGlobalsDefaultValues.NewSubProperty("RefBase", PropertyValueTypes.PropValType_String, false, null, 0);
                                }

                                SequenceFile.FileGlobalsDefaultValues.SetValString("RefBase", 0, Script.GetReferenceBase());
                            }

                            if (Script.SystemConfig?.General?.VerificationPeriod > 0)
                            {
                                if (!SequenceFile.FileGlobalsDefaultValues.Exists("VerBase", 0))
                                {
                                    SequenceFile.FileGlobalsDefaultValues.NewSubProperty("VerBase", PropertyValueTypes.PropValType_String, false, null, 0);
                                }

                                SequenceFile.FileGlobalsDefaultValues.SetValString("VerBase", 0, Script.GetVerificationBase());
                            }
                        }

                        if (string.IsNullOrEmpty(Template.StationConfig.StationID))
                        {
                            Template.StationConfig.StationID = "01";
                        }

                        TestStandHelper.Engine.StationOptions.StationID = TestStandHelper.Engine.StationID = Template.StationConfig.StationID;
                    }
                    catch
                    {
                    }

                    //if (Template.SFCsConfig.EnableSfc)
                    //{
                    //    try
                    //    {
                    //        SfcInitTask.Wait(3000);
                    //        if (!SfcInitTask.IsCompleted)
                    //        {
                    //            throw new InvalidOperationException($"Initialize SFCs Failed. Site {Template.StationConfig.Location}. Product {Template.SFCsConfig.Product}");
                    //        }
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Warn(ex);
                    //        throw ex;
                    //    }
                    //}
                }

                //foreach (var rs in Results)
                //{
                //    OnPreUUTLoop?.Invoke(this, rs);

                //    rs.TestEnd += Rs_TestEnd;
                //}

                Info($"Load Seq {SequenceFile.Path}. IsOriginalModel: {IsOriginalModel}, Model Path: {modulefile}");

                return 1;
            }
            else
            {
                TS_Execution.GetStates(out ExecutionRunStates runState, out ExecutionTerminationStates termState);
                
                if (runState == ExecutionRunStates.ExecRunState_Stopped)
                {
                    TS_Execution.RestartEx(0);
                }
                else if(runState == ExecutionRunStates.ExecRunState_Paused)
                {
                    TS_Execution.Resume();
                }
                return 1;
            }
        }

        // DO Not Register multiple time
        public void ActionOnInitialized()
        {
            if (IsSequentialModel)
            {
                ResultsDut = new TF_Result[1] { Template.Clone() as TF_Result };
                SlotBlockQueues = new NamedPipeQueueServer[1] { new NamedPipeQueueServer($"{PipeKey}_-1") };
                ResultsDut[0].SocketIndex = 0;
                ResultsDut[0].SocketId = "1";
                SlotBlockQueues[0].Initialize();
                SlotSequenceContexts = new SequenceContext[1];
                SlotReportQueues = new Queue<TF_Result>[1] { new Queue<TF_Result>() };
            }
            else if (IsBatchModel)
            {
                var rss = new TF_Result[SocketCount];
                SlotBlockQueues = new NamedPipeQueueServer[1];
                SlotSequenceContexts = new SequenceContext[SocketCount];
                SlotReportQueues = new Queue<TF_Result>[SocketCount];
                for (int i = 0; i < SocketCount; i++)
                {
                    rss[i] = Template.Clone() as TF_Result;
                    rss[i].SocketIndex = i;
                    //rss[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                    SlotReportQueues[i] = new Queue<TF_Result>();
                }

                if (SocketCount < 9)
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        rss[i].SocketId = $"{i + 1}";
                    }
                }
                else
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        rss[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                    }
                }

                ResultsDut = rss;
                SlotBlockQueues[0] = new NamedPipeQueueServer($"{PipeKey}_Batch");
                SlotBlockQueues[0].Initialize();
            }
            else if (IsParallelModel)
            {
                var rss = new TF_Result[SocketCount];
                SlotBlockQueues = new NamedPipeQueueServer[SocketCount];
                SlotReportQueues = new Queue<TF_Result>[SocketCount];
                SlotSequenceContexts = new SequenceContext[SocketCount];
                for (int i = 0; i < SocketCount; i++)
                {
                    rss[i] = Template.Clone() as TF_Result;
                    rss[i].SocketIndex = i;
                    //rss[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                    SlotBlockQueues[i] = new NamedPipeQueueServer($"{PipeKey}_{i}");
                    SlotBlockQueues[i].Initialize();
                    SlotReportQueues[i] = new Queue<TF_Result>();
                }

                if (SocketCount < 9)
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        rss[i].SocketId = $"{i + 1}";
                    }
                }
                else
                {
                    for (int i = 0; i < SocketCount; i++)
                    {
                        rss[i].SocketId = TF_Utility.DecToZnum_2Char(i + 1);
                    }
                }

                ResultsDut = rss;
            }

            foreach (var rs in ResultsDut)
            {
                rs.TestEnd += Rs_TestEnd;
            }

            Results = ResultsDut;
        }

        internal void TrigExecutionStarted()
        {
            ExecutionStarted?.Invoke(this, null);
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
            }
        }

        public void CustomizeInputSn(int slotindex)
        {
            OnUutIdentified?.Invoke(this, Results[slotindex]);
        }

        private void NotifyToContinue(int slotIndex, string sn)
        {
            DateTime t0 = DateTime.Now;
            while (!SlotBlockQueues[slotIndex].IsConnected)
            {
                System.Threading.Thread.Sleep(100);
                if (DateTime.Now.Subtract(t0).TotalSeconds > 30)
                {
                    MessageBox.Show($"Not Ready. Please try scan SN Later. SN {sn}, Slot {slotIndex}", "Warn");
                    return;
                }
            }

            SlotBlockQueues[slotIndex].Enqueue($"{slotIndex}:{sn}");
            try
            {
                SlotBlockQueues[slotIndex].Close();
            }
            catch
            { }
            finally
            {
                SlotBlockQueues[slotIndex].Dispose();
            }
        }

        private int NotifyToExit(int slot)
        {
            if (IsOriginalModel) return 1;
            if (Script.SystemConfig.General.CustomizeInputSn) return 1;
            //if (!IsRunning) return 1;
            if (slot >= 0)
            {
                switch (Results[slot].Status)
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
                        if (IsSequentialModel || IsParallelModel)
                        {
                            SlotSequenceContexts[slot].Parameters.SetValBoolean("ContinueTesting", 0, false);
                        }

                        if (IsBatchModel)
                        {
                            SlotSequenceContexts[slot].Parameters.SetValBoolean("ContinueTesting", 0, false);
                        }
                        break;
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
                    for (int i = 0; i < SocketCount; i++)
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

                                NotifyToContinue(i, string.Empty);
                                break;

                            default:
                                MessageBox.Show($"Slot {i + 1} is {Results[i].Status}, Could not stop");
                                break;
                        }
                    }
                }
            }

            return 1;
        }

        public int StartNewTest(int slotIndex = 0)
        {
            SlotIndex = slotIndex;
            TF_Result rs = Results[slotIndex];
            //rs.SerialNumber = sn;
            if (rs.SerialNumber is null) return -1;

            rs.Status = TF_TestStatus.WAIT_DUT;  // For the preview status might be Error

            OnUutIdentified?.Invoke(this, rs); 

            if (rs.Status == TF_TestStatus.ERROR)
            {
                return -1;
            }

            //rs.Begin(); // Begined in TestStand Event

            rs.SpecialData = null;
            rs.ExtValues = null;
            rs.AdditionalFiles = null;

            foreach (var item in Variables)
            {
                if (item.Value is string val)
                {
                    //ApxEngine.ApRef.Variables.SetUserDefinedVariable(item.Key, val);
                }
            }

            DateTime t0 = DateTime.Now;

            double TimeOut_WaitReport = 30;

            if (slotIndex >= 0)
            {
                if(Results[slotIndex].Status == TF_TestStatus.WAIT_DUT)
                {
                    while (SlotReportQueues[slotIndex].Count != 0 )  // For Prevent the New Test impact the last report SN
                    {
                        System.Threading.Thread.Sleep(50);

                        if(DateTime.Now.Subtract(t0).TotalSeconds > TimeOut_WaitReport)
                        {
                            Results[slotIndex].ErrorMessage = new ErrorMsg(-1, "the Last Test Report has not been generated, Please Check");
                            Results[slotIndex].Status = TF_TestStatus.ERROR;
                            return -1;
                        }
                    }

                    try
                    {
                        if (Results[slotIndex].SFCsConfig.EnableSfc)
                        {
                            SlotSequenceContexts[slotIndex].Locals.GetPropertyObject("UUT.AdditionalData", 0).SetValBoolean("IsSFC", 0, Results[slotIndex].IsSFC && Results[slotIndex].SerialNumber.Length > 2);
                        }

                        if (IsSequentialModel || IsParallelModel || IsBatchModel)
                        {
                            SlotSequenceContexts[slotIndex].Locals.SetValString("UUT.SerialNumber", 0, Results[slotIndex].SerialNumber);
                            SlotSequenceContexts[slotIndex].Locals.SetValString("UUT.PartNumber", 0, Results[slotIndex].PartNo);
                            //SlotSequenceContexts[slotIndex].Locals.SetValString("UUT.AdditionalData.Time", 0, TimeService.CurrentTime.ToString("yyyyMMddHHmmss"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Error(ex);
                    }

                    try
                    {
                        if (IsBatchModel)
                        {
                            if(Results.All(x=> x.Status == TF_TestStatus.WAIT_DUT || x.Status == TF_TestStatus.DISABLED))
                            {
                                NotifyToContinue(0, null);
                            }
                        }
                        else
                        {
                            NotifyToContinue(slotIndex, rs.SerialNumber);
                        }
                    }
                    catch
                    {
                        Warn($"Set SN Failed. {rs.SerialNumber}");
                        return -1;
                    }

                    return 1;
                }
                else
                {
                    Warn($"Try Start new test for slot {slotIndex} under {Results[slotIndex].Status}");
                }
            }
            else
            {
                if (IsBatchModel || IsSequentialModel)
                {
                    while (SlotReportQueues[0].Count != 0)
                    {
                        System.Threading.Thread.Sleep(50);

                        if (DateTime.Now.Subtract(t0).TotalSeconds > TimeOut_WaitReport)
                        {
                            Results[0].ErrorMessage = new ErrorMsg(-1, "the Last Test Report has not been generated, Please Check");
                            Results[0].Status = TF_TestStatus.ERROR;
                            return -1;
                        }
                    }

                    NotifyToContinue(0, rs.SerialNumber);

                    //DateTime t0 = DateTime.Now;
                    //while (!SlotBlockQueues[0].IsConnected)
                    //{
                    //    System.Threading.Thread.Sleep(100);
                    //    if (DateTime.Now.Subtract(t0).TotalSeconds > 30)
                    //    {
                    //        MessageBox.Show($"Not Ready. Please try scan SN Later. SN {rs.SerialNumber}, Slot {slotIndex}", "Warn");
                    //        return -1;
                    //    }
                    //}

                    //SlotBlockQueues[0].Enqueue(string.Empty);

                    //try
                    //{
                    //    SlotBlockQueues[0].Close();
                    //}
                    //catch
                    //{ }
                    //finally
                    //{
                    //    SlotBlockQueues[0].Dispose();
                    //}
                }
            }

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
            //ContinueRun = false;
            //mre_NewTest.Set();

            Info("StopExecution");

            NotifyToExit(-1);
            
            //foreach(var slot in SlotSequenceContexts)
            //{
            //    slot.Execution.Terminate();
            //}

            ExecutionStopped?.Invoke(this, null);

            TestStandHelper.TS_SeqFileViewMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_CloseCompletedExecutions, 0);

            return 1;
        }

        public int Terminate()
        {
            throw new NotImplementedException();
        }

        public void ShowVariables(int slot)
        {
            Variables_Browser browser = new Variables_Browser();
            Results[slot].TestEnd += (sender, args) =>
            {
                browser.Refresh(this, slot);
            };

            browser.Refresh(this, slot);
            browser.Show();
        }

        public int EnableSlot(int slotindex, bool status = true)
        {
            if (SocketCount > 1)
            {
                try
                {
                    if(ModelType == ModelType.Batch)
                    {
                        SlotSequenceContexts[slotindex].Parameters.SetValBoolean("TestSocket.Disabled", 0, !status);
                    }
                    else
                    {
                        SlotSequenceContexts[slotindex].Parameters.SetValBoolean("TestSocket.Running", 0, !status);
                    }
                }
                catch(Exception ex)
                {
                    string msg = string.Format("Enable/Disable Test Error. Seq Name: {0}. Stack : {1}. Err: {2}", SlotSequenceContexts[slotindex]?.Sequence?.Name, SlotSequenceContexts[slotindex]?.CallStackName, ex.Message);
                    Warn(msg);
                }
            }

            return 1;
        }

        //public void SetReferenceMode(bool statue = true)
        //{
        //    //ReferenceMode?.SetValBoolean("", 0, statue);
        //    foreach(var ctx in SlotSequenceContexts)
        //    {
        //        ctx.Locals.SetValBoolean("UUT.AdditionalData.IsRef", 0, statue);
        //    }
        //}

        //public void SetVerificationMode(bool statue = true)
        //{
        //    //VerificationMode?.SetValBoolean("", 0, statue);
        //    foreach (var ctx in SlotSequenceContexts)
        //    {
        //        ctx.Locals.SetValBoolean("UUT.AdditionalData.IsVer", 0, statue);
        //    }
        //}

        public ExecutionMode ExecutionMode { get; private set; } = ExecutionMode.Normal;
        public int SwitchExecutionMode(ExecutionMode mode)
        {
            if (Results.Any(x => x.Status == TF_TestStatus.TESTING))
            {
                throw new InvalidOperationException("Some of Slot are testing, Action Denied");
            }

            switch (mode)
            {
                case ExecutionMode.Normal:
                    foreach (var tsctx in SlotSequenceContexts)
                    {
                        //tsctx.Locals.SetValBoolean("UUT.AdditionalData.IsRef", 0, false);
                        //tsctx.Locals.SetValBoolean("UUT.AdditionalData.IsVer", 0, false);
                        tsctx.Parameters.SetValString("ModelData.ClientSequenceToRun", 0, TestStandEngine.MainSequenceName);
                    }
                    Results = ResultsDut;
                    break;
                case ExecutionMode.Reference:
                    if (Script.SystemConfig.General.ReferencePeriod > 0)
                    {
                        foreach(var tsctx in SlotSequenceContexts)
                        {
                            //tsctx.Locals.SetValBoolean("UUT.AdditionalData.IsRef", 0, true);
                            //tsctx.Locals.SetValBoolean("UUT.AdditionalData.IsVer", 0, false);
                            tsctx.Parameters.SetValString("ModelData.ClientSequenceToRun", 0, TestStandEngine.ReferenceSequenceName);
                        }

                        if(ResultsRef is null)
                        {
                            var rsref = new List<TF_Result>() { Capacity = SocketCount};

                            var spec = Script.AnalyzeSpec(TestStandEngine.ReferenceSequenceName);
                            for (int i = 0; i < SocketCount; i++)
                            {
                                var rs = new TF_Result(spec)
                                {
                                    StationConfig = Script.StationConfig,
                                    SFCsConfig = Script.SFCsConfig,
                                    SocketIndex = i,
                                    SocketId = TF_Utility.DecToZnum_2Char(i + 1),
                                    Status = TF_TestStatus.IDLE,
                                    Name = TestStandEngine.ReferenceSequenceName,
                                };
                                rs.IsSFC = false;
                                //rs.Properties.Add("REF", "REF");  // No Report Generated
                                rsref.Add(rs);
                            }
                            
                            ResultsRef = rsref;
                        }
                        Results = ResultsRef;
                    }
                    else
                    {
                        return -1;
                    }
                    break;

                case ExecutionMode.Verification:
                    if (Script.SystemConfig.General.VerificationPeriod > 0)
                    {
                        foreach (var tsctx in SlotSequenceContexts)
                        {
                            //tsctx.Locals.SetValBoolean("UUT.AdditionalData.IsRef", 0, false);
                            //tsctx.Locals.SetValBoolean("UUT.AdditionalData.IsVer", 0, true);
                            tsctx.Parameters.SetValString("ModelData.ClientSequenceToRun", 0, TestStandEngine.VerificationSequenceName);
                        }

                        if (ResultsVer is null)
                        {
                            var rsver = new List<TF_Result>() { Capacity = SocketCount };
                            var spec = Script.AnalyzeSpec(TestStandEngine.VerificationSequenceName);
                            for (int i = 0; i < SocketCount; i++)
                            {
                                var rs = new TF_Result(spec)
                                {
                                    StationConfig = Script.StationConfig,
                                    SFCsConfig = Script.SFCsConfig,
                                    SocketIndex = i,
                                    SocketId = TF_Utility.DecToZnum_2Char(i + 1),
                                    Status = TF_TestStatus.IDLE,
                                    Name = TestStandEngine.VerificationSequenceName,
                                };
                                rs.IsSFC = false;
                                //rs.AttachProperties.Add("VER", "VER");  // No Report Generated
                                
                                rsver.Add(rs);
                            }
                            ResultsVer = rsver;
                        }
                        Results = ResultsVer;
                    }
                    else
                    {
                        return -1;
                    }
                    break;

                default:
                    return 0;
            }
            ExecutionMode = mode;
            return 1;
        }

        public object GetVariable(string name)
        {
            throw new NotImplementedException();
        }

        public void SetVariable(string name, object value)
        {
            throw new NotImplementedException();
        }
    }
}
