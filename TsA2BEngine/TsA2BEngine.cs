using NationalInstruments.TestStand.Interop.API;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using ToucanCore.Abstraction.Engine;
using TsEngine;

namespace TsA2BEngine
{
    public class TsA2BEngine : TF_Base, IEngine<TsA2BExecution, TsA2BScript>
    {
        internal static TsEngine.TestStandEngine TestStand { get; } = new TestStandEngine();

        public IReadOnlyCollection<TsA2BScript> Scripts {get; private set;}

        public const string ConstName = "TestStand A2B Framework";

        public string Name => ConstName;

        public string Version => "1.0";

        public string UserName => TestStand.Name;

        public string StationId { get => TestStand.StationId; set => TestStand.StationId = value; }

        public string FileFilter => "TestStand A2B Framework Sequence|*.tsab";

        public bool IsInitialized => TestStand.IsInitialized;

        public bool IsStarted => TestStand.IsStarted;

        public bool IsForVerification { get => TestStand.IsForVerification; set => TestStand.IsForVerification = value; }

        public bool BreakOnFirstStep => TestStand.BreakOnFirstStep;

        public bool BreakOnFailure => TestStand.BreakOnFailure;

        public bool GotoCleanupOnFailure => TestStand.GotoCleanupOnFailure;

        public bool DisableResults => TestStand.DisableResults;

        public int ActionOnError => TestStand.ActionOnError;

        public IReadOnlyDictionary<string, object> Variables => TestStand.Variables;

        public IModel Model => TestStand.Model;

        public bool UiVisible 
        {
            get => TestStand.UiVisible; set => TestStand.UiVisible = value;
        }

        public bool IsEditMode
        {
            get => TestStand.IsEditMode; set => TestStand.IsEditMode = value;
        }

        private List<TsA2BExecution> _Executions = new List<TsA2BExecution>();
        public event EventHandler<IScript> OnScriptOpened;
        public event EventHandler<IExecution> OnExecutionCreated;
        public event EventHandler<IExecution> OnExecutionStarted;
        public event EventHandler<IExecution> OnExecutionStopped;

        public event EventHandler OnEngineInitialized
        {
            add
            {
                TestStand.OnEngineInitialized += value;
            }

            remove
            {
                TestStand.OnEngineInitialized -= value;
            }
        }

        public event EventHandler OnEngineStarted
        {
            add
            {
                TestStand.OnEngineStarted += value;
            }

            remove
            {
                TestStand.OnEngineStarted -= value;
            }
        }

        public event EventHandler OnEngineStopped
        {
            add
            {
                TestStand.OnEngineStopped += value;
            }

            remove
            {
                TestStand.OnEngineStopped -= value;
            }
        }

        public event EventHandler<Tuple<TF_Result, string>> OnReportGenerated;

        public event EventHandler CalibrationExpired
        {
            add
            {
                TestStand.CalibrationExpired += value;
            }

            remove
            {
                TestStand.CalibrationExpired -= value;
            }
        }

        public event EventHandler CalibrationExpiring
        {
            add
            {
                TestStand.CalibrationExpiring += value;
            }

            remove
            {
                TestStand.CalibrationExpiring -= value;
            }
        }

        public IReadOnlyCollection<TsA2BExecution> Executions => _Executions;

        public int Login(string username, string password)=>TestStand.Login(username, password);

        public int Initialize()
        {
            TsA2BScript.StaticEngine = this;
            TestStand.Initialize();

            TestStand.OnExecutionCreated += TestStand_OnExecutionCreated;
            TestStand.OnExecutionStarted += TestStand_OnExecutionStarted;
            return 1;
        }

        private void TestStand_OnExecutionCreated(object sender, IExecution e)
        {
            if(_Executions.FirstOrDefault(x => x.TsExecution == e) is TsA2BExecution a2bexec)
            {
                a2bexec.Initialize();
            }

            OnExecutionCreated?.Invoke(this, Executions.FirstOrDefault(x => x.TsExecution == e));
        }

