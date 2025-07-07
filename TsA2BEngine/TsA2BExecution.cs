using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using TestCore;
using TestCore.Data;
using ToucanCore.Abstraction.Engine;
using TsEngine;
using TsEngine.UIs; 

namespace TsA2BEngine
{
    public class TsA2BExecution : TF_Base, IExecution<TsA2BScript>
    {
        public TsEngine.Execution TsExecution { get; private set; }

        public TsA2BScript Script { get; private set; }

        public IEngine Engine => TsA2BScript.StaticEngine;

        public IModel Model => throw new NotImplementedException();

        public string Name => TsExecution.Name;

        public int SlotIndex { get; private set; }

        public bool IsForVerification { get => ((IExecution)TsExecution).IsForVerification; set => ((IExecution)TsExecution).IsForVerification = value; }
        public bool BreakOnFirstStep { get => ((IExecution)TsExecution).BreakOnFirstStep; set => ((IExecution)TsExecution).BreakOnFirstStep = value; }
        public bool BreakOnFailure { get => ((IExecution)TsExecution).BreakOnFailure; set => ((IExecution)TsExecution).BreakOnFailure = value; }
        public bool GotoCleanupOnFailure { get => ((IExecution)TsExecution).GotoCleanupOnFailure; set => ((IExecution)TsExecution).GotoCleanupOnFailure = value; }
        public bool DisableResults { get => ((IExecution)TsExecution).DisableResults; set => ((IExecution)TsExecution).DisableResults = value; }
        public int ActionOnError { get => ((IExecution)TsExecution).ActionOnError; set => ((IExecution)TsExecution).ActionOnError = value; }

        public int SocketCount { get; private set; }

        public TF_Result Template { get; private set; }

        public IReadOnlyList<TF_Result> Results { get; private set; }

        public ModelType ModelType => ModelType.Batch;  // Batch could allow update the dut sn when batch does not start

        public string Workbase { get; private set; }

        public ExecutionMode ExecutionMode => throw new NotImplementedException();

        public event EventHandler ExecutionStarted;
        public event EventHandler ExecutionStopped;
        public event EventHandler<TF_Result> OnPreUUTLoop;
        public event EventHandler<TF_Result> OnPreUUTing;
        public event EventHandler<TF_Result> OnPreUUTed;
        public event EventHandler<TF_Result> OnUutIdentified;
        public event EventHandler<TF_Result> OnUutPassed;
        public event EventHandler<TF_Result> OnUutFailed;
        public event EventHandler<TF_Result> OnError;
        public event EventHandler<TF_Result> OnTestCompleted;
        public event EventHandler<TF_Result> OnPostUUTing;
        public event EventHandler<TF_Result> OnPostUUTed;
        public event EventHandler<TF_Result> OnPostUUTLoop;

        public int Abort() => TsExecution.Abort();

        public void Break() => TsExecution.Break();

        public TsA2BExecution(TsEngine.Execution tsExecution, TsA2BScript script)
        {
            TsExecution = tsExecution;
            Script = script;
            IsForVerification = tsExecution.IsForVerification;
            //SocketCount = TsExecution.SocketCount * Script.NodeCount;

            Template = new TF_Result(Script.Spec);
            Workbase = TsExecution.Workbase;

            //TsExecution.ExecutionStarted += TsExecution_ExecutionStarted;
        }

        public void Initialize()
        {
            if (Script.Spec is null)
            {
                Script.Spec = Script.AnalyzeSpec();
            }

            var spec = Script.Spec;

            SocketCount = TsExecution.SocketCount * Script.NodeCount;

            Template = new TF_Result(spec);
            Template.SFCsConfig = TsExecution.Template.SFCsConfig;
            Template.GeneralConfig = TsExecution.Template.GeneralConfig;
            Template.StationConfig = TsExecution.Template.StationConfig;
            Template.IsSFC = TsExecution.Template.IsSFC;

            var rss = new TF_Result[SocketCount];

            for (int i = 0; i < TsExecution.SocketCount; i++)
            {
                var tsrs = TsExecution.Results[i];

                tsrs.IsSFC = false;    // Skip TS Exec SFCs

                TsExecution.Results[i].TestStart += TsA2BExecution_TestStart;// (obj, ev) => { rss[i].Begin(); };
                TsExecution.Results[i].TestEnd += TsA2BExecution_TestEnd;
                TsExecution.Results[i].TestStatusChanged += TsA2BExecution_TestStatusChanged;
                for (int j = 0; j < Script.NodeCount; j++)
                {
                    var socketidx = i * Script.NodeCount + j;
                    rss[socketidx] = Template.Clone() as TF_Result;
                    rss[socketidx].SocketIndex = socketidx;
                    rss[socketidx].SocketId = TF_Utility.DecToZnum_2Char(i + 1); ;//TF_Utility.DecToZnum_2Char(socketidx);  // Not consider the socket count greate than 99
                }
            }

            Results = rss;
        }

