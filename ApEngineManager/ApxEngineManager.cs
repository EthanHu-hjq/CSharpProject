using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using TestCore.UI;
using ToucanCore.Abstraction.Engine;


namespace ApEngineManager
{
    public class ApxEngineManager : TF_Base, IEngine
    {
        public string Name => "ApEngine";

        public string Version => ActuallyEngine?.Version;

        public string UserName => ActuallyEngine?.UserName;

        public string StationId { get => ActuallyEngine.StationId; set => ActuallyEngine.StationId = value; }

        public string FileFilter => "APx Project|*.approjx";

        public bool IsInitialized => ActuallyEngine?.IsInitialized ?? false;

        public bool IsStarted => ActuallyEngine?.IsStarted ?? false;

        public bool IsForVerification { get => ActuallyEngine?.IsForVerification ?? false; set => ActuallyEngine.IsForVerification = value; }

        public bool BreakOnFirstStep => ActuallyEngine?.BreakOnFirstStep ?? false;

        public bool BreakOnFailure => ActuallyEngine?.BreakOnFailure ?? false;

        public bool GotoCleanupOnFailure => ActuallyEngine?.GotoCleanupOnFailure ?? false;

        public bool DisableResults => ActuallyEngine?.DisableResults ?? false;

        public int ActionOnError => ActuallyEngine.ActionOnError;

        public IReadOnlyDictionary<string, object> Variables => ActuallyEngine.Variables;

        public IModel Model => ActuallyEngine.Model;

        public bool UiVisible { get => ActuallyEngine?.UiVisible ?? false; set => ActuallyEngine.UiVisible = value; }

        public bool IsEditMode { get => ActuallyEngine?.IsEditMode ?? false; set => ActuallyEngine.IsEditMode = value; }

        IEngine ActuallyEngine { get; set; }

        public event EventHandler OnEngineInitialized;

        public event EventHandler OnEngineStarted;

        public event EventHandler OnEngineStopped
        {
            add
            {
                ActuallyEngine.OnEngineStopped += value;
            }

            remove
            {
                ActuallyEngine.OnEngineStopped -= value;
            }
        }

        public event EventHandler<IExecution> OnExecutionCreated
        {
            add
            {
                ActuallyEngine.OnExecutionCreated += value;
            }

            remove
            {
                ActuallyEngine.OnExecutionCreated -= value;
            }
        }

        public event EventHandler<IExecution> OnExecutionStarted
        {
            add
            {
                ActuallyEngine.OnExecutionStarted += value;
            }

            remove
            {
                ActuallyEngine.OnExecutionStarted -= value;
            }
        }

        public event EventHandler<IExecution> OnExecutionStopped
        {
            add
            {
                ActuallyEngine.OnExecutionStopped += value;
            }

            remove
            {
                ActuallyEngine.OnExecutionStopped -= value;
            }
        }

        public event EventHandler<Tuple<TF_Result, string>> OnReportGenerated
        {
            add
            {
                ActuallyEngine.OnReportGenerated += value;
            }

            remove
            {
                ActuallyEngine.OnReportGenerated -= value;
            }
        }

        public event EventHandler CalibrationExpired
        {
            add
            {
                ActuallyEngine.CalibrationExpired += value;
            }

            remove
            {
                ActuallyEngine.CalibrationExpired -= value;
            }
        }

        public event EventHandler CalibrationExpiring
        {
            add
            {
                ActuallyEngine.CalibrationExpiring += value;
            }

            remove
            {
                ActuallyEngine.CalibrationExpiring -= value;
            }
        }

        public event EventHandler<IScript> OnScriptOpened
        {
            add
            {
                ActuallyEngine.OnScriptOpened += value;
            }

            remove
            {
                ActuallyEngine.OnScriptOpened -= value;
            }
        }

        public ApxEngineManager() : this(null) { }

        public ApxEngineManager(string version)
        {
            SpecifiedVersion = version;
        }