        private void TestStand_OnExecutionStarted(object sender, IExecution e)
        {
            OnExecutionStarted?.Invoke(this, Executions.FirstOrDefault(x => x.TsExecution == e));
        }

        public int StartEngine()
        {
            TestStandHelper.TS_AppMgr.AfterUIMessageEvent += TS_AppMgr_AfterUIMessageEvent;
            var rtn = TestStand.StartEngine();
            return rtn;
        }

        private void TS_AppMgr_AfterUIMessageEvent(object sender, NationalInstruments.TestStand.Interop.UI.Ax._ApplicationMgrEvents_AfterUIMessageEventEvent e)
        {
            switch (e.uiMsg.Event)
            {
                case NationalInstruments.TestStand.Interop.API.UIMessageCodes.UIMsg_UserMessageBase + 10:
                    if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                    {
                        var exec = Executions.FirstOrDefault();
                        if (exec is null) return;
                        
                        var slotrow = (int)e.uiMsg.NumericData;

                        if(tempsc.FileGlobals.Exists(TsA2BScript.VariantName,0))
                        {
                            var itemdatas_ts = tempsc.FileGlobals.GetPropertyObjectElements($"{TsA2BScript.VariantName}.{TsA2BScript.VarTestData}", 0);

                            for (int i = 0; i < exec.Template.StepDatas.Count; i++)
                            {
                                if (exec.Template.StepDatas[i].Element is TF_ItemData itemdata)
                                {
                                    if (itemdata.Limit.Comp == Comparison.NULL)  // LoopTest
                                    {
                                        var data = itemdatas_ts[i].GetValVariant("ItemValue", 0);

                                        if (data is double[,] d2array)
                                        {
                                            var nodecount = Math.Min(exec.Script.NodeCount, d2array.GetLength(0));
                                            var arrayd2 = d2array.GetLength(1);

                                            for (int idx = 0; idx < nodecount; idx++)
                                            {
                                                var step = exec.Results[slotrow * exec.Script.NodeCount + idx].StepDatas[i];

                                                var row = Math.Min(step.Count, arrayd2);  // is Column, for the teststand the array is transposed

                                                if (row > 0) 
                                                { 
                                                    step.Element.Begin();
                                                    step.Element.StartTime = DateTime.Now;
                                                }

                                                for (int idxsub = 0; idxsub < row; idxsub++)
                                                {
                                                    if (step[idxsub].Element is TF_ItemData iteminresult)
                                                    {
                                                        iteminresult.Begin();
                                                        iteminresult.StartTime = DateTime.Now;
                                                        iteminresult.SetValue(d2array[idx, idxsub]);
                                                        iteminresult.EndTime = DateTime.Now;
                                                    }
                                                }

                                                if (row > 0)
                                                {
                                                    step.Element.EndTime = DateTime.Now;
                                                }
                                            }
                                        }
                                    }
                                    else if(itemdata.Limit.LimitType == typeof(double))
                                    {
                                        var data = itemdatas_ts[i].GetValVariant("ItemValue", 0);

                                        if (data is double[,] d2array)
                                        {
                                            var nodecount = Math.Min(exec.Script.NodeCount, d2array.GetLength(0));
                                            for (int idx = 0; idx < nodecount; idx++)
                                            {
                                                if(exec.Results[slotrow * exec.Script.NodeCount + idx].StepDatas[i].Element is TF_ItemData iteminresult)
                                                {
                                                    iteminresult.Begin();
                                                    iteminresult.StartTime = DateTime.Now;
                                                    iteminresult.SetValue(d2array[idx, 0]);
                                                    iteminresult.EndTime = DateTime.Now;
                                                }
                                            }
                                        }
                                    }
                                    else if (itemdata.Limit.LimitType == typeof(string))
                                    {
                                        var data = itemdatas_ts[i].GetValVariant("ItemStr", 0);

                                        if (data is string[,] d2array)
                                        {
                                            var nodecount = Math.Min(exec.Script.NodeCount, d2array.GetLength(0));
                                            for (int idx = 0; idx < nodecount; idx++)
                                            {
                                                if (exec.Results[slotrow * exec.Script.NodeCount + idx].StepDatas[i].Element is TF_ItemData iteminresult)
                                                {
                                                    iteminresult.Begin();
                                                    iteminresult.StartTime = DateTime.Now;
                                                    iteminresult.SetValue(d2array[idx, 0]);
                                                    iteminresult.EndTime = DateTime.Now;
                                                }
                                            }
                                        }
                                    }
                                }

                            }

                            if(tempsc.FileGlobals.GetValVariant($"{TsA2BScript.VariantName}.BARCODE_PART", 0) is string[] barcodeparts)
                            {
                                for (int i = 0; i < barcodeparts.Length; i++)
                                {
                                    exec.Results[i].SpecialData = barcodeparts[i];
                                }
                            }
                        }
                    }

                    break;
            }
        }

