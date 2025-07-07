using ApEngineManager;
using NationalInstruments.TestStand.Interop.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.Abstraction.Engine;
using TsEngine;

namespace TsApEngine
{
    public class TsApExecution : IExecution<TsApScript>
    {
        internal TsEngine.Execution TsExecution { get; }
        internal IExecution ApExecution { get; }

        public TsApScript Script { get; private set; }

        public ToucanCore.Abstraction.Engine.IEngine Engine => ((IExecution)TsExecution).Engine;

        public IModel Model => ((IExecution)TsExecution).Model;

        public string Name => ((IExecution)TsExecution).Name;

        public int SlotIndex { get; set; }

        public bool IsForVerification { get => ((IExecution)TsExecution).IsForVerification; set => ((IExecution)TsExecution).IsForVerification = value; }
        public bool BreakOnFirstStep { get => ((IExecution)TsExecution).BreakOnFirstStep; set => ((IExecution)TsExecution).BreakOnFirstStep = value; }
        public bool BreakOnFailure { get => ((IExecution)TsExecution).BreakOnFailure; set => ((IExecution)TsExecution).BreakOnFailure = value; }
        public bool GotoCleanupOnFailure { get => ((IExecution)TsExecution).GotoCleanupOnFailure; set => ((IExecution)TsExecution).GotoCleanupOnFailure = value; }
        public bool DisableResults { get => ((IExecution)TsExecution).DisableResults; set => ((IExecution)TsExecution).DisableResults = value; }
        public int ActionOnError { get => ((IExecution)TsExecution).ActionOnError; set => ((IExecution)TsExecution).ActionOnError = value; }

        public int SocketCount => ((IExecution)TsExecution).SocketCount;

        public TF_Result Template { get; private set; }

        public IReadOnlyList<TF_Result> Results { get; private set; }

        public ModelType ModelType => ((IExecution)TsExecution).ModelType;

        public string Workbase => ((IExecution)TsExecution).Workbase;

        public ExecutionMode ExecutionMode => throw new NotImplementedException();

        public event EventHandler ExecutionStarted
        {
            add
            {
                ((IExecution)TsExecution).ExecutionStarted += value;
            }

            remove
            {
                ((IExecution)TsExecution).ExecutionStarted -= value;
            }
        }

        public event EventHandler ExecutionStopped
        {
            add
            {
                ((IExecution)TsExecution).ExecutionStopped += value;
            }

            remove
            {
                ((IExecution)TsExecution).ExecutionStopped -= value;
            }
        }

        public event EventHandler<TF_Result> OnPreUUTLoop
        {
            add
            {
                ((IExecution)TsExecution).OnPreUUTLoop += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnPreUUTLoop -= value;
            }
        }

        public event EventHandler<TF_Result> OnPreUUTing
        {
            add
            {
                ((IExecution)TsExecution).OnPreUUTing += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnPreUUTing -= value;
            }
        }

        public event EventHandler<TF_Result> OnPreUUTed
        {
            add
            {
                ((IExecution)TsExecution).OnPreUUTed += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnPreUUTed -= value;
            }
        }

        public event EventHandler<TF_Result> OnUutIdentified
        {
            add
            {
                ((IExecution)TsExecution).OnUutIdentified += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnUutIdentified -= value;
            }
        }

        public event EventHandler<TF_Result> OnUutPassed
        {
            add
            {
                ((IExecution)TsExecution).OnUutPassed += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnUutPassed -= value;
            }
        }

        public event EventHandler<TF_Result> OnUutFailed
        {
            add
            {
                ((IExecution)TsExecution).OnUutFailed += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnUutFailed -= value;
            }
        }

        public event EventHandler<TF_Result> OnError
        {
            add
            {
                ((IExecution)TsExecution).OnError += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnError -= value;
            }
        }

        public event EventHandler<TF_Result> OnTestCompleted;

        public event EventHandler<TF_Result> OnPostUUTing
        {
            add
            {
                ((IExecution)TsExecution).OnPostUUTing += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnPostUUTing -= value;
            }
        }

        public event EventHandler<TF_Result> OnPostUUTed
        {
            add
            {
                ((IExecution)TsExecution).OnPostUUTed += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnPostUUTed -= value;
            }
        }

        public event EventHandler<TF_Result> OnPostUUTLoop
        {
            add
            {
                ((IExecution)TsExecution).OnPostUUTLoop += value;
            }

            remove
            {
                ((IExecution)TsExecution).OnPostUUTLoop -= value;
            }
        }