        public int AbortAll()
        {
            return ActuallyEngine.AbortAll();
        }

        public int ApplyCalibration()
        {
            return ActuallyEngine.ApplyCalibration();
        }

        public void Dispose()
        {
            ActuallyEngine.Dispose();
        }

        public int FormatScript()
        {
            return ActuallyEngine.FormatScript();
        }

        public int GenerateReport(TF_Result rs, string basepath)
        {
            return ActuallyEngine.GenerateReport(rs, basepath);
        }

        public string SpecifiedVersion { get; set; }

        public static Lazy<string[]> ApVersions = new Lazy<string[]>(()=> Registry.LocalMachine.OpenSubKey("Software\\Audio Precision\\APx500", false).GetSubKeyNames());
        public static Lazy<string> SpecifyVersion = new Lazy<string>(() => ServiceStatic.RootKey.GetValue("APx Version", "") as string);

        public int Initialize()
        {
            var keypath = "Software\\Audio Precision\\APx500";
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(keypath, false);

            var name = SpecifiedVersion;

            if (name is null)
            {
                name = ApVersions.Value.Max();
                if (ApVersions.Value.Length > 1)
                {
                    Warn($"Multiple Version APx {string.Join(" ", ApVersions.Value)} Detected. the {name} is active");
                }
            }
            else
            {
                var fetchname = ApVersions.Value.FirstOrDefault(x => x.CompareTo(name) >= 0);

                if(fetchname != name)
                {
                    Warn($"Specified version {name} does not exist. use {fetchname} instead");
                }
                
                name = fetchname;
            }

            Assembly assembly;
            Type type;

            var basedir = Path.GetDirectoryName(typeof(ApxEngineManager).Assembly.Location);

            var appapipath = Path.Combine(Path.GetDirectoryName(registryKey.OpenSubKey(name).GetValue("Location") as string), "AudioPrecision.API.dll");

            switch (name)
            {
                case "5.0":
                    File.Copy(appapipath, Path.Combine(basedir, "AudioPrecision.API.dll"), true);
                    //assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "AudioPrecision.API_v5.dll"));
                    assembly = Assembly.LoadFrom(Path.Combine(basedir, "ApEngine_v5.dll"));
                    type = assembly.ExportedTypes.First(x => typeof(IEngine).IsAssignableFrom(x));

                    ActuallyEngine = Activator.CreateInstance(type) as IEngine;

                    //ActuallyEngine = new ApEngine_v5::ApEngine.ApxEngine() { Name = "APx v5" };

                    break;
                case "6.0":
                case "6.1":
                    File.Copy(appapipath, Path.Combine(basedir, "AudioPrecision.API.dll"), true);
                    //assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "AudioPrecision.API_v6.dll"));
                    assembly = Assembly.LoadFrom(Path.Combine(basedir, "ApEngine_v6.dll"));
                    type = assembly.ExportedTypes.First(x => typeof(IEngine).IsAssignableFrom(x));

                    ActuallyEngine = Activator.CreateInstance(type) as IEngine;
                    
                    //ActuallyEngine = new ApEngine_v6::ApEngine.ApxEngine() { Name = "APx v6" };
                    break;
                case "7.0":
                    File.Copy(appapipath, Path.Combine(basedir, "AudioPrecision.API.dll"), true);
                    //assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "AudioPrecision.API_v7.dll"));
                    assembly = Assembly.LoadFrom(Path.Combine(basedir, "ApEngine_v7.dll"));
                    type = assembly.ExportedTypes.First(x => typeof(IEngine).IsAssignableFrom(x));

                    ActuallyEngine = Activator.CreateInstance(type) as IEngine;
                    
                    //ActuallyEngine = new ApEngine_v7::ApEngine.ApxEngine() { Name = "APx v7" };
                    break;
                case "9.0":
                    File.Copy(appapipath, Path.Combine(basedir, "AudioPrecision.API.dll"), true);
                    //assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "AudioPrecision.API_v7.dll"));
                    assembly = Assembly.LoadFrom(Path.Combine(basedir, "ApEngine_v9.dll"));
                    type = assembly.ExportedTypes.First(x => typeof(IEngine).IsAssignableFrom(x));

                    ActuallyEngine = Activator.CreateInstance(type) as IEngine;

                    //ActuallyEngine = new ApEngine_v7::ApEngine.ApxEngine() { Name = "APx v7" };
                    break;
                default:
                    File.Copy(appapipath, Path.Combine(basedir, "AudioPrecision.API.dll"), true);
                    //assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "AudioPrecision.API_v5.dll"));
                    assembly = Assembly.LoadFrom(Path.Combine(basedir, "ApEngine_v5.dll"));
                    type = assembly.ExportedTypes.First(x => typeof(IEngine).IsAssignableFrom(x));

                    ActuallyEngine = Activator.CreateInstance(type) as IEngine;
                    
                    //ActuallyEngine = new ApEngine_v5::ApEngine.ApxEngine() { Name = "APx v5" };
                    break;
            }

            name = ActuallyEngine.Name;
            ActuallyEngine.OnEngineStarted += ActuallyEngine_OnEngineStarted;
            return ActuallyEngine.Initialize();
        }

