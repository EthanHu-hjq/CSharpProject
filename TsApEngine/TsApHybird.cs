using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore;
using TestCore.Data;
using TestCore.Configuration;
using TsEngine;
using ToucanCore.Abstraction.Engine;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using ApEngineManager;
using System.Threading;

namespace TsApEngine
{
    public partial class TsApHybird : TF_Base, ToucanCore.Abstraction.Engine.IEngine<TsApEngine.TsApExecution, TsApEngine.TsApScript>
    {
        public static TsEngine.TestStandEngine TestStand { get; } = new TestStandEngine();
        public static ApEngineManager.ApxEngineManager Apx { get; } = new ApxEngineManager();

        private List<TsApEngine.TsApExecution> _Executions = new List<TsApEngine.TsApExecution>();
        public IReadOnlyCollection<TsApEngine.TsApExecution> Executions => _Executions;

        public IReadOnlyCollection<TsApEngine.TsApScript> Scripts { get; private set; }

        public string Name => "TsApHybird";

        public string Version => "1.0";

        public string UserName => ((IEngine)TestStand).UserName;

        public string StationId { get => ((IEngine)TestStand).StationId; set => ((IEngine)TestStand).StationId = value; }

        public string FileFilter => "TestStand & APx Hybird Sequence|*.tsap";

        public bool IsInitialized => ((IEngine)TestStand).IsInitialized;

        public bool IsStarted => ((IEngine)TestStand).IsStarted;

        public bool IsForVerification { get => ((IEngine)TestStand).IsForVerification; set => ((IEngine)TestStand).IsForVerification = value; }

        public bool BreakOnFirstStep => ((IEngine)TestStand).BreakOnFirstStep;

        public bool BreakOnFailure => ((IEngine)TestStand).BreakOnFailure;

        public bool GotoCleanupOnFailure => ((IEngine)TestStand).GotoCleanupOnFailure;

        public bool DisableResults => ((IEngine)TestStand).DisableResults;

        public int ActionOnError => ((IEngine)TestStand).ActionOnError;

        public IReadOnlyDictionary<string, object> Variables => ((IEngine)TestStand).Variables;

        public IModel Model => ((IEngine)TestStand).Model;

        public bool UiVisible 
        { 
            get => (TestStand).UiVisible; 
            set { TestStand.UiVisible = value; Apx.UiVisible = value; } 
        }

        public bool IsEditMode
        {
            get => TestStand.IsEditMode; set => TestStand.IsEditMode = value;
        }

        public TsApHybird()
        {
            TsApScript.StaticEngine = this;

            TestStand.TestStandUserEvent += TestStandUserEvent;
            TestStand.OnExecutionCreated += TestStand_OnExecutionCreated;
            TestStand.OnExecutionStarted += TestStand_OnExecutionStarted;
            TestStand.OnExecutionStopped += TestStand_OnExecutionStopped;
        }

        private void TestStand_OnExecutionCreated(object sender, IExecution e)
        {
            if (Executions.FirstOrDefault(x => x.TsExecution == e) is TsApExecution exec)
            {
                exec.TsExecution_ExecutionCreated(e, null);
                OnExecutionCreated?.Invoke(this, exec);
            }
        }

        private void TestStand_OnExecutionStopped(object sender, IExecution e)
        {
            if(Executions.FirstOrDefault(x => x.TsExecution == e) is IExecution exec)
            {
                OnExecutionStopped?.Invoke(this, exec);
            }
        }

        private void TestStand_OnExecutionStarted(object sender, IExecution e)
        {
            if (Executions.FirstOrDefault(x => x.TsExecution == e) is TsApExecution exec)
            {
                OnExecutionStarted?.Invoke(this, exec);
            }
        }

        private void TestStandUserEvent(object sender, Tuple<int, double, string, object> e)
        {
            var exec = Executions.FirstOrDefault();
            switch(e.Item1) 
            {
                // Update Config
                case 10100:
                    
                    break;

                // Call APx
                case 10200:
                    lock (this) 
                    {
                        if(e.Item4 is NationalInstruments.TestStand.Interop.API.SequenceContext sc)
                        {
                            exec.TrigApStart(sc, (int)e.Item2);

                            

                            //Thread.Sleep(500);
                            //ApxEngine.Mre_Operation.WaitOne(); // wait for test finished

                            //Info("APx Done");

                            //for (int i = 0; i < exec.Script.DefaultVariable.Count; i++)
                            //{
                            //    var name = exec.Script.DefaultVariable.Keys.ElementAt(i);
                            //    var val = exec.ApExecution.GetVariable(name);

                            //    syncupvar.SetValString(name, 0, val) ;
                            //}
                        }
                    }
                    break;
            }
        }

        public event EventHandler OnEngineInitialized
        {
            add
            {
                ((IEngine)TestStand).OnEngineInitialized += value;
            }

            remove
            {
                ((IEngine)TestStand).OnEngineInitialized -= value;
            }
        }

        public event EventHandler OnEngineStarted;

        public event EventHandler OnEngineStopped
        {
            add
            {
                ((IEngine)TestStand).OnEngineStopped += value;
            }

            remove
            {
                ((IEngine)TestStand).OnEngineStopped -= value;
            }
        }