        public TsApExecution(TsApScript tsapscript, string sequencename=null)
        {
            Script = tsapscript;

            ApExecution = TsApHybird.Apx.CreateExecution(tsapscript.ApScript, sequencename);
            TsExecution = TsApHybird.TestStand.CreateExecution(tsapscript.TsScript, sequencename) as TsEngine.Execution;

            if(Script.SystemConfig.General.CustomizeInputSn)
            {
                TsExecution.OnUutIdentified += TsExecution_OnUutIdentified;
            }
            
            TsExecution.ExecutionStarted += TsExecution_ExecutionStarted;

            ApExecution.OnPostUUTed += ApExecution_OnPostUUTed;
        }

        private void ApExecution_OnPostUUTed(object sender, TF_Result e)
        {
            var syncupvar = ClientSequenceCotext.FileGlobals.GetPropertyObject(TsApScript.ApVarSyncUp, 0);
            for (int i = 0; i < Script.DefaultVariable.Count; i++)
            {
                var name = Script.DefaultVariable.Keys.ElementAt(i);
                var val = ApExecution.GetVariable(name) as string;

                syncupvar.SetValString(name, 0, val);
            }

            if (ClientSequenceCotext.FileGlobals.GetPropertyObject(TsApScript.ApVarConclusion, 0) is PropertyObject apconclusionvar)
            {
                apconclusionvar.SetValString("", 0, e.Status.ToString());
            }

            if (e.Status == TestCore.TF_TestStatus.ERROR)
            {
                Results[e.SocketIndex].ErrorMessage = e.ErrorMessage;
            }

            TsExecution.SlotExecutions[e.SocketIndex].Resume();
        }

        private void TsExecution_OnUutIdentified(object sender, TF_Result e)
        {
            e.IsSFC = Results[e.SocketIndex].IsSFC;  // For customer input sn, sync ts status with UI
            Results[e.SocketIndex].SerialNumber = e.SerialNumber;
        }