        public int StopEngine() => TestStand.StopEngine();

        public static string CalibrationBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Calibration");
        public static string ReferenceBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Reference");
        public static string VerificationBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Verification");

        public IScript LoadScriptFile(string path)
        {
            if (Executions?.Count > 0)
            {
                foreach (var exec in Executions)
                {
                    exec.Stop();
                }

                _Executions.Clear();
            }

            TsA2BScript script = new TsA2BScript();
            script.Open(path);
            OnScriptOpened?.Invoke(this, script);
            return script;
        }

        public IScript NewScript(GlobalConfiguration config = null)
        {
            throw new NotImplementedException();
        }

        public int FormatScript()
        {
            throw new NotImplementedException();
        }

        public IExecution CreateExecution(IScript script, string sequencename = null)
        {
            var a2bscript = script as TsA2BScript;
            var exec = TestStand.CreateExecution(a2bscript.TsScript, sequencename);

            var a2bexec = new TsA2BExecution(exec as TsEngine.Execution, a2bscript);
            _Executions.Add(a2bexec);
            return a2bexec;
        }

        public IExecution StartExecution(IScript script, string sequencename = null)
        {
            var a2bscript = script as TsA2BScript;
            var exec = TestStand.StartExecution(a2bscript.TsScript, sequencename);

            var a2bexec = new TsA2BExecution(exec as TsEngine.Execution, a2bscript);
            _Executions.Add(a2bexec);
            return a2bexec;
        }

        public IExecution StartReferenceExecution(IScript script) => TestStand.StartReferenceExecution(script);

        public IExecution StartVerificationExecution(IScript script) => TestStand.StartVerificationExecution(script);

        public int StopExecution(IExecution exec) => TestStand.StopExecution(exec);

        public int ResumeAll() => TestStand.ResumeAll();

        public int TerminateAll() => TestStand.TerminateAll();

        public int AbortAll() => TestStand.AbortAll();

        public int SetModulePath(string modulepath) => TestStand.SetModulePath(modulepath);

        public int GenerateReport(TF_Result rs, string basepath)
        {
            var fname = rs.GenerateReportName("tyml");
            var dir = rs.GenerateLocalReportDir(basepath);

            var path = System.IO.Path.Combine(dir, fname);
            try
            {
                rs.XmlSerialize().Save(path);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(dir);
                rs.XmlSerialize().Save(path);
            }
            catch (System.NotSupportedException)
            {
                if (fname.Contains(':'))
                {
                    fname = fname.Replace(':', '`');
                    path = System.IO.Path.Combine(dir, fname);
                    rs.XmlSerialize().Save(path);
                }
            }

            OnReportGenerated?.Invoke(this, new Tuple<TF_Result, string>(rs, path));

            return 1;
        }

        public int StartCalibration() => TestStand.StartCalibration();

        public int ApplyCalibration()=> TestStand.ApplyCalibration();

        public void Dispose() => TestStand.Dispose();

        public void ShowEngineSettingDialog()
        {
            throw new NotImplementedException();
        }
    }
}
