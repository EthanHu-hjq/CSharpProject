using ApEngineManager;
using MahApps.Metro.Controls.Dialogs;
using Mes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Threading;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using ToucanCore.HAL;
using TestCore.MetaData;
using TestCore.Misc;
using TestCore.Services;
using TestCore.UI;
using Toucan_WPF.ViewModels;
using ToucanCore;
using ToucanCore.Driver;
using ToucanCore.Engine;
using TsEngine;
using TsEngine.UIs;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.Engine;
using ToucanCore.Abstraction.HAL;
using ToucanCore.UIs;
using Toucan_WPF.UIs;
using System.Web.UI;
using System.Runtime.Remoting.Channels;
using TsApEngine;
using TsA2BEngine;
using System.Security.Cryptography;
using ScottPlot;
using System.Windows.Interop;
using System.IO.Packaging;
using ScottPlot.Drawing.Colormaps;
using System.Windows.Controls;

namespace Toucan_WPF
{
    public partial class VM_Main : DependencyObject
    {
        public const string RK_ConstStationID = "ConstStationID";
        public const string RK_ConstLineNo = "ConstLineNo";

        internal IToolboxService ToolboxService { get; }
        // the toolbox will block the close process, need to be debug
        // For the close have been cancelled. need to be fixed
        internal IReportService ReportService { get; set; }
        internal IAuthService AuthService { get; set; }
        internal ITimeService TimeService { get; set; }

        public DelegateCommand UnlockScript { get; }
        public DelegateCommand NewScript { get; }
        public DelegateCommand OpenScript { get; }
        public DelegateCommand SaveScript { get; }
        public DelegateCommand ProjectSetting { get; }
        public DelegateCommand HardwareSetting { get; }
        public DelegateCommand GoldenSampleManager { get; }

        public DelegateCommand ShowSpec { get; }
        public DelegateCommand ShowWorkbase { get; }
        public DelegateCommand ShowReferenceBase { get; }

        public DelegateCommand ShowDriverDebugger { get; }
        public DelegateCommand FixtureManipulation { get; }
        public DelegateCommand ShowDebugger { get; }
        public DelegateCommand RunScript { get; }

        public DelegateCommand InputUserText { get; }
        public DelegateCommand ExecuteTest { get; }
        public DelegateCommand Calibration { get; }
        public DelegateCommand ScriptCalibration { get; }
        public DelegateCommand Reference { get; }
        public DelegateCommand Verification { get; }
        public DelegateCommand LoopTest { get; }

        //TODO
        public DelegateCommand ReportIssue { get; }
        public DelegateCommand CheckUpdate { get; }
        public DelegateCommand RollbackUpdate { get; }

        public DelegateCommand ShowSpecMerge { get; }

        public DelegateCommand TagResult { get; }
        public DelegateCommand ExportAttachResultAs { get; }

        public DelegateCommand Open { get; }
        public DelegateCommand Exit { get; }

        public DelegateCommand ShowVariables { get; }

        public DelegateCommand ResetTestTable { get; }
        public DelegateCommand ShowRecordList { get; }
        public DelegateCommand ShowRecordChart { get; }

        public DelegateCommand ShowWebReportViewer { get; }
        public DelegateCommand ShowNetworkHelper { get; }
        public DelegateCommand ShowEnvironmentHelper { get; }
        public DelegateCommand ShowMesHelper { get; }
        public DelegateCommand ShowSelfTest { get; }

        public DelegateCommand ShowTool { get; }

        public DelegateCommand Link { get; }

        public DelegateCommand SwitchLanguage { get; }

        public DelegateCommand SetPhysicalStationID { get; }
        public DelegateCommand SetLineNo { get; }

        public DelegateCommand RestoreLocalReport { get; }

        public DelegateCommand GoldenSamplePick { get; }

        static string Language = TestCore.Services.ServiceStatic.RootKey.GetValue("lang") as string;
        public static string ConstStationID { get; set; } = ServiceStatic.RootKey.GetValue(RK_ConstStationID, null) as string;
        public static string ConstLineNo{ get; set; } = ServiceStatic.RootKey.GetValue(RK_ConstLineNo, null) as string;

        static RegistryKey ToucanKey = ServiceStatic.RootKey.CreateSubKey("Toucan", true);

        public DelegateCommand SlotVariableTableSetting { get; }
        public DelegateCommand ShowEngineSetting { get; }
        public VM_Main()
        {
            Open = new DelegateCommand(cmd_Open);
            Exit = new DelegateCommand(cmd_Exit);
            ResetTestTable = new DelegateCommand(cmd_ResetTestTable);
            ShowVariables = new DelegateCommand(cmd_ShowVariables);
            ShowDriverDebugger = new DelegateCommand(cmd_ShowDriverDebugger);
            FixtureManipulation = new DelegateCommand(cmd_FixtureManipulation);
            ShowDebugger = new DelegateCommand(cmd_ShowDebugger);

            ShowRecordList = new DelegateCommand(cmd_ShowRecordList);
            ShowRecordChart = new DelegateCommand(cmd_ShowRecordChart);

            UnlockScript = new DelegateCommand(cmd_UnlockScript);
            NewScript = new DelegateCommand(cmd_NewScript);
            OpenScript = new DelegateCommand(cmd_OpenScript);
            SaveScript = new DelegateCommand(cmd_SaveScript);
            ProjectSetting = new DelegateCommand(cmd_ProjectSetting);
            InputUserText = new DelegateCommand(cmd_InputUserText);
            HardwareSetting = new DelegateCommand(cmd_HardwareSetting);
            SlotVariableTableSetting = new DelegateCommand(cmd_SlotVariableTableSetting);
            GoldenSampleManager = new DelegateCommand(cmd_GoldenSampleManager);
            GoldenSamplePick = new DelegateCommand(cmd_GoldenSamplePick);

            ShowWorkbase = new DelegateCommand(cmd_ShowWorkbase);
            ShowReferenceBase = new DelegateCommand(cmd_ShowReferenceBase);
            ShowSpec = new DelegateCommand(cmd_ShowSpec);

            ShowWebReportViewer = new DelegateCommand(cmd_ShowWebReportViewer);
            ShowNetworkHelper = new DelegateCommand(cmd_ShowNetworkHelper);
            ShowEnvironmentHelper = new DelegateCommand(cmd_ShowEnvironmentHelper);
            ShowMesHelper = new DelegateCommand(cmd_ShowMesHelper);
            ShowSelfTest = new DelegateCommand(cmd_ShowSelfTest);

            ShowTool = new DelegateCommand(cmd_ShowTool);

            ExecuteTest = new DelegateCommand(cmd_ExecuteTest);
            ScriptCalibration = new DelegateCommand(cmd_ScriptCalibration);
            Calibration = new DelegateCommand(cmd_Calibration);
            Reference = new DelegateCommand(cmd_Reference);
            Verification = new DelegateCommand(cmd_Verification);
            LoopTest = new DelegateCommand(cmd_LoopTest);

            ShowSpecMerge = new DelegateCommand(cmd_ShowSpecMerge);
            TagResult = new DelegateCommand(cmd_TagResult);

            Link = new DelegateCommand(cmd_Link);

            SwitchLanguage = new DelegateCommand(cmd_SwitchLanguage);

            SetPhysicalStationID = new DelegateCommand(cmd_SetPhysicalStationID);
            SetLineNo = new DelegateCommand(cmd_SetLineNo);

            RestoreLocalReport = new DelegateCommand(cmd_RestoreLocalReport);

            ShowEngineSetting = new DelegateCommand(cmd_ShowEngineSetting);

            ToolboxService = EngineUtilities.ToolboxService;

            if(ToolboxService is null)
            {
                throw new InvalidProgramException("Toucan Depends on Toolbox Service, Please run it at first");
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerAsync();

            var initlang = Language;
            if (Language is null)
            {
                initlang = System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag;
            }

            if (initlang == "zh-CN")
            {
                SwitchLanguage.Execute("中文");
            }
            else if (initlang == "th-TH")
            {
                SwitchLanguage.Execute("ภาษาไทย");
            }

            VM_Slot.ShowRecordList = TF_Base.TRUE_STRING.Contains(ToucanKey.GetValue("ShowRecordList", string.Empty));
            VM_Slot.ShowRecordChart = TF_Base.TRUE_STRING.Contains(ToucanKey.GetValue("ShowRecordChart", string.Empty));

            //DecodeArgs(args);

            HardwareConfig.LoadHardwareDrivers(System.Reflection.Assembly.GetAssembly(typeof(FixtureDemo)));

            HardwareConfig.StartTriggers.AddRange(new IStartTrigger[] { StartTrigger_None.Instance, StartTrigger_Fixture.Instance, StartTrigger_Keyboard.Instance, StartTrigger_Ap.Instance, StartTrigger_Exteranl.Instance });

            //InitTestEngine();

            //try
            //{
            //    TF_Utility.GetPlugInAndCreateInstance(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Driver"), out List<IFixture> fixtures, out List<IRelayArray> relays);
            //}
            //catch (FileNotFoundException ex)
            //{ }
        }

        private void cmd_ShowEngineSetting(object obj)
        {
            if(Engine is null)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Engine is null");
                return;
            }

            try
            {
                Engine.ShowEngineSettingDialog();
            }
            catch(Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Engine Setting Error. {ex}");
            }
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            InitService();
        }

        private void cmd_LoopTest(object obj)
        {
            if (ActiveExecution?.Execution is null)
            {
                Message = "No Available Execution";
                return;
            }

            UIs.LoopTest loopTestUI = new UIs.LoopTest(ActiveExecution);

            ActiveExecution.WaterPrint = "Loop Test";
            loopTestUI.ShowDialog();

            ActiveExecution.WaterPrint = String.Empty;
        }

        private void cmd_Link(object obj)
        {
            if (obj is string arg)
            {
                string linkpath = null;
                switch (arg)
                {
                    case "document":
                        Process.Start(new ProcessStartInfo($"http://raven{StationConfig.SiteFlag}.tymphany.com/Toucan/UserGuide/"));
                        break;
                    case "report":
                        if (ActiveExecution?.Script?.SystemConfig?.Station is StationConfig config)
                        {
                            try
                            {
                                if (config.ProductName == config.ProjectName)
                                {
                                    linkpath = $"\\\\ftp{StationConfig.SiteFlag}d\\db\\Test Data\\{config.StationType}\\{config.CustomerName}\\{config.ProjectName}\\{config.StationName}\\";
                                }
                                else
                                {
                                    linkpath = $"\\\\ftp{StationConfig.SiteFlag}d\\db\\Test Data\\{config.StationType}\\{config.CustomerName}\\{config.ProjectName}\\{config.ProductName}\\{config.StationName}\\";
                                }
                                Process.Start(new ProcessStartInfo(linkpath));
                            }
                            catch
                            {
                                Message = $"Open report path {linkpath} failed. Please config";
                            }
                        }
                        else
                        {
                            Message = "Please Start an Execution at first";
                        }
                        break;
                    case "sharepoint":
                        Process.Start(new ProcessStartInfo($"https://intranet.tymphany.com/Departments/pte/Projects/Forms/AllItems.aspx"));
                        break;
                    case "script":
                        if(Directory.Exists(ActiveExecution.Execution.Workbase))
                        {
                            Process.Start(new ProcessStartInfo(ActiveExecution.Execution.Workbase));
                        }
                        break;
                }
            }
        }