        internal void TsExecution_ExecutionCreated(object sender, EventArgs e)
        {
            if (sender is TsEngine.Execution tsexec)
            {
                var spec = new TF_Spec(tsexec.Template.Specification.Name, tsexec.Template.TestSoftwareVersion);

                foreach (var item in tsexec.Template.Specification.Limit)
                {
                    spec.Limit.Add(item);
                }

                foreach (var item in ApExecution.Template.Specification.Limit)
                {
                    spec.Limit.Add(item);
                }

                Script.Spec = spec;

                Template = new TF_Result(spec);
                Template.SFCsConfig = tsexec.Template.SFCsConfig;
                Template.GeneralConfig = tsexec.Template.GeneralConfig;
                Template.StationConfig = tsexec.Template.StationConfig;
                Template.IsSFC = tsexec.Template.IsSFC;

                var rss = new TF_Result[SocketCount];

                for (int i = 0; i < SocketCount; i++)
                {
                    rss[i] = Template.Clone() as TF_Result;
                    rss[i].StepDatas.Clear();

                    foreach (var item in tsexec.Results[i].StepDatas)
                    {
                        rss[i].StepDatas.Add(item);
                    }

                    foreach (var item in ApExecution.Results[i].StepDatas)
                    {
                        rss[i].StepDatas.Add(item);
                    }

                    //rss[i].SocketId = $"{i + 1}";
                    rss[i].SocketIndex = i;

                    tsexec.Results[i].TestStart += TsApExecution_TestStart;// (obj, ev) => { rss[i].Begin(); };
                    tsexec.Results[i].TestEnd += TsApExecution_TestEnd;
                    tsexec.Results[i].TestStatusChanged += TsApExecution_TestStatusChanged;
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

                Results = ResultsDut = rss;
            }
        }

        private void TsExecution_ExecutionStarted(object sender, EventArgs e)
        {
        }

        private void TsApExecution_TestStatusChanged(object sender, TestCore.TF_TestStatus status)
        {
            if (sender is TF_Result tsrs)
            {
                switch(status)
                {
                    case TestCore.TF_TestStatus.PASSED:
                    case TestCore.TF_TestStatus.FAILED:
                    case TestCore.TF_TestStatus.ABORT:
                    case TestCore.TF_TestStatus.ERROR:
                    case TestCore.TF_TestStatus.TERMINATED:
                        break;

                    default:
                        Results[tsrs.SocketIndex].Status = status;
                        break;
                }
            }
        }

        private void TsApExecution_TestStart(object sender, EventArgs args)
        {
            if (sender is TF_Result tsrs)
            {
                Results[tsrs.SocketIndex].Begin();
            }
        }

        private void TsApExecution_TestEnd(object sender, EventArgs args)
        {
            if (sender is TF_Result tsrs)
            {
                if (tsrs.Status == TestCore.TF_TestStatus.PASSED)
                {
                    if (ApExecution.Results[tsrs.SocketIndex].Status == TestCore.TF_TestStatus.PASSED)
                    {
                        Results[tsrs.SocketIndex].End();
                    }
                    else if (ApExecution.Results[tsrs.SocketIndex].Status == TestCore.TF_TestStatus.ERROR)
                    {
                        Results[tsrs.SocketIndex].ErrorMessage = ApExecution.Results[tsrs.SocketIndex].ErrorMessage;
                        Results[tsrs.SocketIndex].End(TestCore.TF_TestStatus.ERROR);
                    }
                    else
                    {
                        Results[tsrs.SocketIndex].End(ApExecution.Results[tsrs.SocketIndex].Status);
                    }
                }
                else if (tsrs.Status == TestCore.TF_TestStatus.FAILED)
                {
                    Results[tsrs.SocketIndex].End();
                }
                else
                {
                    Results[tsrs.SocketIndex].ErrorMessage = tsrs.ErrorMessage;
                    Results[tsrs.SocketIndex].End(tsrs.Status);
                }

                Results[tsrs.SocketIndex].SpecialData = tsrs.SpecialData;
                Results[tsrs.SocketIndex].ExtValues = tsrs.ExtValues;
                Results[tsrs.SocketIndex].ExtColumns = tsrs.ExtColumns;

                OnTestCompleted?.Invoke(this, Results[tsrs.SocketIndex]);
            }
        }

        public int Abort()
        {
            return ((IExecution)TsExecution).Abort();
        }

        public void Break()
        {
            ((IDebugable)TsExecution).Break();
        }

        public void Dispose()
        {
            ((IDisposable)TsExecution).Dispose();
        }

        public int EnableSlot(int slotindex, bool status = true)
        {
            return ((IExecution)TsExecution).EnableSlot(slotindex, status);
        }

        public IScript GetScript()
        {
            return Script;
        }

        public void Resume()
        {
            ((IDebugable)TsExecution).Resume();
        }

        public void ShowVariables(int slot)
        {
            ((IExecution)TsExecution).ShowVariables(slot);
        }

        public int Start()
        {
            TsExecution.Start();
            ApExecution.Start();
            return 1;
        }

        public int StartNewTest(int slotIndex = 0)
        {
            ApExecution.Results[slotIndex].SerialNumber = TsExecution.Results[slotIndex].SerialNumber = Results[slotIndex].SerialNumber;
            ApExecution.Results[slotIndex].IsSFC = TsExecution.Results[slotIndex].IsSFC = Results[slotIndex].IsSFC;
            ApExecution.Results[slotIndex].PartNo = TsExecution.Results[slotIndex].PartNo = Results[slotIndex].PartNo;
            ApExecution.Results[slotIndex].LineNo = TsExecution.Results[slotIndex].LineNo = Results[slotIndex].LineNo;
            return ((IExecution)TsExecution).StartNewTest(slotIndex);
        }

        public void StepIn()
        {
            ((IDebugable)TsExecution).StepIn();
        }

        public void StepOut()
        {
            ((IDebugable)TsExecution).StepOut();
        }

        public void StepOver()
        {
            ((IDebugable)TsExecution).StepOver();
        }

        public int Stop()
        {
            TsExecution.Stop();
            ApExecution.Stop();
            return 1;
        }

        public int Terminate()
        {
            return ((IExecution)TsExecution).Terminate();
        }

        public IReadOnlyList<TF_Result> ResultsRef;
        public IReadOnlyList<TF_Result> ResultsVer;
        public IReadOnlyList<TF_Result> ResultsDut;

        public int SwitchExecutionMode(ExecutionMode mode)
        {
            if (Results.Any(x => x.Status == TF_TestStatus.TESTING))
            {
                throw new InvalidOperationException("Some of Slot are testing, Action Denied");
            }

            TsExecution.SwitchExecutionMode(mode);
            ApExecution.SwitchExecutionMode(mode);

            switch (mode)
            {
                case ExecutionMode.Normal:
                    Results = ResultsDut;
                    break;
                case ExecutionMode.Reference:
                    if (Script.SystemConfig.General.ReferencePeriod > 0)
                    {
                        if (ResultsRef is null)
                        {
                            var tsresult = TsExecution.Results.FirstOrDefault();
                            var apresult = ApExecution.Results.FirstOrDefault();
                            var spec = new TF_Spec(tsresult.Specification.Name, tsresult.TestSoftwareVersion);

                            foreach (var item in tsresult.Specification.Limit)
                            {
                                spec.Limit.Add(item);
                            }

                            foreach (var item in apresult.Specification.Limit)
                            {
                                spec.Limit.Add(item);
                            }

                            //Script.Spec = spec;

                            Template = new TF_Result(spec);
                            Template.SFCsConfig = tsresult.SFCsConfig;
                            Template.GeneralConfig = tsresult.GeneralConfig;
                            Template.StationConfig = tsresult.StationConfig;
                            Template.IsSFC = tsresult.IsSFC;
                            Template.Name = "REF";

                            var rss = new TF_Result[SocketCount];

                            for (int i = 0; i < SocketCount; i++)
                            {
                                rss[i] = Template.Clone() as TF_Result;
                                rss[i].StepDatas.Clear();

                                foreach (var item in TsExecution.Results[i].StepDatas)
                                {
                                    rss[i].StepDatas.Add(item);
                                }

                                foreach (var item in ApExecution.Results[i].StepDatas)
                                {
                                    rss[i].StepDatas.Add(item);
                                }

                                rss[i].SocketId = $"{i + 1}";
                                rss[i].SocketIndex = i;

                                TsExecution.Results[i].TestStart += TsApExecution_TestStart;// (obj, ev) => { rss[i].Begin(); };
                                TsExecution.Results[i].TestEnd += TsApExecution_TestEnd;
                                TsExecution.Results[i].TestStatusChanged += TsApExecution_TestStatusChanged;
                            }

                            ResultsRef = rss;
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
                        if (ResultsVer is null)
                        {
                            var tsresult = TsExecution.Results.FirstOrDefault();
                            var apresult = ApExecution.Results.FirstOrDefault();
                            var spec = new TF_Spec(tsresult.Specification.Name, tsresult.TestSoftwareVersion);

                            foreach (var item in tsresult.Specification.Limit)
                            {
                                spec.Limit.Add(item);
                            }

                            foreach (var item in apresult.Specification.Limit)
                            {
                                spec.Limit.Add(item);
                            }

                            //Script.Spec = spec;

                            Template = new TF_Result(spec);
                            Template.SFCsConfig = tsresult.SFCsConfig;
                            Template.GeneralConfig = tsresult.GeneralConfig;
                            Template.StationConfig = tsresult.StationConfig;
                            Template.IsSFC = tsresult.IsSFC;
                            Template.Name = "VER";

                            var rss = new TF_Result[SocketCount];

                            for (int i = 0; i < SocketCount; i++)
                            {
                                rss[i] = Template.Clone() as TF_Result;
                                rss[i].StepDatas.Clear();

                                foreach (var item in TsExecution.Results[i].StepDatas)
                                {
                                    rss[i].StepDatas.Add(item);
                                }

                                foreach (var item in ApExecution.Results[i].StepDatas)
                                {
                                    rss[i].StepDatas.Add(item);
                                }

                                rss[i].SocketId = $"{i + 1}";
                                rss[i].SocketIndex = i;

                                TsExecution.Results[i].TestStart += TsApExecution_TestStart;// (obj, ev) => { rss[i].Begin(); };
                                TsExecution.Results[i].TestEnd += TsApExecution_TestEnd;
                                TsExecution.Results[i].TestStatusChanged += TsApExecution_TestStatusChanged;
                            }

                            ResultsVer = rss;
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
            //ExecutionMode = mode;
            return 1;
        }

        SequenceContext ClientSequenceCotext = null;
        public void TrigApStart(NationalInstruments.TestStand.Interop.API.SequenceContext sc, int socketindex)
        {
            ClientSequenceCotext = sc;
            var syncupvar = sc.FileGlobals.GetPropertyObject(TsApScript.ApVarSyncUp, 0);

            for (int i = 0; i < Script.DefaultVariable.Count; i++)
            {
                var name = Script.DefaultVariable.Keys.ElementAt(i);
                var val = syncupvar.GetValString(name, 0);

                ApExecution.SetVariable(name, val);
            }

            if (ClientSequenceCotext.FileGlobals.GetPropertyObject(TsApScript.ApVarConclusion, 0) is PropertyObject apconclusionvar)
            {
                apconclusionvar.SetValString("", 0, string.Empty);
            }

            var rs = Results[socketindex];
            var aprs = ApExecution.Results[rs.SocketIndex];
            aprs.SerialNumber = rs.SerialNumber;
            aprs.SocketIndex = rs.SocketIndex;
            TsExecution.SlotExecutions[rs.SocketIndex].Break();
            ApExecution.StartNewTest(socketindex);
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