        public event EventHandler<IExecution> OnExecutionCreated;

        public event EventHandler<IExecution> OnExecutionStarted;

        public event EventHandler<IExecution> OnExecutionStopped;

        public event EventHandler<Tuple<TF_Result, string>> OnReportGenerated
        {
            add
            {
                ((IEngine)TestStand).OnReportGenerated += value;
                Apx.OnReportGenerated += value;
            }

            remove
            {
                ((IEngine)TestStand).OnReportGenerated -= value;
                Apx.OnReportGenerated -= value;
            }
        }

        public event EventHandler CalibrationExpired
        {
            add
            {
                ((IEngine)TestStand).CalibrationExpired += value;
            }

            remove
            {
                ((IEngine)TestStand).CalibrationExpired -= value;
            }
        }

        public event EventHandler CalibrationExpiring
        {
            add
            {
                ((IEngine)TestStand).CalibrationExpiring += value;
            }

            remove
            {
                ((IEngine)TestStand).CalibrationExpiring -= value;
            }
        }

        public event EventHandler<IScript> OnScriptOpened;

        public int AbortAll()
        {
            return ((IEngine)TestStand).AbortAll();
        }

        public int ApplyCalibration()
        {
            return ((IEngine)TestStand).ApplyCalibration();
        }

        public void Dispose()
        {
            ((IDisposable)TestStand).Dispose();
        }

        public int FormatScript()
        {
            return ((IEngine)TestStand).FormatScript();
        }

        public int GenerateReport(TF_Result rs, string basepath)
        {
            return ((IEngine)TestStand).GenerateReport(rs, basepath);
        }

        public int Initialize()
        {
            Apx.Initialize();
            return ((IEngine)TestStand).Initialize();
        }

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

            TsApScript script = new TsApScript();
            script.Open(path);
            OnScriptOpened?.Invoke(this, script);

            script.TsScript.SystemConfig = script.SystemConfig;
            script.ApScript.SystemConfig = script.SystemConfig;

            return script;
        }

        public int Login(string username, string password)
        {
            Apx.Login(username, password);
            return TestStand.Login(username, password);
        }

        public IScript NewScript(GlobalConfiguration config = null)
        {
            return ((IEngine)TestStand).NewScript(config);
        }

        public int ResumeAll()
        {
            return ((IEngine)TestStand).ResumeAll();
        }

        public int SetModulePath(string modulepath)
        {
            return ((IEngine)TestStand).SetModulePath(modulepath);
        }

        public int StartCalibration()
        {
            return Apx.StartCalibration();
        }

        public int StartEngine()
        {
            var a = Task.Run(() => { Apx.StartEngine(); });
            TestStand.StartEngine();   // for TestStand UI require STA, can not make it in async
            a.Wait();
            OnEngineStarted?.Invoke(this, null);
            return 1;
        }

        public IExecution CreateExecution(IScript script, string sequencename = null)
        {
            var tsapscript = script as TsApScript;

            var exec = new TsApExecution(tsapscript);
            _Executions.Add(exec);
            //OnExecutionCreated?.Invoke(this, exec);
            return exec;
        }

        public IExecution StartExecution(IScript script, string sequencename = null)
        {
            var tsapscript = script as TsApScript;

            var exec = new TsApExecution(tsapscript);
            _Executions.Add(exec);
            return exec;
        }

        public IExecution StartReferenceExecution(IScript script)
        {
            return Apx.StartReferenceExecution(script);
        }

        public IExecution StartVerificationExecution(IScript script)
        {
            return Apx.StartVerificationExecution(script);
        }

        public int StopEngine()
        {
            TestStand.StopEngine();
            Apx.StopEngine();
            return 1;
        }

        public int StopExecution(IExecution exec)
        {
            return ((IEngine)TestStand).StopExecution(exec);
        }

        public int TerminateAll()
        {
            return ((IEngine)TestStand).TerminateAll();
        }

        public static string GetProjectFileApVersion(string filepath)
        {
            GetTsApFilePath(filepath, out _, out string appath, out _);

            return ApxEngineManager.GetProjectFileApVersion(appath);
        }

        public static void GetTsApFilePath(string path, out string tspath, out string appath, out string dir)
        {
            tspath = null;
            appath = null;
            dir = Path.GetDirectoryName(path);
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    var p = sr.ReadLine();
                    var ext = Path.GetExtension(p);

                    if (TsApHybird.TestStand.FileFilter.Contains(ext))
                    {
                        if (Path.IsPathRooted(p))
                        {
                            tspath = p;
                        }
                        else
                        {
                            tspath = Path.Combine(dir, p);
                        }
                    }
                    else if (TsApHybird.Apx.FileFilter.Contains(ext))
                    {
                        if (Path.IsPathRooted(p))
                        {
                            appath = p;
                        }
                        else
                        {
                            appath = Path.Combine(dir, p);
                        }
                    }
                }
            }

            if (tspath is null || appath is null)
            {
                throw new InvalidDataException($"Open TsAp Sequence Failed. TS {tspath}, Ap {appath}");
            }
        }

        public void ShowEngineSettingDialog()
        {
            throw new NotImplementedException();
        }
    }
}