        private void cmd_ShowVariables(object obj)
        {
            if(ActiveExecution is null)
            {
                MessageBox.Show("Please Select an Execution at first", "Warning");
                return;
            }
            ActiveExecution.ShowVariables.Execute(obj);
        }

        DriverDebugger driverdebugger;
        private void cmd_ShowDriverDebugger(object obj)
        {
            //if (!VerifyAction()) return;

            driverdebugger = new DriverDebugger();
            driverdebugger.ShowDialog();
        }

        Lazy<FixtureManipulator> fixturemanipulator = new Lazy<FixtureManipulator>(() => { return new FixtureManipulator(); });
        private void cmd_FixtureManipulation(object obj)
        {
            try
            {
                if (!VerifyAction()) return;

                if (ActiveExecution?.Script?.HardwareConfig?.Fixture is null)
                {
                    Message = "No active fixture found";
                    return;
                }

                fixturemanipulator.Value.DataContext = ActiveExecution.Script.HardwareConfig.Fixture;

                fixturemanipulator.Value.Show();
            }
            catch(Exception ex)
            {
                Message = ex.Message;
            }
        }

        //Lazy<Mitsubishi_FX_Debugger> MitsubishiPlcDebugger = new Lazy<Mitsubishi_FX_Debugger>(() => { return new Mitsubishi_FX_Debugger(); });
        private void cmd_ShowDebugger(object obj)
        {
            try
            {
                if(obj is string para)
                {
                    switch(para)
                    {
                        case "Modbus":
                            var modbus = new Modbus_Debugger();
                            modbus.ShowDialog();
                            break;
                        case "MitsubishiPlc":
                            var MitsubishiPlc = new Mitsubishi_FX_Debugger();
                            MitsubishiPlc.ShowDialog();
                            break;
                    }
                }

                //MitsubishiPlcDebugger.Value.ShowDialog();
                
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }

        List<IEngine> Engines;
        VM_Execution Execution_Previous;
        ExecutionMode ExecutionMode_Previous = ExecutionMode.Normal;
        public void DecodeArgs(string[] args)
        {
            Engines = new List<IEngine>();

            this.UILog("Load Engines");

            try
            {
                Engines.Add(new TestStandEngine());
                Engines.Add(new ApxEngineManager());
                Engines.Add(new TsApHybird());
                Engines.Add(new TsA2BEngine.TsA2BEngine());
            }
            catch(Exception ex)
            {
                this.UILog($"Load Engine Failed. ex {ex}");
            }

            //bool isverb = false;
            string enginename = null;
            string specialversion = null;
            string filepath = null;
            for (int i = 1; i < args.Length; i++) // args start with exe path
            {
                switch (args[i].ToLower())
                {
                    case "-engine":
                        i++;
                        enginename = args[i];
                        break;

                    case "-version":
                        i++;
                        specialversion = args[i];
                        break;

                    default:
                        filepath = args[i];
                        break;
                }
            }

            if (enginename != null)
            {
                Engine = Engines.FirstOrDefault(x => string.Equals(enginename, x.Name, StringComparison.OrdinalIgnoreCase));
                if(specialversion != null) 
                {
                    if(Engine is ApxEngineManager aem)
                    {
                        aem.SpecifiedVersion = specialversion;
                    }
                }
            }

            if (filepath != null)
            {
                if (File.Exists(filepath)) 
                {
                    var ext = System.IO.Path.GetExtension(filepath);
                    if (Engine is null && Engines.FirstOrDefault(x => x.FileFilter.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) is IEngine engine)
                    {
                        Engine = engine;
                        if (Engine is ApxEngineManager aem)
                        {
                            if (ApxEngineManager.ApVersions.Value.Length > 1 && string.IsNullOrEmpty(aem.SpecifiedVersion))
                            {
                                if (ApxEngineManager.GetProjectFileApVersion(filepath) is string ver)
                                {
                                    aem.SpecifiedVersion = ver;
                                }
                            }
                        }
                        else if(Engine is TsApHybird tsap)
                        {
                            if (ApxEngineManager.ApVersions.Value.Length > 1 && string.IsNullOrEmpty(TsApHybird.Apx.SpecifiedVersion))
                            {
                                if (TsApHybird.GetProjectFileApVersion(filepath) is string ver)
                                {
                                    TsApHybird.Apx.SpecifiedVersion = ver;
                                }
                            }
                        }
                    }

                    Engine.OnEngineStarted += (sender, obj) =>
                    {
                        cmd_OpenScript(filepath);
                    };
                }
                else
                {
                    Message = $"File {filepath} does not exist";
                }
            }

            if(Engine != null)
            {
                InitTestEngine();
            }

            //if (Engine is null)
            //{
            //    Engine = Engines.FirstOrDefault();
            //}
            //EngineVersion = Engine?.Version;
        }

        private void cmd_UnlockScript(object obj)
        {
            if (ActiveScript is null) return;

            ActiveScript.LockStatus = !ActiveScript.LockStatus;
            Message = $"Script {ActiveScript.Name} Lock status update to {(ActiveScript.LockStatus ? "locked" : "unlocked")}";
        }

        private void cmd_NewScript(object obj)
        {
            try
            {
                if (Engine is TestStandEngine)
                {
                    DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"{Engine.Name} does NOT support create script yet");
                    return;
                }
                else if (Engine is null)
                {
                    Engine = Engines.FirstOrDefault(x => x is ApxEngineManager);
                    Engine.Initialize();
                }

                Setting setting = null;
                if (ToolboxService != null)
                {
                    do
                    {
                        setting = new Setting();
                        var system = GlobalConfiguration.Load(null);
                        system.Station = new StationConfig(ToolboxService.Root?.Name, ToolboxService.Customer?.Name, ToolboxService.Project?.Name, ToolboxService.Product?.Name, ToolboxService.Station?.Name, ToolboxService.StationInstance?.StationId);
                        setting.SystemSetting = system;

                        setting.Title = "Please Config your setting for New Script";

                        if (setting.ShowDialog() == true)
                        {
                            var cu = ToolboxService.Customers?.FirstOrDefault(x => x.Name == setting.SystemSetting.Station.CustomerName);
                            var prj = cu?.FirstOrDefault(x => x.Name == setting.SystemSetting.Station.ProjectName);
                            var prd = prj?.FirstOrDefault(x => x.Name == setting.SystemSetting.Station.ProductName);
                            var sts = prd?.FirstOrDefault(x => x.Name == setting.SystemSetting.Station.StationName);

                            if (sts is null)
                            {
                                if (MessageBox.Show($"Your station {setting.SystemSetting.Station.CustomerName}_{setting.SystemSetting.Station.ProjectName}_{setting.SystemSetting.Station.ProductName}_{setting.SystemSetting.Station.StationName} is not exists. Click OK to redo config, or cancel for use it anyway", "Warning", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                {
                                    continue;
                                }
                            }
                            break;
                        }
                        else
                        {
                            return;
                        }
                    }
                    while (true);
                }
                else
                {
                    setting = new Setting();
                    setting.Title = "Please Config your setting for New Script in Offline Mode.";

                    if (setting.ShowDialog() != true)
                    {
                        return;
                    }
                }

                var config = setting.SystemSetting;

                var task_script = Task.Run(() =>
                {
                    var script = Engine.NewScript(config);
                    script.Author = AuthService?.UserName;

                    return script;
                });

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = Engine.FileFilter;
                sfd.Title = "Save New Script As";

                if (sfd.ShowDialog() == true)
                {
                    task_script.Wait();
                    var script = task_script.Result;
                    script.SystemConfig = setting.SystemSetting;
                    script.Save(sfd.FileName);
                    Message = "Create Script OK";

                    //Scripts.Add(script);
                    Scripts.Clear();
                    Scripts.Add(script);   // TODO, Not Support Multiple Script Yet

                    ActiveScript = new ToucanCore.Engine.Script(script);
                    UnlockScript.Execute(null);
                    EngineUiVisible = true;

                    ExecuteTest.Execute(null);
                }
                else
                {
                    task_script.Wait();
                    Message = "Script Created, Not Saved";
                }
            }
            catch(Exception ex)
            {
                Message = $"Create New {Engine.Name} Script Failed. Err: {ex.Message}";
            }
        }

        private void cmd_ExecuteTest(object obj)
        {
            if (ActiveScript is null) return;

            try
            {
                if (Executions.FirstOrDefault(x => x.Script == ActiveScript) is VM_Execution vm)
                {
                    if(DialogCoordinator.Instance.ShowModalMessageExternal(this, "Execution Exists", "Do you want to stop current execution and start a new one?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                    {
                        vm.Stop.Execute(null);
                    }
                    else
                    {
                        return;
                    }
                }

                try
                {
                    if (ToolboxService?.Station != null && ActiveScript.SystemConfig != null) // ActionScript might be in orignal mode
                    {
                        //// the Station Setting might impact the execution variable, need update the orignal script config before executed
                        //if (ActiveScript.SystemConfig.Station is null
                        //    || string.IsNullOrEmpty(ActiveScript.SystemConfig.Station.CustomerName)
                        //    || string.IsNullOrEmpty(ActiveScript.SystemConfig.Station.ProjectName)
                        //    || string.IsNullOrEmpty(ActiveScript.SystemConfig.Station.ProductName)
                        //    || string.IsNullOrEmpty(ActiveScript.SystemConfig.Station.StationName))
                        //{
                        //    ActiveScript.SystemConfig.Station = new StationConfig("", ToolboxService.Station.Product.Project.Customer.Name,
                        //        ToolboxService.Station.Product.Project.Name,
                        //        ToolboxService.Station.Product.Name,
                        //        ToolboxService.Station.Name,
                        //        ToolboxService.StationInstance.StationId
                        //        );

                        //    ActiveScript.SystemConfig.Station.ReadLocationFromRegistry();

                        //    //ActiveScript.SystemConfig.Station.StationType; // Toolbox does not claim what the type it is
                        //}

                        if (ToolboxService.Customer.Name != ActiveScript.StationConfig.CustomerName
                            || ToolboxService.Project.Name != ActiveScript.StationConfig.ProjectName
                            || ToolboxService.Product.Name != ActiveScript.StationConfig.ProductName
                            || ToolboxService.Station.Name != ActiveScript.StationConfig.StationName)
                        {
                            // If not the same, check Current if it has been set in Toolbox, and then Notifying

                            var cu = ToolboxService.Customers?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.CustomerName);
                            var prj = cu?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.ProjectName);
                            var prd = prj?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.ProductName);
                            var sts = prd?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.StationName);

                            if (sts is null)
                            {
                                var msg = $"Start Execution Denied! Current Station Setting {ActiveScript.StationConfig.CustomerName}->{ActiveScript.StationConfig.ProjectName}->{ActiveScript.StationConfig.ProductName}->{ActiveScript.StationConfig.StationName} is not effective in Toolbox";

                                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", msg);

                                return;
                            }
                            else
                            {
                                var msg = $"CARE! Current Station Setting {ActiveScript.StationConfig.CustomerName}->{ActiveScript.StationConfig.ProjectName}->{ActiveScript.StationConfig.ProductName}->{ActiveScript.StationConfig.StationName} is not same as Toolbox setting, Are you sure to continue testing with your LOCAL setting?";

                                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                                {
                                    return;
                                }
                            }
                        }
                    }

                    var ie = Engine.CreateExecution(ActiveScript.OriginalScript);
                    ie.OnPreUUTing += (sender, e) => { Dispatcher.Invoke(() => { InputText = string.Empty; }); };
                    ie.OnTestCompleted += Execution_OnTestCompleted;
                    //// For Execution might not started. Create VM on event when Started
                    //ActiveExecution = new VM_Execution(ie);

                    // Only Watch Directory, need to be figure out
                    //FileSystemWatcher fw = new FileSystemWatcher(path, );
                    //fw.Created += ScriptFileChanged;
                    //ie.Start();
                }
                catch(SpecFileNotFoundException)
                {
                    var errmsg = $"Spec file not found. Please contact with Engineer";
                    if (AuthService?.CurrentAuthType >= AuthType.Engineer)
                    {
                        var msg = "No Spec File Detect for RestrictLimit is True. Are you want to Generate One. All history data will be missed.\r\n未检测到Spec文件，是否需要重新生成. 历史数据将会丢失";
                        if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                        {
                            var specpath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ActiveScript.FilePath), TF_Spec.DefaultFileName);
                            var spec = ActiveScript.AnalyzeSpec();
                            spec.Author = AuthService.UserName;
                            spec.Time = TimeService?.CurrentTime ?? DateTime.Now;
                            spec.Note = "Auto Generate By Toucan";
                            spec.XmlSerialize().Save(specpath);

                            spec.UpdateDefectCode(ActiveScript.SystemConfig.General.Prefix_DefectCode ?? "D-");

                            FileInfo fi = new FileInfo(specpath);
                            fi.IsReadOnly = true;

                            var ie = Engine.CreateExecution(ActiveScript.OriginalScript);
                            ie.OnPreUUTing += (sender, e) => { Dispatcher.Invoke(() => { InputText = string.Empty; }); };
                            ie.OnTestCompleted += Execution_OnTestCompleted;
                            //ie.Start();
                        }
                        else
                        {
                            Message = errmsg;
                        }
                    }
                    else
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", errmsg);