        private void ActuallyEngine_OnEngineStarted(object sender, EventArgs e)
        {
            OnEngineStarted?.Invoke(this, e);
        }

        public IScript LoadScriptFile(string path)
        {
            return ActuallyEngine.LoadScriptFile(path);
        }

        public int Login(string username, string password)
        {
            return ActuallyEngine.Login(username, password);
        }

        public IScript NewScript(GlobalConfiguration config = null)
        {
            return ActuallyEngine.NewScript(config);
        }

        public int ResumeAll()
        {
            return ActuallyEngine.ResumeAll();
        }

        public int SetModulePath(string modulepath)
        {
            return ActuallyEngine.SetModulePath(modulepath);
        }

        public int StartCalibration()
        {
            return ActuallyEngine.StartCalibration();
        }

        public int StartEngine()
        {
            return ActuallyEngine.StartEngine();
        }

        public IExecution CreateExecution(IScript script, string sequencename = null)
        {
            return ActuallyEngine.CreateExecution(script, sequencename);
        }

        public IExecution StartExecution(IScript script, string sequencename = null)
        {
            return ActuallyEngine.StartExecution(script, sequencename);
        }

        public IExecution StartReferenceExecution(IScript script)
        {
            return ActuallyEngine.StartReferenceExecution(script);
        }

        public IExecution StartVerificationExecution(IScript script)
        {
            return ActuallyEngine.StartVerificationExecution(script);
        }

        public int StopEngine()
        {
            return ActuallyEngine.StopEngine();
        }

        public int StopExecution(IExecution exec)
        {
            return ActuallyEngine.StopExecution(exec);
        }

        public int TerminateAll()
        {
            return ActuallyEngine.TerminateAll();
        }

        public static string GetProjectFileApVersion(string filepath)
        {
            string ver = null;
            var zip = System.IO.Packaging.ZipPackage.Open(filepath);
            var prjpart = zip.GetPart(new Uri("/project.xml", UriKind.Relative));
            using (var stream = prjpart.GetStream(FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(stream))
                {
                    sr.ReadLine();
                    sr.ReadLine();
                    var str = sr.ReadLine();
                    var match = Regex.Match(str, @"version\s+(\d+\.\d+)\.\d+");
                    if (match.Success)
                    {
                        ver = match.Groups[1].Value;
                    }
                }
            }
            zip.Close();

            return ver;
        }

        public void ShowEngineSettingDialog()
        {
            ActuallyEngine?.ShowEngineSettingDialog();
        }
    }
}