        private void TsExecution_ExecutionStarted(object sender, EventArgs e)
        {
            if (sender is TsEngine.Execution tsexec)
            {
                if(Script.Spec is null)
                {
                    Script.Spec = Script.AnalyzeSpec();
                }

                var spec = Script.Spec;

                SocketCount = TsExecution.SocketCount * Script.NodeCount;

                Template = new TF_Result(spec);
                Template.SFCsConfig = tsexec.Template.SFCsConfig;
                Template.GeneralConfig = tsexec.Template.GeneralConfig;
                Template.StationConfig = tsexec.Template.StationConfig;
                Template.IsSFC = tsexec.Template.IsSFC;

                var rss = new TF_Result[SocketCount];

                for (int i = 0; i < tsexec.SocketCount; i++)
                {
                    var tsrs = tsexec.Results[i];

                    tsrs.IsSFC = false;    // Skip TS Exec SFCs

                    tsexec.Results[i].TestStart += TsA2BExecution_TestStart;// (obj, ev) => { rss[i].Begin(); };
                    tsexec.Results[i].TestEnd += TsA2BExecution_TestEnd;
                    tsexec.Results[i].TestStatusChanged += TsA2BExecution_TestStatusChanged;
                    for (int j = 0; j < Script.NodeCount; j++)
                    {
                        var socketidx = i * Script.NodeCount + j;
                        rss[socketidx] = Template.Clone() as TF_Result;
                        rss[socketidx].SocketIndex = socketidx;
                        rss[socketidx].SocketId = TF_Utility.DecToZnum_2Char(i + 1); ;//TF_Utility.DecToZnum_2Char(socketidx);  // Not consider the socket count greate than 99
                    }
                }

                Results = rss;
            }
        }

        private void TsA2BExecution_TestStatusChanged(object sender, TF_TestStatus status)
        {
            if (sender is TF_Result tsrs)
            {
                switch (status)
                {
                    case TestCore.TF_TestStatus.PASSED:
                    case TestCore.TF_TestStatus.FAILED:
                    case TestCore.TF_TestStatus.ABORT:
                    case TestCore.TF_TestStatus.ERROR:
                    case TestCore.TF_TestStatus.TERMINATED:
                        break;

                    default:
                        var initidx = tsrs.SocketIndex * Script.NodeCount;
                        for (int i = initidx;  i< initidx + Script.NodeCount; i++)
                        {
                            Results[i].Status = status;
                        }

                        break;
                }
            }
        }

        private void TsA2BExecution_TestEnd(object sender, EventArgs args)
        {
            if (sender is TF_Result tsrs)
            {
                var initidx = tsrs.SocketIndex * Script.NodeCount;

                //var seqctx = TsExecution.SlotSequenceContexts[initidx];

                if (tsrs.Status == TestCore.TF_TestStatus.PASSED)
                {
                    for (int i = initidx; i < initidx + Script.NodeCount; i++)
                    {
                        Results[i].End();
                    }
                }
                else if (tsrs.Status == TestCore.TF_TestStatus.FAILED)
                {
                    for (int i = initidx; i < initidx + Script.NodeCount; i++)
                    {
                        Results[i].End();
                    }
                }
                else
                {
                    for (int i = initidx; i < initidx + Script.NodeCount; i++)
                    {
                        Results[i].ErrorMessage = tsrs.ErrorMessage;
                        Results[i].End(tsrs.Status);
                    }
                }

                if (!string.IsNullOrEmpty(Script.SystemConfig.General.Raw_ReportPath))
                {
                    for (int i = initidx; i < initidx + Script.NodeCount; i++)
                    {
                        Engine.GenerateReport(Results[i], Script.SystemConfig.General.Raw_ReportPath);
                    }
                }

                for (int i = initidx; i < initidx + Script.NodeCount; i++)
                {
                    OnTestCompleted?.Invoke(this, Results[i]);
                }
            }
        }

        private void TsA2BExecution_TestStart(object sender, EventArgs args)
        {
            if (sender is TF_Result tsrs)
            {
                var initidx = tsrs.SocketIndex * Script.NodeCount;

                for (int i = initidx; i < initidx + Script.NodeCount; i++)
                {
                    Results[i].Begin();
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public int EnableSlot(int slotindex, bool status = true)
        {
            throw new NotImplementedException();
        }

        public IScript GetScript()
        {
            return Script;
        }

        public void Resume() => TsExecution.Resume();

        public void ShowVariables(int slot)
        {
            throw new NotImplementedException();
        }

        public int Start()
        {
            var rtn = TsExecution.Start();

            return rtn;
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

            var slotrow = slotIndex / Script.NodeCount;

            for (int i = slotrow * Script.NodeCount; i < (slotrow + 1) * Script.NodeCount; i++)
            {
                switch (Results[i].Status)
                {
                    case TF_TestStatus.WAIT_DUT:
                        break;


                    default:
                        return 0;
                }
            }

            TsExecution.Results[slotrow].SerialNumber = "tt";
            TsExecution.StartNewTest(slotrow);

            return 1;
        }

        public void StepIn() => TsExecution.StepIn();

        public void StepOut() => TsExecution.StepOut();

        public void StepOver() => TsExecution.StepOver();

        public int Stop() => TsExecution.Stop();

        public int Terminate() => TsExecution.Terminate();

        public int SwitchExecutionMode(ExecutionMode mode)
        {
            throw new NotImplementedException();
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