                        return;
                    }
                }

                Message = $"Start test in {ActiveScript.FilePath} ok";
            }
            catch (CalibrationDataExpiredException)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"run {ActiveScript.FilePath} failed. Calibration Data had been expired. Click Button to goto Calibration");
                Calibration.Execute(null);
            }
            catch (ReferenceDataExpiredException)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"run {ActiveScript.FilePath} failed. Reference Data had been expired. Click Button to goto Reference");
            }
            catch(VerificationDataExpiredException)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"run {ActiveScript.FilePath} failed. Verification Data had been expired. Click Button to goto Verification");
            }
            catch(Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"run {ActiveScript.FilePath} failed. Err: {ex}");
            }
        }

        /// <summary>
        /// Check the Preconditon Data if it is OK, CAUTION, This will force to specified Execution Mode
        /// </summary>
        /// <returns></returns>
        private int CheckPreconditionData()
        {
            try
            {
                Engine.ApplyCalibration(); // Check if can start test;
            }
            catch (CalibrationDataExpiringWarning cdew)
            {
                var msg = $"Hardware Calibration is going to out of date after {cdew.RemainedHours} hours. Please DO Hardware Calibrartion asap.\r\nClick OK to to goto Calibration, otherwise keep Testing";
                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    Engine.StartCalibration();
                    Engine.ApplyCalibration();
                }
            }
            catch (CalibrationDataExpiredException)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Current Hardware Calibration data expired. Need DO Calibration at first");
                Engine.StartCalibration();
                Engine.ApplyCalibration();
            }
            catch
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Current Calibration does not match script. Need DO Calibration at first");
                Engine.StartCalibration();
                Engine.ApplyCalibration();
            }

            try
            {
                ActiveScript.ApplyCalibration();
            }
            catch (CalibrationDataExpiringWarning cdew)
            {
                var msg = $"Script Calibration is going to out of date after {cdew.RemainedHours} hours. Please DO Script Calibrartion asap.\r\nClick OK to to goto Calibration, otherwise keep Testing";
                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    ActiveScript.StartCalibration();
                    ActiveScript.ApplyCalibration();
                }
            }
            catch (CalibrationDataExpiredException)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Current Calibration data expired. Need DO Calibration at first");
                ActiveScript.StartCalibration();
                ActiveScript.ApplyCalibration();
            }
            catch
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Current Calibration does not match script. Need DO Calibration at first");
                ActiveScript.StartCalibration();
                ActiveScript.ApplyCalibration();
            }

            try
            {
                ActiveScript.ApplyReference();
            }
            catch (ReferenceDataExpiringWarning rdew)
            {
                var msg = $"Reference data is going to out of date after {rdew.RemainedHours} hours. Please DO Reference asap.\r\nClick OK to to goto Reference, otherwise keep Testing";
                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    if (ActiveExecution.ExecutionMode != ExecutionMode.Reference) Reference.Execute(null);
                    return 1;
                }
            }
            catch (ReferenceDataExpiredException rdee)
            {
                string msg = null;
                if (ActiveExecution.ExecutionMode == ExecutionMode.Reference)
                {
                    msg = "No Referenece Data Generated. Please check you script at first";
                }
                else
                {
                    msg = $"{rdee.Message}.\r\nClick OK to to goto Reference, otherwise stop testing";
                }
                
                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    if (ActiveExecution.ExecutionMode == ExecutionMode.Reference)
                    { 
                        return 0;  // If current is reference, return 0, to make switching been denied
                    }
                    else
                    {
                        Reference.Execute(null);
                        return 1;
                    }
                }
                else
                {
                    return 0;
                }
            }

            try
            {
                ActiveScript.ApplyVerification();
            }
            catch (VerificationDataExpiringWarning vdew)
            {
                var msg = $"Verification data is going to out of date after {vdew.RemainedHours} hours. Please DO Verification asap.\r\nClick OK to to goto Verification, otherwise keep Testing";
                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    if (ActiveExecution.ExecutionMode != ExecutionMode.Reference) Verification.Execute(null);
                    return 1;
                }
            }
            catch (VerificationDataExpiredException vdee)
            {
                string msg = null;
                if (ActiveExecution.ExecutionMode == ExecutionMode.Verification)
                {
                    msg = "No Verification Data Generated. Please check you script at first";
                }
                else
                {
                    msg = $"{vdee.Message}.\r\nClick OK to to goto Verification, otherwise stop testing";
                }

                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    if (ActiveExecution.ExecutionMode == ExecutionMode.Verification)
                    {
                        return 0;  // If current is verification, return 0, to make switching been denied
                    }
                    else
                    {
                        Verification.Execute(null);
                        return 1;
                    }
                }
                else
                {
                    return 0;
                }
            }

            return 1;
        }

        VM_Execution RefExec;
        VM_Execution VerExec;

        /// <summary>
        /// After Reference, the Engine should goback to Normal Mode
        /// </summary>
        /// <param name="obj"></param>
        private void cmd_Reference(object obj)
        {
            if (ActiveScript != null)
            {
                if (ActiveExecution?.Execution?.Results?.FirstOrDefault(x=> x.Status == TF_TestStatus.TESTING) != null)
                {
                    Message = "Action Denied. Execution is Testing, Please Wait Test Finished";
                    DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", Message);

                    return;
                }

                try
                {
                    if (ActiveExecution.ExecutionMode == ExecutionMode.Reference)
                    {
                        if (CheckPreconditionData() > 0)
                        {
                            if (ActiveExecution.ExecutionMode == ExecutionMode.Reference) ActiveExecution.SwitchNormalMode.Execute(null);
                        }
                    }
                    else
                    {
                        ActiveExecution.SwitchReferenceMode.Execute(null);
                    }
                }
                catch (NotImplementedException)
                {
                    DialogCoordinator.Instance.ShowModalMessageExternal(this, "Action Denied!", "The Engine does not support Reference yet.");
                }

                return;  // REF VER integrated into normal executon

                //try
                //{
                //    if (IsReference)
                //    {
                //        ActiveExecution?.Execution?.Stop();   // Resource might be conflict
                //        ActiveExecution = Execution_Previous;
                //        ActiveExecution.Execution.Start();
                //        IsReference = false;
                //        IsVerification = false;
                //    }
                //    else
                //    {
                //        Execution_Previous = ActiveExecution;

                //        Execution_Previous?.Execution?.Stop();
                //        if (Engine.StartReferenceExecution(ActiveScript) is IExecution refexec)
                //        {
                //            if(Executions.FirstOrDefault(x=>x.Execution?.Exec == refexec) is VM_Execution vmexec)
                //            {
                //                RefExec = vmexec;
                //                vmexec.WaterPrint = "Reference";

                //                var gss = refexec.GetScript().GoldenSamples;
                //                if (gss.Count > 0)
                //                {
                //                    refexec.OnUutIdentified += (sender, e) =>
                //                    {
                //                        if(!gss.Contains(e.SerialNumber))
                //                        {
                //                            var msg = $"{e.SerialNumber} is not in Golden Sample List {string.Join(",", gss)}";
                //                            e.ErrorMessage = new ErrorMsg((int)ErrorCode.InvalidOperation, msg);
                //                            e.Status = TF_TestStatus.ERROR;
                //                            //throw new InvalidOperationException(msg);
                //                        }
                //                    };
                //                }

                //                //RefExec.Execution.ApplyHardwareSetting(); // Move to Exec Start

                //                refexec.ExecutionStopped += (sender, e) =>
                //                {
                                    
                //                    Dispatcher.Invoke(() =>
                //                    {
                //                        Execution_Previous?.Execution?.Start();
                //                        //ActiveExecution?.Execution?.Stop();
                //                        ActiveExecution = Execution_Previous;
                //                        IsReference = false;
                //                        Verification.Execute(null);
                //                    });
                //                };
                //                //RefExec.Execution.Start();
                //            }
                //        }
                //        else
                //        {
                //            Execution_Previous?.Execution?.Start();
                //            DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Current script contains no Reference");
                //            return;
                //        }

                //        ActiveExecution = RefExec;
                //        IsReference = true;
                //        IsVerification = false;
                //        Message = "Start Refernece";
                //        ActiveExecution.Execution.Start(); 
                //    }
                //}
                //catch(NotImplementedException)
                //{
                //    IsReference = false;
                //    MessageBox.Show($"Engine {Engine.Name} does not support Reference Yet");
                //}
                //catch(Exception ex)
                //{
                //    IsReference = false;
                //    Message = $"Run Reference Error. {ex.Message}";
                //}
            }
            else
            {
                Message = "Can not Start Reference, please open a script at first";
            }
        }

        private void cmd_Verification(object obj)
        {
            if (ActiveScript != null)
            {
                try
                {
                    try
                    {
                        if (ActiveExecution.ExecutionMode == ExecutionMode.Verification)
                        {
                            if (CheckPreconditionData() > 0)
                            {
                                if (ActiveExecution.ExecutionMode == ExecutionMode.Verification)  ActiveExecution.SwitchNormalMode.Execute(null);
                            }
                        }
                        else
                        {
                            ActiveExecution.SwitchVerificationMode.Execute(null);
                        }
                    }
                    catch (NotImplementedException)
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Action Denied!", "The Engine does not support Verification yet.");
                    }

                    return;  // REF VER integrated into normal executon

                    //if (IsVerification)
                    //{
                    //    if (ActiveExecution.Execution.Results.All(x => x.Result == TF_ItemStatus.Passed))
                    //    {
                    //        //ActiveExecution?.Execution?.Stop();
                    //        ActiveExecution.Execution.Stop();
                    //        ActiveExecution = Execution_Previous;
                    //        IsVerification = false;
                    //        ActiveExecution.Execution.Start();
                    //    }
                    //}
                    //else
                    //{
                    //    Execution_Previous = ActiveExecution;

                    //    Execution_Previous?.Execution?.Stop();
                    //    if (Engine.StartVerificationExecution(ActiveScript) is IExecution verexec)
                    //    {
                    //        if (Executions.FirstOrDefault(x => x.Execution?.Exec == verexec) is VM_Execution vmexec)
                    //        {
                    //            VerExec = vmexec;
                    //            vmexec.WaterPrint = "Verification";

                    //            var gss = verexec.GetScript().GoldenSamples;
                    //            if (gss.Count > 0)
                    //            {
                    //                verexec.OnUutIdentified += (sender, e) =>
                    //                {
                    //                    if (!gss.Contains(e.SerialNumber))
                    //                    {
                    //                        var msg = $"{e.SerialNumber} is not in Golden Sample List {string.Join(",", gss)}";
                    //                        e.ErrorMessage = new ErrorMsg((int)ErrorCode.InvalidOperation, msg);
                    //                        e.Status = TF_TestStatus.ERROR;
                    //                        throw new InvalidOperationException(msg);
                    //                    }
                    //                };
                    //            }

                    //            verexec.ExecutionStopped += (sender, e) =>
                    //            {
                    //                Dispatcher.Invoke(() =>
                    //                {
                    //                    if (verexec.Results.All(x => x.Result == TF_ItemStatus.Passed))
                    //                    {
                    //                        //ActiveExecution?.Execution?.Stop();
                    //                        ActiveExecution = Execution_Previous;
                    //                        IsVerification = false;
                    //                        ActiveExecution.Execution.Start();
                    //                    }
                    //                    else
                    //                    {
                    //                        Message = "Verification Failed. Please Redo Reference";
                    //                    }
                    //                });
                    //            };

                    //            //VerExec.Execution.ApplyHardwareSetting();
                    //            //VerExec.Execution.Start();
                    //        }
                    //    }
                    //    else
                    //    {
                    //        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Current script contains no Verification");
                    //        ActiveExecution.Execution.Start(); // TODO: prevent call by reference, which will make the execute not start again
                    //        return;
                    //    }

                    //    ActiveExecution = VerExec;
                    //    IsVerification = true;
                    //    IsReference = false;
                    //    Message = "Start Verification";
                    //    ActiveExecution.Execution.Start();
                    //}
                }
                catch (NotImplementedException)
                {
                    IsVerification = false;
                    MessageBox.Show($"Engine {Engine.Name} does not support Verification Yet");
                }
                catch (Exception ex)
                {
                    IsVerification = false;
                    Message = $"Run Verification Error. {ex.Message}";
                }
            }
            else
            {
                Message = "Can not Start Verification, please open a script at first";
            }
        }

        private void cmd_ScriptCalibration(object obj)
        {
            try
            {
                if(ActiveScript is null)
                {
                    Message = "No Script, please open one at first";
                    return;
                }

                if (ActiveScript.SystemConfig?.General.CalibrationPeriod <= 0)
                {
                    DialogCoordinator.Instance.ShowModalMessageExternal(this, "Action Denied!", "The setting is disable calibration on scrip.");
                    return;
                }

                if(ActiveScript.StartCalibration() > 0)
                {
                    ActiveScript.ApplyCalibration();
                    Message = $"Script Calibration Updated";
                }
            }
            catch (Exception ex)
            {
                this.UILog(ex);
                Message = $"Start Calibration Failed. Err: {ex.Message}";
            }
        }

        private void cmd_Calibration(object obj)
        {
            try
            {
                Engine?.StartCalibration();
            }
            catch(Exception ex)
            {
                this.UILog(ex);
                Message = $"Start Calibration Failed. Err: {ex.Message}";
            }
        }

        private void cmd_InputUserText(object obj)
        {
            try
            {
                if (obj is string str)
                {
                    InputText = str;
                }

                if (string.IsNullOrEmpty(InputText)) return;

                var inputtext = InputText.Trim();

                if (inputtext.StartsWith("#") && ApplyQuickFunction(inputtext))
                {
                    InputText = string.Empty;
                    return;
                }

                if (ActiveExecution is null) return;

                if (ActiveExecution.Slots.Length > 1)
                {
                    if (ActiveExecution.Script.HardwareConfig?.Fixture is IAutoActiveSlotFixture aasf)
                    {
                        //aasf.GetSlotActiveState(out bool state, activeslot.SlotIndex);

                        var asi = aasf.GetActiveSocketIndex();

                        if (asi < 0)
                        {
                            Message = $"Fixture {aasf.Resource} Slot is not ready, please wait";
                            return;
                        }

                        ActiveExecution.ActivateSlot.Execute(asi);
                        UpdateDutSn(inputtext);
                    }
                    else
                    {
                        var slotmatch = ActiveExecution.Script.SystemConfig.General.RE_SocketNumber.Match(inputtext);

                        if (slotmatch.Success)
                        {
                            var slot = int.Parse(slotmatch.Groups[1].Value);

                            if (slot >= ActiveExecution.Slots.Length)
                            {
                                Message = $"Slot {slot} should be less than {ActiveExecution.Slots.Length}";
                            }
                            else
                            {
                                ActiveExecution.ActivateSlot.Execute(slot);
                                Message = $"Activate Slot {slot}";
                            }

                            InputText = string.Empty;
                            return;
                        }

                        switch (ActiveExecution.Execution.GetScript().SystemConfig.General.InputSnMode)
                        {
                            case InputSnMode.Inherit:
                                //InputSnInInherit(inputtext);
                                Message = $"Not support {InputSnMode.Inherit} yet";
                                break;

                            case InputSnMode.Normal:
                                //InputSnInNormal(inputtext);
                                if (ActiveExecution.ActiveSlotIndex < 0)
                                {
                                    Message = $"Activate Slot Failed. Input Text {InputText}, should be {ActiveExecution.Script.SystemConfig.General.RE_SocketNumber}";
                                    return;
                                }
                                else
                                {
                                    UpdateDutSn(inputtext);
                                    InputText = string.Empty;
                                }
                                break;

                            case InputSnMode.PostfixByMainSn:
                                //InputSnInPostfixByMainSn(inputtext);
                                Message = $"Not support {InputSnMode.PostfixByMainSn} yet";
                                break;

                            case InputSnMode.Simple:
                                if (ActiveExecution.ActiveSlotIndex < 0)
                                {
                                    ActiveExecution.ActivateSlot.Execute(0);
                                }

                                UpdateDutSn(inputtext);
                                InputText = string.Empty;

                                var next = ActiveExecution.ActiveSlotIndex + 1;

                                if (next >= ActiveExecution.Slots.Length)
                                {
                                    ActiveExecution.ActivateSlot.Execute(-1);
                                }
                                else
                                {
                                    while (next < ActiveExecution.Slots.Length)
                                    {
                                        if (ActiveExecution.Slots[next]?.IsEnable == true)
                                        {
                                            ActiveExecution.ActivateSlot.Execute(next);
                                            break;
                                        }
                                        else
                                        {
                                            next++;
                                        }
                                    }

                                    if (next >= ActiveExecution.Slots.Length)
                                    {
                                        ActiveExecution.ActivateSlot.Execute(-1);
                                    }
                                }

                                break;
                        }
                        return;
                    }
                }
                else
                {
                    UpdateDutSn(inputtext);
                }
            }
            catch(Exception ex)  // there might be Driver Crash, add try catch to prevent it
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", ex.ToString());
            }
        }

        const string HELP_QUICK_FUNCTION = "Note: Function need to be start and end with char # and it is captical sensitive\r\n"
            + "\t1. #AUTOSN#. Fill each slot with slot number as sn automatically\r\n"
            + "\t2. #CLEARSN#. Clear SN Pool by set current active slot sn to be null\r\n"
            + "\t3. #CLEARALLSN#. Clear SN Pool by set all slot sn to be null\r\n"
            + "\t4. #ALLMESON#. Switch all slot into MES ON\r\n"
            + "\t5. #ALLMESOFF#. Switch all slot into MES Off\r\n"
            + "\t6. #MESVER#. For MES Verification, it will NOT commit data into MES\r\n";


        public enum QuickFunction
        {
            HELP,
            AUTOSN,
            CLEARSN,
            CLEARALLSN,
            ALLMESOFF,
            ALLMESON,
            MESVER,
        }

        private bool ApplyQuickFunction(string str)
        {
            switch(str)
            {
                case "#HELP#":
                    DialogCoordinator.Instance.ShowModalMessageExternal(this, "Quick Function Help", HELP_QUICK_FUNCTION);

                    return true;

                case "#AUTOSN#":
                    if (ActiveExecution != null)
                    {
                        if(!ActiveExecution.Execution.Results.Any(x=> x.Status == TF_TestStatus.TESTING))
                        {
                            for (int i = 0; i < ActiveExecution.Execution.SocketCount; i++)
                            {
                                ActiveExecution.UpdateDutSn.Execute(new Tuple<int, string>(i, $"{i:d2}"));
                            }
                        }
                    }

                    return true;
                case "#CLEARSN#":
                    if (ActiveExecution != null)
                    {
                        if (ActiveExecution.Execution.Results[ActiveExecution.ActiveSlotIndex].Status != TF_TestStatus.TESTING)
                        {
                            ActiveExecution.UpdateDutSn.Execute(new Tuple<int, string>(ActiveExecution.ActiveSlotIndex, null));
                        }
                    }

                    return true;

                case "#CLEARALLSN#":
                    if (ActiveExecution != null)
                    {
                        if (!ActiveExecution.Execution.Results.Any(x => x.Status == TF_TestStatus.TESTING))
                        {
                            for (int i = 0; i < ActiveExecution.Execution.SocketCount; i++)
                            {
                                ActiveExecution.UpdateDutSn.Execute(new Tuple<int, string>(i, null));
                            }
                        }
                    }

                    return true;


                case "#ALLMESOFF#":
                    if (ActiveExecution?.Script?.SystemConfig?.SFCs.Lock == false)
                    {
                        foreach (var slot in ActiveExecution.Slots)
                        {
                            slot.Result.IsSFC = false;
                            slot.MesStatus = MesStatus.SFCsOff;
                        }
                    }
                    else
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "ALLMESOFF Denied", "SFCs Status is Locked, Could not change");
                    }
                    return true;

                case "#ALLMESON#":
                    if (ActiveExecution?.Script?.SystemConfig?.SFCs.Lock == false)
                    {
                        foreach(var slot in ActiveExecution.Slots)
                        {
                            slot.Result.IsSFC = true;
                            slot.MesStatus = MesStatus.SFCsOn;
                        }
                    }
                    return true;

                case "#MESVER#":
                    if (ActiveExecution?.Mes != null)
                    {
                        if(!ActiveExecution.Mes.IsForValidation)
                        {
                            ActiveExecution.Mes.IsForValidation = true;
                            Message = "MES is in verification mode";
                        }
                        else
                        {
                            ActiveExecution.Mes.IsForValidation = false;
                            Message = "MES quit verification Mode";
                        }
                    }

                    return true;

                default:
                    string cu = null;
                    string prj = null;
                    string prd = null;
                    string sts = null;
                    string stsid = null;
                    if (Info_StationInstance.ReadQcStationInstance(str.Substring(1), ref cu, ref prj, ref prd, ref sts, ref stsid) > 0)
                    {
                        var mcu = ToolboxService.Customers.FirstOrDefault(x => x.Name == cu);
                        var mprj = mcu?.FirstOrDefault(x => x.Name == prj);
                        var mprd = mprj?.FirstOrDefault(x => x.Name == prd);
                        var msts = mprd?.FirstOrDefault(x => x.Name == sts);
                        
                        if(msts.LastOrDefault() is Info_Software sw)
                        {
                            cmd_OpenScript(sw);
                        }
                    }
                    else
                    {
                        Message = $"Unsupport Macro {str} Detected";
                    }

                    return false;
            }
            
            return true;
        }

        private void UpdateDutSn(string content) 
        {
            VM_Slot activeslot = ActiveExecution.Slots[ActiveExecution.ActiveSlotIndex];
            if (!activeslot.IsEnable)
            {
                Message = $"Current Active Socket {activeslot.SlotIndex} is disabled";
                return;
            }

            var match = ActiveExecution.Script.SystemConfig?.General.RE_SerialNumber?.Match(content);
            if (match is null || match.Success || content.Length == 2)
            {
                var sn = content;
                if (ActiveExecution.Script.SystemConfig?.General.AutoCapitalSn == true)
                {
                    sn = sn.ToUpper();
                }

                PreviousSn4 = PreviousSn3;
                PreviousSn3 = PreviousSn2;
                PreviousSn2 = PreviousSn1;
                PreviousSn1 = sn;
                InputText = null;

                try
                {
                    ActiveExecution.UpdateDutSn.Execute(sn);
                    Message = $"Serial Number Accepted: {sn}";
                }
                catch(InvalidOperationException ex)
                {
                    Message = ex.Message;
                }
            }
            else
            {
                Message = $"Input Text {content} can not be identified. Expect {ActiveExecution.Script.SystemConfig.General.RE_SerialNumber}";
            }
        }

        private void cmd_ResetTestTable(object obj)
        {
            if (ActiveExecution is null) return;

            if(obj is string str)
            {
                var match = Regex.Match(str, @"^(\d+),(\d+)$");

                int col = 1;
                int row = 1;
                if (match.Success)
                {
                    row = int.Parse(match.Groups[1].Value);
                    col = int.Parse(match.Groups[2].Value);
                }
                else
                { }

                if(row > 0)
                {
                    ActiveExecution.SlotRows = row;

                    if(col > 0)
                    {
                        ActiveExecution.SlotColumns = col;
                    }
                    else
                    {
                        col = ActiveExecution.Execution.SocketCount / row;
                        if (ActiveExecution.Execution.SocketCount % row != 0)
                        {
                            col++;
                        }
                        ActiveExecution.SlotColumns = col;
                    }
                }
                else if (col > 0)
                {
                    ActiveExecution.SlotColumns = col;
                    row = ActiveExecution.Execution.SocketCount / col;
                    if (ActiveExecution.Execution.SocketCount % col != 0)
                    {
                        row++;
                    }
                    ActiveExecution.SlotRows = row;
                }
            }
            else if(obj is int row)
            {
                ActiveExecution.SlotRows = row;

                var col = ActiveExecution.Execution.SocketCount / row;
                if(ActiveExecution.Execution.SocketCount % row != 0)
                {
                    col++;
                }
                ActiveExecution.SlotColumns = col;
            }
        }

        private void cmd_OpenScript(object obj)
        {
            //if (Engine?.IsInitialized != true)
            //{
            //    Message = $"Engine {Engine?.Name} not Initialized, please hold on";
            //    return;
            //}

            if (obj is string path) { }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                if (Engines is null)
                {
                    ofd.Filter = $"PTE Software Package|*{PspFile.SEQ_EXT}|Any File|*.*";
                }
                else
                {
                    ofd.Filter = $"Toucan File|{string.Join(";", Engines.Select(x => x.FileFilter.Split('|')[1]))}|PTE Software Package|*{PspFile.SEQ_EXT}|Any File|*.*";
                    //ofd.Filter = $"PTE Software Package|*{PspFile.SEQ_EXT}|{string.Join("|", Engines.Select(x=>x.FileFilter))}|Any File|*.*";
                }

            if (ofd.ShowDialog() != true)
                {
                    return;
                }

                path = ofd.FileName;
            }

            if (path.EndsWith(PspFile.SEQ_EXT, StringComparison.OrdinalIgnoreCase))
            {
                string pspdest = null;
                PspFile psp = new PspFile(path);
                if (PspFile.Unpack(path, ref pspdest) > 0)
                {
                    path = System.IO.Path.Combine(pspdest, psp.SetupConfig.EntryPoint);

                }
                else
                {
                    Message = $"Unpack {path} failed";
                    return;
                }
            }

            var ext = System.IO.Path.GetExtension(path);

            if (Engines.FirstOrDefault(x => x.FileFilter.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) is IEngine engine)
            {
                Engine = engine;
                EngineVersion = Engine.Version;
                if (!Engine.IsInitialized)
                {
                    if(engine is ApxEngineManager apx)
                    {
                        if (ApxEngineManager.ApVersions.Value.Length > 1 && string.IsNullOrEmpty(apx.SpecifiedVersion))
                        {
                            if(ApxEngineManager.GetProjectFileApVersion(path) is string ver)
                            {
                                apx.SpecifiedVersion = ver;
                            }
                        }
                    }
                    else if (Engine is TsApHybird tsap)
                    {
                        if (ApxEngineManager.ApVersions.Value.Length > 1 && string.IsNullOrEmpty(TsApHybird.Apx.SpecifiedVersion))
                        {
                            if (TsApHybird.GetProjectFileApVersion(path) is string ver)
                            {
                                TsApHybird.Apx.SpecifiedVersion = ver;
                            }
                        }
                    }

                    InitTestEngine();
                }
            }

           if (Executions.FirstOrDefault(x => x.Execution.GetScript()?.FilePath == path) is VM_Execution vm_exec)
            {
                if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"{path} has Already Opened. switch the execution to be active",
                    MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    Executions.Clear();
                    try
                    {
                        if (Executions.Count > 0)
                        {
                            foreach (var exec in Executions)
                            {
                                exec.Stop.Execute(null);
                            }

                            Executions.Clear();
                        }

                        ExecuteTest.Execute(null);
                    }
                    catch (Exception ex)
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Reopen {path} failed. Err: {ex.Message}");
                    }
                }
                else
                {
                    ActiveExecution = vm_exec;
                }
            }
            else
            {
                Executions.Clear();
                try
                {
                    if(Executions.Count > 0)
                    {
                        foreach(var exec in Executions)
                        {
                            exec.Stop.Execute(null);
                        }

                        Executions.Clear();
                    }

                    var script = Engine.LoadScriptFile(path);
                    if(obj is null) SoftwareOpened = null;   // Clear the Info_Software for opened a script file, if will be set in event;
                    Scripts.Clear();
                    Scripts.Add(script);
                    //ActiveScript = new ToucanCore.Engine.Script(script);

                    ExecuteTest.Execute(null);
                }
                catch (SpecFileNotFoundException sfnfe)
                {
                    var errmsg = $"Spec file not found. Please contact with Engineer";
                    if (IsEngineer)
                    {
                        var msg = "No Spec File Detect for RestrictLimit is True. Are you want to Generate One. All history data will be missed.\r\n未检测到Spec文件，是否需要重新生成. 历史数据将会丢失";
                        if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", msg, MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                        {
                            var specpath = System.IO.Path.Combine(sfnfe.Script.BaseDirectory, TF_Spec.DefaultFileName);
                            var spec = sfnfe.Script.AnalyzeSpec();
                            spec.Author = AuthService.UserName;
                            spec.Time = TimeService?.CurrentTime ?? DateTime.Now;
                            spec.Note = "Auto Generate By Toucan";
                            spec.UpdateDefectCode(sfnfe.Script.SystemConfig?.General?.Prefix_DefectCode ?? "D-");

                            spec.XmlSerialize().Save(specpath);

                            ActiveScript = new ToucanCore.Engine.Script(sfnfe.Script);

                            

                            FileInfo fi = new FileInfo(specpath);
                            fi.IsReadOnly = true;

                            var ie = Engine.CreateExecution(ActiveScript.OriginalScript);
                            ie.OnPreUUTing += (sender, e) => { Dispatcher.Invoke(() => { InputText = string.Empty; }); };
                            ie.OnTestCompleted += Execution_OnTestCompleted;
                            //ie.Start();
                        }
                        else
                        {
                            Message = errmsg;
                        }
                    }
                    else
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", errmsg);

                        return;
                    }
                }
                catch (SpecNotMatchException snme)
                {
                    if(snme.Script != null)
                    {
                        Scripts.Add(snme.Script);
                        ActiveScript = new ToucanCore.Engine.Script(snme.Script);
                    }
                    
                    if (IsEngineer)
                    {
                        var rs = DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Spec file does not match to script file, Do you want Merge spec or, Export current script settings to replace the spec file" 
                            ,MessageDialogStyle.AffirmativeAndNegativeAndSingleAuxiliary
                            , new MetroDialogSettings() { AffirmativeButtonText = "Merge", NegativeButtonText = "Export", FirstAuxiliaryButtonText="Cancel"});

                        if(rs == MessageDialogResult.Affirmative)
                        {
                            SpecMerge sm = new SpecMerge(snme.Original, snme.Target, snme.Script?.SystemConfig?.General?.Prefix_DefectCode);

                            sm.ShowDialog();

                            cmd_OpenScript(path);
                        }
                        else if(rs == MessageDialogResult.Negative)
                        {
                            SpecEditor se = new SpecEditor(snme.Target, AuthService, TimeService, snme.Script);
                            se.ShowDialog();
                        }
                        else
                        { 
                        }
                    }
                    else
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Spec file does not match to script file, Please Contact with Engineer");
                    }
                }
                catch(Exception ex)
                {
                    DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Open {path} failed. Err: {ex.Message}");
                }
            }
        }

        private void cmd_SaveScript(object obj)
        {
            if (ActiveScript is null) return;

            if(!ActiveScript.IsModified)
            {
                if(DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "The script content has no change, Are you sure to save it anyway?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { DefaultButtonFocus= MessageDialogResult.Negative}) == MessageDialogResult.Negative)
                {
                    return;
                }
            }

            try
            {
                if (ActiveScript.Save() > 0)
                {
                    if (SoftwareOpened is null)
                    {
                        if (ToolboxService.Station is null)  // even the software is empty, using the station setting anyway
                        {
                             Message = "Script Saved";
                        }
                        else
                        {
                            var psp = PspFile.PackWizard(UserName, ActiveScript.FilePath);
                            if (psp is null)
                            {
                                Message = $"Script Temporary Saved and Ignored packing as Psp file";
                            }
                            else 
                            {
                                var cu = ToolboxService.Customers.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.CustomerName);
                                var prj = cu?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.ProjectName);
                                var prd = prj?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.ProductName);
                                var sts = prd?.FirstOrDefault(x => x.Name == ActiveScript.StationConfig.StationName);

                                var stsstring = $"{ActiveScript.StationConfig.CustomerName}->{ActiveScript.StationConfig.ProjectName}->{ActiveScript.StationConfig.ProductName}->{ActiveScript.StationConfig.StationName}";
                                if(sts is null)
                                {
                                    if(DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning",$"{stsstring} does not exist, Do you want use Toolbox setting {ToolboxService.Customer.Name}->{ToolboxService.Project.Name}->{ToolboxService.Product.Name}->{ToolboxService.Station.Name} instead?", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                                    {
                                        sts = ToolboxService.Station;
                                    }
                                    else
                                    {
                                        Message = $"Illegal Station {stsstring}, Pushing software into Toolbox denied";
                                        return;
                                    }
                                }

                                ToolboxService.PushPspDialog(sts, psp, out Info_Software newsw);
                                if(newsw is null)
                                {
                                    Message = $"Script Saved {psp.FilePath} and ignore updating into remote";
                                    
                                }
                                else
                                {
                                    Message = $"Script Saved and Create new one into Remote as {newsw.UniqueName}";
                                }
                            }
                        }
                    }
                    else
                    {
                        ToolboxService.RepackAndSaveSoftware(SoftwareOpened, ActiveScript.FilePath, out Info_Software newsw);

                        if (newsw is null)
                        {
                            DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warn", $"Save Project Cacelled.");
                        }
                        else
                        {
                            Message = $"Script Saved and Push into Remote {newsw.UniqueName}";
                        }
                    }

                    try
                    {
                        ActiveExecution.Stop.Execute(null);
                        Executions.Clear();
                        //ExecuteTest.Execute(null);
                        OpenScript.Execute(ActiveScript.FilePath);
                    }
                    catch(Exception eex)
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", $"Save Project Passed, Reopen project failed. Err: {eex.Message}");
                    }
                }
            }
            catch(Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", $"Save Project Failed. Err: {ex.Message}");
            }
        }

        private void cmd_ShowSpec(object obj)
        {
            if (ActiveScript?.Spec is null)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"No Spec Detected");
                return;
            }

            var chk = ActiveScript?.Spec?.CheckValue;

            SpecEditor se = new SpecEditor(ActiveScript?.Spec, AuthService, TimeService, ActiveScript);

            se.ShowDialog();

            //ServiceStatic.ToolboxService().SpecEditorDialog(ActiveScript?.Spec);

            if (chk != ActiveScript?.Spec?.CheckValue)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Spec changes. Please reload the Project to make it be effective in Execution");
            }
        }

        string ResultTag;
        private void cmd_TagResult(object obj)
        {
            if (ActiveExecution is null)
            {
                Message = "Set Tag Failed. No Execution Found";
                return;
            }
            
            if(obj is string str)
            {
                ResultTag = str;   // Empty means clear Tag, null means customized
            }
            else if(obj is null)
            {
                ResultTag = DialogCoordinator.Instance.ShowModalInputExternal(this, "Input Result Tag", "Add Tag for following test results, Muliple Tag should be separated by \";\".\r\n Empty Tag will be ignored");
                if (string.IsNullOrEmpty(ResultTag)) return;
            }

            ApplyResultTag(ResultTag);
        }
        
        private void cmd_ExportAttachResultAs(object obj)
        {
            if (ActiveExecution is null)
            {
                Message = "Set Tag Failed. No Execution Found";
                return;
            }

            if(AttachResultInChart)
            {
                SaveFileDialog sfd = new SaveFileDialog();

                sfd.Title = "Save Attachdata into Local";
                sfd.Filter = "PTE Data|*.db3|CSV|*.csv";

                if(sfd.ShowDialog() == true)
                {
                    var ext = System.IO.Path.GetExtension(sfd.FileName);

                    if (ext.Equals(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var slot in ActiveExecution.Slots)
                        {
                            slot.AttachResults.ForEach((rs) => { rs.ExportTestDataCSV(sfd.FileName); });
                        }
                    }
                    else
                    {
                        // Save Into Database;
                    }
                }
                
            }
            else
            {
                Message = "Attach Result is OFF";
            }
        }

        private void ApplyResultTag(string resulttag)
        {
            if (string.IsNullOrEmpty(resulttag))
            {
                foreach (var rs in ActiveExecution.Execution.Results)
                {
                    foreach (var key in rs.AttachProperties.Keys)
                    {
                        if (key.StartsWith("ResultTag"))
                        {
                            rs.AttachProperties.Remove(key);
                        }
                    }
                }
            }
            else
            {
                var tags = resulttag.Split(';');
                for (int i = 0; i < tags.Length; i++)
                {
                    foreach (var rs in ActiveExecution.Execution.Results)
                    {
                        if (rs.AttachProperties.ContainsKey($"ResultTag{i}"))
                        {
                            rs.AttachProperties["ResultTag"] = tags[i];
                        }
                        else
                        {
                            rs.AttachProperties.Add($"ResultTag{i}", tags[i]);
                        }
                    }
                }
            }
        }

        private void cmd_ShowSpecMerge(object obj)
        {
            try
            {
                SpecMerge sm = new SpecMerge();

                sm.ShowDialog();
            }
            catch(Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show Spec Merge Failed. {ex}");
            }
        }

        private void cmd_ShowWebReportViewer(object obj)
        {
            try
            {
                WebReportViewer xml = new WebReportViewer();
                xml.Show();
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show Web Report Viewer Failed. {ex}");
            }
        }

        private void cmd_ShowNetworkHelper(object obj)
        {
            try
            {
                TestCore.UI.NetworkHelper nh = new NetworkHelper();
                nh.ShowDialog();
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show Network Helper Failed. {ex}");
            }
        }

        private void cmd_ShowTool(object obj)
        {
            try
            {
                if(obj is string toolname)
                {
                    switch(toolname)
                    {
                        case "MouseSimHelper":
                            MouseSimHelper msh = new MouseSimHelper();
                            msh.ShowDialog();
                            break;

                        case "SelfTest":
                            SelfTest nh = new SelfTest();
                            nh.ShowDialog();
                            break;

                        default:
                            DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Not Support {toolname} yet");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show Tool {obj} failed. {ex}");
            }
        }

        private void cmd_ShowSelfTest(object obj)
        {
            try
            {
                SelfTest nh = new SelfTest();
                nh.ShowDialog();
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show SelfTest Failed. {ex}");
            }
        }

        private void cmd_ShowEnvironmentHelper(object obj)
        {
            try
            {
                EnvironmentHelper eh = new EnvironmentHelper();
                eh.ShowDialog();
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show Environment Helper Failed. {ex}");
            }
        }

        private void cmd_ShowMesHelper(object obj)
        {
            try
            {
                MesHelper eh = new MesHelper(ActiveScript?.SystemConfig?.SFCs);
                eh.ShowDialog();
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Show Environment Helper Failed. {ex}");
            }
        }

        private void cmd_SetPhysicalStationID(object obj) 
        {
            var input = DialogCoordinator.Instance.ShowModalInputExternal(this, "Warning", "Input the Physical Station ID, such as 01, 02.\r\n\r\n It will clear the physical station id if empty");
            if(input != null)  // is null means cancel;
            {
                ServiceStatic.RootKey.SetValue(RK_ConstStationID, input.Trim());
            }
        }

        private void cmd_SetLineNo(object obj)
        {
            var input = DialogCoordinator.Instance.ShowModalInputExternal(this, "Warning", "Input the Line No.\r\n\r\n It will clear the Line No if empty");
            if (input != null)  // is null means cancel;
            {
                ServiceStatic.RootKey.SetValue(RK_ConstLineNo, input.Trim());
            }
        }

        static string asmname = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        private void cmd_SwitchLanguage(object obj)
        {
            if (obj is string lang)
            {
                ResourceDictionary rd_lang = null;
                string langset = null;
                if (lang == "中文")
                {
                    //var uri = new Uri(@"lang\zh-CN.xaml", UriKind.Relative);
                    //rd_lang = Application.LoadComponent(uri) as ResourceDictionary;

                    var uri = new Uri($"{asmname};component/lang/zh-CN.xaml", UriKind.RelativeOrAbsolute);
                    rd_lang = Application.LoadComponent(uri) as ResourceDictionary;
                    langset = "zh-CN";
                }
                //else if (lang == "ภาษาไทย")
                //{
                //    var uri = new Uri($"{asmname};component/lang/th-TH.xaml", UriKind.RelativeOrAbsolute);
                //    rd_lang = Application.LoadComponent(uri) as ResourceDictionary;
                //    langset = "th-TH";
                //}
                else
                {
                    //var uri = new Uri(@"lang\default.xaml", UriKind.Relative);
                    //rd_lang = Application.LoadComponent(uri) as ResourceDictionary;
                    var uri = new Uri($"{asmname};component/lang/default.xaml", UriKind.RelativeOrAbsolute);
                    rd_lang = Application.LoadComponent(uri) as ResourceDictionary;
                    langset = "default";
                }

                if (Language != langset)
                {
                    Language = langset;
                    TestCore.Services.ServiceStatic.RootKey.SetValue("lang", langset);
                }

                //var md = Application.Current.Resources.MergedDictionaries;
                var md = App.Current.Resources.MergedDictionaries;

                //var prelang = md.FirstOrDefault(x => x.Source?.OriginalString?.StartsWith(@"lang\") == true || x.Source is null);
                var prelang = md.FirstOrDefault(x => x.Source?.OriginalString?.StartsWith(@"lang\") == true);
                if (prelang != null)
                {
                    md.Remove(prelang);
                }

                md.Add(rd_lang);
            }
        }

        private void Execution_OnTestCompleted(object sender, TF_Result e)
        {
            Dispatcher.Invoke(() =>
            {
                switch (e.Result)
                {
                    case TF_ItemStatus.Passed:
                        PassCnt++;
                        break;
                    case TF_ItemStatus.Failed:
                        FailCnt++;
                        break;
                }
                TotalCnt++;
                Yield = PassCnt / (double)TotalCnt;
            });
        }
        
        private void ScriptFileChanged(object sender, FileSystemEventArgs e)
        {
            // For Monitoring if script changed. and if changed, notify user to save.
        }

        private void cmd_ProjectSetting(object obj)
        {
            if (!VerifyAction()) return;

            if (ActiveScript != null)
            {
                if (ActiveScript.SystemConfig is null)
                {
                    if (MessageBox.Show($"{ActiveScript.FilePath} is an original script. Are you sure to enable the project setting, which will enable Toucan Mode", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                    {
                        return;
                    }
                    var path = System.IO.Path.Combine(ActiveScript.BaseDirectory, GlobalConfiguration.DefaultFileName);

                    GlobalConfiguration.Default.Station = null;

                    if (Engine is ApEngineManager.ApxEngineManager)
                    {
                        GlobalConfiguration.Default.General.RestrictLimitOnlyForDefectCode = true;  // AP User prefer update Limit in AP Sequence
                    }

                    GlobalConfiguration.Default.Save(path);
                    ActiveScript.SystemConfig = GlobalConfiguration.Load(path);
                    
                }
                else if(ToolboxService.Station is IRemoteStation rs)
                {
                    if ( rs.RemoteConfig is GlobalConfiguration)
                    {
                        MessageBox.Show($"There is config for Station {ToolboxService.Station.Name} on Toolbox. Please Update it in Toolbox. Update here would be temporary");
                    }
                }

                TestCore.UI.Setting setting = new TestCore.UI.Setting();
                setting.UI_EditSpec_Trigged += (_, e) => { cmd_ShowSpec(null); };
                setting.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                setting.SystemSetting = ActiveScript.SystemConfig;
                setting.Variables = ActiveScript.Variables;

                setting.IsEditable = !ActiveScript.LockStatus;

                if (setting.ShowDialog() == true && ActiveExecution != null)
                {
                    setting.SystemSetting.Station.StationID = ToolboxService?.Station?.StationId ?? "01";
                    ActiveExecution?.UpdateUI(setting.SystemSetting);
                }
            }
        }

        private bool VerifyAction()
        {
            if (ActiveScript is null) return false;
            if (ActiveExecution?.Execution is null) return true;

            if(ActiveExecution?.Execution?.Results?.Any(x=> x.Status == TF_TestStatus.TESTING) == true)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Action Denied. Please redo after test completed");
                return false;
            }
            else
            {
                return true;
            }
        }

        private void cmd_HardwareSetting(object obj)
        {
            if (!VerifyAction()) return;

            if (obj is string path)
            {
                ActiveScript.HardwareConfig = HardwareConfig.Load(path);
            }

            try
            {
                ActiveExecution?.Execution.RemoveHardwareSetting();
            }
            catch
            {
                // the previous hardware setting might be wrong, which make removing exception as well
            }

            try
            {
                string defaultpath = System.IO.Path.Combine(ActiveScript.BaseDirectory, HardwareConfig.DefaultFileName);
                if (ActiveScript.HardwareConfig is null)
                {
                    if (File.Exists(defaultpath))
                    {
                        ActiveScript.HardwareConfig = HardwareConfig.Load(defaultpath);
                    }
                }

                HardwareSetting setting = new HardwareSetting() { SlotCount = ActiveScript?.SystemConfig?.General?.SocketCount ?? 1 };
                setting.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                setting.DataContext = ActiveScript.HardwareConfig ?? new HardwareConfig();
                if (ActiveScript.HardwareConfig?.FilePath is null)
                {
                    setting.DefaultFilePath = defaultpath;
                }

                setting.IsEditable = !ActiveScript.LockStatus;

                setting.ShowDialog();

                ActiveExecution?.Execution.ApplyHardwareSetting();  // No matter saved or not, need apply the HS

                if(ActiveScript.LockStatus == false && ActiveExecution?.Execution?.IsStarted == false)
                {
                    ActiveExecution.Start.Execute(null);
                }
            }
            catch(Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Open Hardware Setting Failed. {ex}");
            }
        }

        private void cmd_SlotVariableTableSetting(object obj)
        {
            if (!VerifyAction()) return;

            try
            {
                if(ActiveScript.InjectedVariableTable is null) 
                {
                    ActiveScript.InjectedVariableTable = new InjectedVariableTable(ActiveScript.SystemConfig?.General?.SocketCount ?? 1);
                }

                VariableTableBrowser vtb = new VariableTableBrowser(ActiveScript.InjectedVariableTable, ActiveScript.SystemConfig?.General?.SocketCount ?? 1);

                vtb.ShowDialog();
            }
            catch(NotImplementedException)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"Current Script {ActiveScript.GetType().Name} does not support Injected Variable Yet");
            }
            catch
            {
                
            }
        }

        private void cmd_GoldenSampleManager(object obj)
        {
            if (ActiveScript is null) return;
            try
            {
                GoldenSampleManager gsm = new GoldenSampleManager(ActiveScript);
                gsm.ShowDialog();
            }
            catch
            { }
        }
        
        private void cmd_GoldenSamplePick(object obj) 
        {
            if (ActiveExecution?.Execution?.Script?.GoldenSampleSpec is null)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", $"No Golden Sample Spec Detected, Please Initial a new one on Spec Editor");
                return;
            }

            var chk = ActiveExecution.Execution.Script.GoldenSampleSpec.CheckValue;

            SpecEditor se = new SpecEditor(ActiveExecution.Execution.Script.GoldenSampleSpec, AuthService, TimeService, ActiveScript, SpecType.GoldenSample);

            se.ShowDialog();

            if (chk != ActiveExecution.Execution.Script.GoldenSampleSpec.CheckValue)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Gold Sample Spec updated");
            }
        }

        private void cmd_ShowWorkbase(object obj)
        {
            if (ActiveScript is null) return;
            FileBrowser browser = new FileBrowser(new ProjectDirectory(ActiveScript.FilePath));
            browser.ShowDialog();
        }

        private void cmd_ShowReferenceBase(object obj)
        {
            if (ActiveScript is null) return;
            var refbase = ActiveScript.GetReferenceBase();
            if (Directory.Exists(refbase))
            {
                FileBrowser browser = new FileBrowser(new ProjectDirectory(refbase));
                browser.ShowDialog();
            }
            else
            {
                MessageBox.Show("Reference dir does not exist", "Warning");
            }
        }

        public bool IsClosed = false;
        private void cmd_Exit(object obj)
        {
            if (ActiveExecution?.Execution?.Results?.Any(x => x.Status == TF_TestStatus.TESTING) == true)
            {
                var rs = DialogCoordinator.Instance.ShowMessageAsync(this, "Warning", "Execution is still testing, are you sure to EXIT?", MessageDialogStyle.AffirmativeAndNegative);

                while (!rs.IsCompleted)
                {
                    if (!ActiveExecution.Execution.Results.Any(x => x.Status == TF_TestStatus.TESTING))
                    {
                        rs.Dispose();
                        break;
                    }
                }

                if (rs.IsCompleted && rs.Result == MessageDialogResult.Negative)
                {
                    return;
                }
            }

            ActiveExecution?.Execution?.Stop();

            foreach (var engine in Engines)
            {
                if (engine.IsStarted)
                {
                    engine.StopEngine();
                    engine.Dispose();
                }
            }

            AuthService?.StopAsync();
            AuthService?.Clear();
            AuthService?.Dispose();

            ReportService?.StopAsync();
            ReportService?.Clear();
            ReportService?.Dispose();

            ToolboxService?.CloseToolBoxUI();
            ToolboxService?.StopAsync();
            ToolboxService?.Clear();
            ToolboxService?.Dispose();

            App.Current.Shutdown();

            ToucanKey.SetValue("ShowRecordList", VM_Slot.ShowRecordList);
            ToucanKey.SetValue("ShowRecordChart", VM_Slot.ShowRecordChart);
            ToucanKey.Dispose();
            
            IsClosed = true;
        }

        private void cmd_Open(object obj)
        {
            if(ToolboxService?.IsStarted == true)
            {
                ToolboxService.ShowToolBoxUI();
            }
            else
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "ToolboxService is not ready, Please wait one more second and retry later");
            }
        }

        private void cmd_ShowRecordList(object obj)
        {
            VM_Slot.ShowRecordList = !VM_Slot.ShowRecordList;
            if (ActiveExecution?.Slots != null)
            {
                foreach (var slot in ActiveExecution.Slots)
                {
                    slot.VisibilityRecordList = VM_Slot.ShowRecordList ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void cmd_ShowRecordChart(object obj)
        {
            VM_Slot.ShowRecordChart = !VM_Slot.ShowRecordChart;

            if(ActiveExecution?.Slots !=null)
            {
                foreach(var slot in ActiveExecution.Slots)
                {
                    slot.VisibilityRecordChart = VM_Slot.ShowRecordChart ? Visibility.Visible: Visibility.Collapsed;
                }
            }
        }

        private void cmd_RestoreLocalReport(object obj)
        {
            try
            {
                RestoreLocalReportWizard rlrw = new RestoreLocalReportWizard(ReportService, ActiveExecution?.Script?.SystemConfig?.General?.Raw_ReportPath);
                rlrw.ShowDialog();
            }
            catch(Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Restore Local Report Failed", ex.Message);
                this.UILog(ex.ToString());
            }
        }

        public void InitService()
        {
            Dispatcher.Invoke(() =>
            {
                ToolboxService.Initialize();

                ToolboxService.ActOnRunSoftware += ToolboxService_ActOnRunSoftware;
                ToolboxService.ServiceStarted += (sender, args) =>
                {
                    ReportService = ToolboxService.GetService<IReportService>();

                    ReportService.ServiceWarning += (_,_args)=> { Dispatcher.Invoke(() => { Message = $"ReportService Warning. {_args.Message}"; }); };
                    ReportService.FilePushStarted += (_, _args) => { Dispatcher.Invoke(() => { Message = $"Pushing {_args.SourcePath} Start"; }); };
                    ReportService.FilePushCompleted += (_, _args)=> { Dispatcher.Invoke(() => { Message = $"Push {_args.SourcePath} Completed"; }); };

                    try
                    {
                        ReportService.StartAsync();
                    }
                    catch (Exception ex)
                    {
                        this.UILog($"Toolbox Started -- ReportService Err {ex.Message}");
                    }

                    AuthService = ToolboxService.GetService<IAuthService>();
                    AuthService.Initialize();
                    AuthService.UserInfoUpdated += (_, _args) => {
                        Dispatcher.Invoke(() =>
                        {
                            IsTester = AuthService.CurrentAuthType >= TestCore.AuthType.Tester;
                            IsLineLeader = AuthService.CurrentAuthType >= TestCore.AuthType.LineLeader;
                            IsMaintainer = AuthService.CurrentAuthType >= TestCore.AuthType.Maintainer;
                            IsEngineer = AuthService.CurrentAuthType >= TestCore.AuthType.Engineer;
                            IsAdmin = AuthService.CurrentAuthType >= TestCore.AuthType.Admin;

                            UserName = AuthService.UserName;
                            CurrentAuthType = AuthService.CurrentAuthType;

                            Engine?.Login(AuthService.UserName, AuthService.CurrentAuthType.ToString());
                        });
                    };
                    
                    AuthService.StartAsync();
                    Dispatcher.Invoke(() => 
                    {
                        ToolboxService.InitToolBoxUI(StationType.Toucan);
                        //this.UILog($"Toolbox Started -- Before Auth update");
                        IsTester = AuthService.CurrentAuthType >= TestCore.AuthType.Tester;
                        IsLineLeader = AuthService.CurrentAuthType >= TestCore.AuthType.LineLeader;
                        IsMaintainer = AuthService.CurrentAuthType >= TestCore.AuthType.Maintainer;
                        IsEngineer = AuthService.CurrentAuthType >= TestCore.AuthType.Engineer;
                        IsAdmin = AuthService.CurrentAuthType >= TestCore.AuthType.Admin;

                        UserName = AuthService.UserName;
                        CurrentAuthType = AuthService.CurrentAuthType;
                        //this.UILog($"Toolbox Started -- Auth updated");
                    });

                    TimeService = ToolboxService.GetService<ITimeService>();
                    TimeService.Initialize();
                    TimeService.StartAsync();

                    //if (AuthService.CurrentAuthType > AuthType.Tester)
                    //{
                    //    UserInfoUpdated(this, null);
                    //}

                    this.UILog("Toolbox Started");
                    //SwStatus = ToolboxService.CheckUpdateForApps("Toucan", "V2R0");
                };
                ToolboxService.StartAsync();
            });
        }

        public void InitTestEngine()
        {
            if (Engine.IsInitialized) return;

            Message = $"Starting {Engine.Name}, Please Wait";

            //Engine.OnEngineInitialized += Engine_OnInitializated;
            Engine.Initialize();

            Engine.OnEngineStarted += Engine_OnEngineStarted;
            Engine.OnScriptOpened += Engine_OnScriptOpened;
            Engine.OnExecutionCreated += Engine_OnExecutionCreated;
            Engine.OnExecutionStarted += Engine_OnExecutionStarted;
            Engine.OnReportGenerated += Engine_OnReportGenerated;
            
            Engine.StartEngine();
        }

        private void Engine_OnScriptOpened(object sender, IScript e)
        {
            try
            {
                ActiveScript = new ToucanCore.Engine.Script(e);
            }
            catch (SpecNotMatchException snme)
            {
                Dispatcher.Invoke(
                    () => 
                    {
                        if (DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", "Open Script Failed by Spec does not match. Do you remove the conflict one", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                        {
                            string specpath = System.IO.Path.Combine(e.BaseDirectory, TF_Spec.DefaultFileName);
                            if (File.Exists(specpath))
                            {
                                FileInfo fi = new FileInfo(specpath);
                                fi.IsReadOnly = false;
                                fi.Delete();
                            }

                            ActiveScript = new ToucanCore.Engine.Script(e);
                        }
                    }
                    );
            }
            catch(Exception ex)
            {
                TF_Base.StaticLog($"Engine_OnScriptOpened Failed. {ex}");
                Dispatcher.Invoke(() => { DialogCoordinator.Instance.ShowModalMessageExternal(this, "Open Script Failed", ex.Message); ; });
            }
        }

        private void Engine_OnReportGenerated(object sender, Tuple<TestCore.Data.TF_Result, string> e)
        {
            if (e?.Item1 != null)
            {
                if (File.Exists(e?.Item2))
                {
                    if (e.Item1?.SerialNumber?.Length > 2)
                    {
                        if(e.Item1.GeneralConfig.EnableTymReport)
                        {
                            var dir =System.IO.Path.GetDirectoryName(e.Item2);
                            var tympath = System.IO.Path.Combine(dir, e.Item1.GenerateReportName("tyml"));
                            e.Item1.XmlSerialize().Save(tympath);

                            ReportService?.Push(e.Item1, tympath);
                        }

                        if (e.Item1.GeneralConfig?.DisableRemoteReport == false)
                        {
                            Dispatcher.Invoke(() => { Message = $"report {e.Item2} has been generated"; });
                            ReportService?.Push(e.Item1, e.Item2);

                            if (e.Item1.AdditionalFiles != null)
                            {
                                foreach (var file in e.Item1.AdditionalFiles)
                                {
                                    if (string.IsNullOrWhiteSpace(file)) continue;

                                    try
                                    {
                                        ReportService.Push(e.Item1, file);
                                    }
                                    catch
                                    { }
                                }
                            }
                        }
                    }
                }

                if(EnableCsvReport)
                {
                    e.Item1.ExportTestDataCSV(e.Item1.GeneralConfig.ReportPath);
                }
            }
        }

        private void Engine_OnExecutionCreated(object sender, IExecution e)
        {
            if (e != null)
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        e.Template.Operator = UserName;  // for mes require op, which will initialize when construct VM_Exec

                        // Teststand the execution result is initialized after start
                        var vm_exec = new VM_Execution(e, ActiveScript) { Parent = this };

                        var sc = e.GetScript()?.SystemConfig;

                        if (sc is null)   // original model
                        {
                            e.Template.StationConfig = new StationConfig("CABU", ToolboxService?.Customer?.Name ?? "CU", ToolboxService?.Project?.Name ?? "PRJ", ToolboxService?.Product?.Name ?? "PRD", ToolboxService?.Station?.Name ?? "STS", ToolboxService?.StationInstance?.StationId ?? "01");

                            foreach (var rs in e.Results)
                            {
                                rs.StationConfig = e.Template.StationConfig;
                            }
                        }
                        else if (sc.Station.StationID is null)
                        {
                            sc.Station.StationID = ToolboxService?.StationInstance?.StationId ?? "01";
                        }
                        else
                        {
                            e.GetScript().StationConfig.StationID = ToolboxService?.StationInstance?.StationId;
                        }
                        e.Template.TestGuiVersion = Application.ResourceAssembly.GetName().Version.ToString();
                        
                        if (SoftwareOpened is Info_Software sw)
                        {
                            e.Template.TestSoftwareVersion = sw.Version;
                        }

                        //vm_exec.Execution.ApplyHardwareSetting();   // integrated into Start

                        e.GetScript().Spec.UpdateDefectCode(e.GetScript().SystemConfig?.General?.Prefix_DefectCode ?? "D-");

                        Executions.Clear();

                        ActiveExecution = vm_exec;    // Need Assign before check precondition, for the ref/ver/cal depends on the active execution
                        EnableInputText = vm_exec.EnableUserInteraction;

                        Executions.Add(vm_exec);

                        vm_exec.Execution.Start();
                    }
                    catch (Exception ex)
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Create Execution Failed", ex.Message);
                        ActiveExecution?.Stop?.Execute(null);
                        this.UILog(ex.ToString());
                    }
                });
            }
        }

        private void Engine_OnExecutionStarted(object sender, IExecution e)
        {
            if (e != null)
            {
                Dispatcher.Invoke(()=>
                {
                    try
                    {
                        //e.Template.Operator = UserName;  // for mes require op, which will initialize when construct VM_Exec

                        //var vm_exec = new VM_Execution(e) { Parent = this };

                        //var sc = e.GetScript()?.SystemConfig;

                        //if (sc is null)   // original model
                        //{
                        //    e.Template.StationConfig = new StationConfig("CABU", ToolboxService?.Customer?.Name ?? "CU", ToolboxService?.Project?.Name ?? "PRJ", ToolboxService?.Product?.Name ?? "PRD", ToolboxService?.Station?.Name ?? "STS", ToolboxService?.StationInstance?.StationId ?? "01");

                        //    foreach(var rs in e.Results)
                        //    {
                        //        rs.StationConfig = e.Template.StationConfig;
                        //    }
                        //}
                        //else if (sc.Station.StationID is null)
                        //{
                        //    sc.Station.StationID = ToolboxService?.StationInstance?.StationId ?? "01";
                        //}
                        //else
                        //{
                        //    e.GetScript().SystemConfig.Station.StationID = ToolboxService?.StationInstance?.StationId;
                        //}

                        ////vm_exec.Execution.ApplyHardwareSetting();   // integrated into Start

                        //e.GetScript().Spec.UpdateDefectCode(e.GetScript().SystemConfig?.General?.Prefix_DefectCode ?? "D-");

                        //Executions.Clear();

                        //ActiveExecution = vm_exec;    // Need Assign before check precondition, for the ref/ver/cal depends on the active execution
                        //EnableInputText = vm_exec.EnableUserInteraction;
                        //Station = ActiveExecution.Station;

                        //Executions.Add(vm_exec);

                        //ApplyResultTag(ResultTag);

                        ActiveExecution.Initialize();
                        ApplyResultTag(ResultTag);

                        if (CheckPreconditionData() <= 0)   // Check after execution started for the switch need Execution initied
                        {
                            ActiveExecution.Stop.Execute(null);
                            Message = "Execution start failed. Please Check the REF/VER/CAL data";

                            ActiveExecution = null;
                            Executions.Remove(ActiveExecution);
                            return;
                        }
                        else
                        {
                            Message = $"{Engine.Name} Start the Execution {e.Name} on {e.GetScript().FilePath}";
                            return;
                        }
                    }
                    catch(Exception ex)
                    {
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Start Execution Failed", ex.Message);
                        ActiveExecution?.Stop?.Execute(null);
                        this.UILog(ex.ToString());
                    }
                });
            }
        }

        private void Engine_OnEngineStarted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Message = $"{Engine.Name} Started";
                EngineVersion = Engine.Version;

                Engine?.Login(AuthService?.UserName, $"{AuthService?.CurrentAuthType}");
                //TYM_Apx_Engine.TYM_Apx_Engine.ApVisible = true;
            });
        }

        private void Engine_OnInitializated(object sender, EventArgs e)
        {
            if (AuthService?.CurrentAuthType > AuthType.Tester)
            {
                Engine?.Login(AuthService.UserName, AuthService.CurrentAuthType.ToString());
            }

            Message = $"{Engine.Name} Initialized";
        }

        private Info_Software SoftwareOpened { get; set; }

        private int ToolboxService_ActOnRunSoftware(Info_Software software, PcpFile[] pcp, Info_EquipmentInstance[] eqis)
        {
            var ext = System.IO.Path.GetExtension(software.Psp.SetupConfig.EntryPoint).ToLower();
            var entrypointpath = software.RunPreprocess(pcp, eqis);
            var swdest = System.IO.Path.GetDirectoryName(entrypointpath);

            if (!string.IsNullOrEmpty(ConstStationID))
            {
                if (ToolboxService.StationInstance.StationId != ConstStationID)
                {
                    Dispatcher.Invoke(() => {
                        var msg = $"Current Station ID {ToolboxService.StationInstance.StationId} does not match local physical ID {ConstStationID}, please contact with Engineer to update the Toolbox setting or Local Setting";
                        DialogCoordinator.Instance.ShowModalMessageExternal(this, "Warning", msg);
                    });
                    
                    return -1;
                }
            }

            OpenScript.Execute(entrypointpath);

            // For the Toolbox ExecuteSoftware might changed in 3rd app, Temporary store the current Software
            SoftwareOpened = software;
            return 1;
        }
    }
}
