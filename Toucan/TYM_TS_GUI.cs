using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCore;
using TestCore.Ctrls;
using TestCore.Data;
using TestCore.Configuration;
using System.IO;
using ToucanCore;
using System.Globalization;
using System.Diagnostics;
using TestCore.Services;
using TestCore.MetaData;

namespace Toucan
{
    public partial class Toucan : Form
    {
        public string BasePath = ".";
        private OpenFileDialog openFileDialog;

        Timer timer_StopExecution = new Timer();

        // Determine if quit when Toolbox unavailable
        internal bool Unlock { get; set; }

        private IToolboxService ToolboxService { get; }
        private IAuthService AuthService { get; }
        private ITimeService TimeService { get; }
        private IReportService ReportService { get; }
        private SoftwareStatus _swsts = SoftwareStatus.ServerUnreached;
        public SoftwareStatus SwStatus
        {
            get => _swsts;
            set
            {
                _swsts = value;
                SetSoftwareStatus();
            }
        }

        public TYM_TS_Engine Engine { get; internal set; }

        Task<int> Task_StartEngine;

        public Toucan()
        {
            try
            {
                InitializeComponent();

                FormClosing += TYM_TS_GUI_FormClosing;
                SizeChanged += TYM_TS_GUI_SizeChanged;

                Text = $"Toucan v{ProductVersion}";

                if (StationConfig.IpAddress is null)
                {
                    toolStripMenuItem_IP.BackColor = Color.Red;
                    toolStripMenuItem_IP.Text = "Offline";
                }
                else
                {
                    toolStripMenuItem_IP.BackColor = Control.DefaultBackColor;
                    toolStripMenuItem_IP.Text = StationConfig.IpAddress;
                }                

                if (GlobalConfiguration.Default.Station.Location == TestCore.Location.Vendor)
                {
                    toolStripMenuItem_Site.Text = GlobalConfiguration.Default.Station.Vendor;
                    toolStripMenuItem_Site.BackColor = Color.LightYellow;
                }
                else
                {
                    toolStripMenuItem_Site.Text = GlobalConfiguration.Default.Station.Location.ToString();
                }

                timer_seq_init.Interval = 200;
                timer_seq_init.Tick += Timer_seq_init_Tick;

                timer_StopExecution.Tick += Timer_StopExecution_Tick;
                timer_StopExecution.Interval = 100;

                ToolboxService = ServiceStatic.ToolboxService();
                if (ToolboxService is null)
                {
                    if (Unlock)
                    {
                        toolStripStatusLabel_RavenAddr.BackColor = Color.Red;  // if Ip Identified and service is null, probably the library compatibility issue
                        ReportIssueToolStripMenuItem.Enabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Toolbox invalid. Please Check if you Toolbox is correct");
                        Close();
                        return;
                    }                    
                }
                else
                {
                    ToolboxService.Initialize();
                    ToolboxService.InitToolBoxUI(TestCore.MetaData.StationType.Toucan);
                    ToolboxService.ActOnRunSoftware += ToolboxService_ActOnRunSoftware;
                    SwStatus = ToolboxService.CheckUpdateForApps("Toucan", "V1R1");
                    ReportIssueToolStripMenuItem.Enabled = true;

                    AuthService = ToolboxService?.GetService<IAuthService>();
                    ReportService = ToolboxService?.GetService<IReportService>();
                    TimeService = ToolboxService?.GetService<ITimeService>();
                }

                if (AuthService is null)
                { 
                    loginToolStripMenuItem.Enabled = false;
                }
                else
                {
                    AuthService.Initialize();
                    AuthService.UserInfoUpdated += UserInfoUpdated;
                }

                if (ReportService != null)
                {
                    ReportService.Initialize();

                    ReportService.ServiceWarning += ReportService_ServiceWarning;
                    ReportService.FilePushStarted += ReportService_FilePushStarted;
                    ReportService.FilePushCompleted += ReportService_FilePushCompleted;
                }

                if (TimeService != null)
                {
                    TimeService.Initialize();
                }
            }
            finally
            {
                ToucanCore.Misc.SplashScreen.Close();
            }

#if DEBUG
            openSequenceToolStripMenuItem.Enabled = true;
#endif
        }

        /// <summary>
        /// Init test engine, For Support Dynamic Engine, Should be initialized in program.cs
        /// </summary>
        public void InitializeEngine()
        {
            try
            {
                Engine.OnAsyncEngineStarted += Engine_OnAsyncEngineStarted;
                Engine.OnAsyncInitializated += Engine_OnAsyncInitializated;
                Engine.OnExecutionInitialized += Engine_OnExecutionInitialized;

                Engine.Initialize();
                Engine.ReportService = ReportService;
                Engine.ToolboxService = ToolboxService;
                Engine.TimeService = TimeService;
                Engine.AuthService = AuthService;
                
                Task_StartEngine = Engine.StartEngineAsyn();

                openFileDialog = new OpenFileDialog();
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = Engine.FileFilter;
            }
            catch(Exception ex)
            {
                UseWaitCursor = false;
                TF_Extension.UILog(this, $"Init Toucan Failed, Ex: {ex}");
                throw new InvalidOperationException($"Init Toucan Engine {Engine.Name} Failed, please check if the RunTime Engine install approperiatly");
            }
        }

        private void ReportService_FilePushCompleted(object sender, FileEventArgs args)
        {
            if (sender is IReportService service)
            {
                if (service.CountInQueue == 0)
                {
                    //toolStripStatusLabel_RavenAddr.Text = "Raven";
                    //toolStripStatusLabel_RavenAddr.BackColor = Color.LightGreen;
                    SetSoftwareStatus();
                }
            }
        }

        private void ReportService_FilePushStarted(object sender, FileEventArgs args)
        {
            if (sender is IReportService service)
            {
                toolStripStatusLabel_RavenAddr.Text = $"Pushing: {Path.GetFileName(args.SourcePath)}";
                toolStripStatusLabel_RavenAddr.BackColor = Color.LightYellow;
            }
        }

        private void ReportService_ServiceWarning(object sender, ServiceEventArgs args)
        {
            if (Engine.IsRunning)
            {
                if (sender is IReportService service)
                {
                    //MessageBox.Show(args?.Message ?? "Report Service Warning", "Warning");
                    toolStripStatusLabel_RavenAddr.Text = $"Report Service Warning: {Path.GetFileName(args.Message)}";
                    toolStripStatusLabel_RavenAddr.BackColor = Color.Red;
                }
            }
        }

        string SequnceFileDir = null;
        TestCore.MetaData.Info_Software InfoSoftware = null;

        private int ToolboxService_ActOnRunSoftware(TestCore.MetaData.Info_Software software, TestCore.Misc.PcpFile[] pcps, TestCore.MetaData.Info_EquipmentInstance[] eqis)
        {
            if (!Engine.IsReadyToRun)
            {
                MessageBox.Show("Engine is not ready, please try later.");
            }

            SequnceFileDir = Path.Combine(TestCore.Services.ServiceStatic.TempExecuteDir, $"{software.Name}_{DateTime.Now.ToString("MMdd_HHmmss")}");
            
            software.Psp.UnpackWithPcp(ref SequnceFileDir, pcps);

            var ext = Path.GetExtension(software.Psp.SetupConfig.EntryPoint).ToLower();
            var entrypointpath = Path.Combine(SequnceFileDir, software.Psp.SetupConfig.EntryPoint);

            toolStripMenuItem_StationID.Text = software.Station.StationId;

            if (string.IsNullOrWhiteSpace(software.Psp.SetupConfig.StartUpExec))
            {
                if (ext == ".exe" || ext == ".bat")
                {
                    using (Process proc = new Process())
                    {
                        proc.StartInfo.FileName = entrypointpath;
                        proc.StartInfo.Arguments = software.Psp.SetupConfig.Args;
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        proc.StartInfo.CreateNoWindow = true;

                        proc.Start();
                        return 1;
                    }
                }
                else if (ext == ".seq")
                {
                    _OpenSequenceFile(entrypointpath);
                }
                else
                {
                    using (Process proc = new Process())
                    {
                        proc.StartInfo.FileName = entrypointpath;

                        proc.StartInfo.Arguments = software.Psp.SetupConfig.Args;
                        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        proc.StartInfo.CreateNoWindow = true;

                        proc.Start();
                    }

                    toolStripStatusLabel_Info.Text = $"Exec {entrypointpath}";

                    return 1;
                }
            }
            else
            {
                if (File.Exists(software.Psp.SetupConfig.StartUpExec))
                {
                    using (System.Diagnostics.Process proc = new Process())
                    {
                        proc.StartInfo.FileName = software.Psp.SetupConfig.StartUpExec;
                        proc.StartInfo.Arguments = string.Format("\"{0}\" {1}", entrypointpath, software.Psp.SetupConfig.Args);
                        proc.StartInfo.UseShellExecute = false;
                        proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        proc.StartInfo.CreateNoWindow = true;

                        proc.Start();
                        return 1;
                    }
                }
                else
                {
                    throw new InvalidOperationException(string.Format("StartUpExec {0} does not exist", software.Psp.SetupConfig.StartUpExec));
                }
            }

            InfoSoftware = software;

            return 1;
        }

        private void Timer_seq_init_Tick(object sender, EventArgs e)
        {
            var rs = Engine?.Results?.FirstOrDefault();

            if (rs == null)
            {
                toolStripStatusLabel_Info.Text = "Result is null";
                timer_seq_init.Stop();
                return;
            }

            if (rs.Status == TF_TestStatus.IDLE)
            {
                toolStripStatusLabel_Info.Text = string.Format("Seq Initialized.");
                timer_seq_init.Stop();
            }
            else
            {
                toolStripStatusLabel_Info.Text = string.Format("Seq Initializing. Time: {0}", DateTime.Now);
            }
        }

        private string PspTemporaryPath = null;

        private void UserInfoUpdated(object sender, EventArgs e)
        {
            toolStripStatusLabel_User.Text = AuthService.UserName ?? "Op";
            Engine?.SetUserName(toolStripStatusLabel_User.Text);

            if (AuthService.CurrentAuthType == AuthType.Anonymous)
            {
                loginToolStripMenuItem.Text = "&Login...";
            }
            else
            {
                loginToolStripMenuItem.Text = "&Logout...";
                toolStripStatusLabel_Info.Text = $"Welcome, {toolStripStatusLabel_User.Text}. Auth: {AuthService.CurrentAuthType}";

                if (AuthService.CurrentAuthType >= AuthType.Tester)
                {
                    gotoCleanUpWhenFailureToolStripMenuItem.Enabled = true;
                    promoteWhenErrorToolStripMenuItem.Enabled = true;
                    openToolStripMenuItem.Enabled = true;

                    resumeToolStripMenuItem.Enabled = true;
                }
                else
                {
                    gotoCleanUpWhenFailureToolStripMenuItem.Enabled = false;
                    promoteWhenErrorToolStripMenuItem.Enabled = false;
                    openToolStripMenuItem.Enabled = false;

                    resumeToolStripMenuItem.Enabled = false;
                }

                if (AuthService.CurrentAuthType >= AuthType.Maintainer)
                {
                    configurationToolStripMenuItem.Enabled = true;
                    specEditorToolStripMenuItem.Enabled = true;
                    specMergeToolStripMenuItem.Enabled = true;

                    terminateAllToolStripMenuItem.Enabled = false;
                    abortAllToolStripMenuItem.Enabled = false;
                    breakOnFailureToolStripMenuItem.Enabled = true;
                    breakOnFirstStepToolStripMenuItem.Enabled = true;

                    if (Engine.IsInitialized)
                    {
                        if (Engine.TS_AppMgr.GetEngine().CurrentUser is null)
                        {
                            var userobj = Engine.TS_AppMgr.GetEngine().GetUser("administrator");
                            //TS_AppMgr.GetEngine().DisplayLoginDialog("Login", "administrator", "", true, out User userobj);
                            Engine.TS_AppMgr.GetEngine().CurrentUser = userobj;
                        }
                    }
                }
                else
                {
                    configurationToolStripMenuItem.Enabled = false;
                    specEditorToolStripMenuItem.Enabled = false;
                    specMergeToolStripMenuItem.Enabled = false;

                    terminateAllToolStripMenuItem.Enabled = false;
                    abortAllToolStripMenuItem.Enabled = false;
                    breakOnFailureToolStripMenuItem.Enabled = false;
                    breakOnFirstStepToolStripMenuItem.Enabled = false;

                    //if (Engine.IsInitialized)
                    //{
                    //    Engine.TS_AppMgr.Logout();
                    //}
                }

                if (AuthService.CurrentAuthType >= AuthType.Engineer)
                {
                    sFCsToolStripMenuItem.Enabled = true;

                    toolStripMenuItem_GotoSeqDir.Enabled = true;
                    ToolStripMenuItem_RepackAndCommit.Enabled = true;

                    openSequenceToolStripMenuItem.Enabled = true;
                }
                else
                {
                    sFCsToolStripMenuItem.Enabled = false;

                    toolStripMenuItem_GotoSeqDir.Enabled = false;
                    ToolStripMenuItem_RepackAndCommit.Enabled = false;

                    openSequenceToolStripMenuItem.Enabled = false;
                }
            }
        }

        private void Engine_OnExecutionInitialized(object sender, EventArgs e)
        {
            Text = string.Format("{0} v{1}    Spec: {2} Ver: {3}    SeqVer: {4}", "Toucan", ProductVersion, Engine.ResultTemplate?.Specification?.Name, Engine.ResultTemplate?.Specification?.Version, Engine.ResultTemplate?.TestSoftwareVersion);

            label_SeqVer.Text = Engine.ResultTemplate?.TestSoftwareVersion;
            label_SpecVer.Text = Engine.ResultTemplate?.Specification?.Version;
            label_SpecName.Text = Engine.ResultTemplate?.Specification?.Name;

            Toucan_Utility.AutoResizePanelCtrl(panel1, Engine.SlotCtrls);

            toolStripStatusLabel_Info.Text = "Execution initialized";
            TF_Base.StaticLog("Execution initialized");
        }

        private void Engine_OnAsyncInitializated(object sender, EventArgs e)
        {
            label_StationName.Text = GlobalConfiguration.Default.Station.StationName;

            var d = Engine.StartEngineAsyn();
        }

        private void Engine_OnAsyncEngineStarted(object sender, EventArgs e)
        {
            ToucanCore.Misc.SplashScreen.Close();
            Activate();

            label_StationName.Text = GlobalConfiguration.Default.Station.StationName;
            toolStripStatus_Version.Text = Engine.Version;

            if (GlobalConfiguration.Default.General.InputUserName)
            {
                if (!File.Exists(BasePath))
                {
                    ToolboxService?.ShowToolBoxUI();
                }
            }

            UseWaitCursor = false;

            if (!Engine.IsReadyToRun)
            {
                toolStripStatusLabel_Info.Text = $"Start Engine {Engine.Name} Failed";
                return;
            }

            if (File.Exists(BasePath))
            {
                string filepath = BasePath;

                switch (Path.GetExtension(BasePath).ToLower())
                {
                    case ".seq":
                        this.BeginInvoke(new Action(() => { _OpenSequenceFile(filepath); }));
                        toolStripStatusLabel_Info.Text = filepath;
                        break;

                    case ".psp":
                        this.BeginInvoke(new Action(() => { _OpenPspFile(filepath); }));
                        break;
                }

                BasePath = Path.GetDirectoryName(BasePath);
            }

            if (Directory.Exists(BasePath))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(BasePath);
            }
            else
            {
                openFileDialog.InitialDirectory = ".";
            }

            toolStripStatusLabel_Info.Text = "Engine Started";
        }

        /// <summary>
        /// Init env when sequence open 
        /// </summary>
        public void Initialize()  
        {
            toolStripMenuItem_Site.Text = GlobalConfiguration.Default.Station.Location.ToString();

            toolStripMenuItem_Customer.Text = Engine.ResultTemplate?.StationConfig?.CustomerName;
            toolStripMenuItem_Product.Text = Engine.ResultTemplate?.StationConfig?.ProductName;
            toolStripMenuItem_StationID.Text = Engine.ResultTemplate?.StationConfig?.StationID;

            label_StationName.Text = Engine.ResultTemplate?.StationConfig?.StationName;

            try
            {
                Engine.BreakOnFirstStep = breakOnFirstStepToolStripMenuItem.Checked = false;
                Engine.BreakOnFirstFailure = breakOnFailureToolStripMenuItem.Checked = GlobalConfiguration.Default.General.AutoBreakWhenFailure;
                Engine.AlwaysGotoCleanupOnFailure = gotoCleanUpWhenFailureToolStripMenuItem.Checked = GlobalConfiguration.Default.General.AutoCleanupWhenFailure;

                Engine.DisableResults = false;
                Engine.ActionOnError = GlobalConfiguration.Default.General.ErrorHandle;
                promoteWhenErrorToolStripMenuItem.Checked = GlobalConfiguration.Default.General.ErrorHandle == 1;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error");
            }
        }

        int stopexecutioncnt = 0;
        private void Timer_StopExecution_Tick(object sender, EventArgs e)
        {
            stopexecutioncnt++;

            if (Engine.IsRunning && stopexecutioncnt <= 50)
            {
                  
            }
            else
            {
                timer_StopExecution.Stop();
                
                if (Engine.IsRunning)
                {
                    Engine.StopExecution();
                }
                else
                {
                    MessageBox.Show($"stop elapsed {stopexecutioncnt * timer_StopExecution.Interval} ms", "");
                }

                stopexecutioncnt = 0;
            }
        }
        
        public void OpenSequenceFile()
        {
            try
            {
                if (Engine.IsRunning)
                {
                    try
                    {
                        if (MessageBox.Show("Execution is running, Do you want to STOP Current and Start a new one?", "Warning", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Engine.FinishTest(-1);
                        }
                        else
                        {
                            return;
                        }
                    }
                    catch
                    { }
                }

                openFileDialog.Filter = Engine.FileFilter;

                var rs = openFileDialog.ShowDialog();

                if (rs == DialogResult.OK)
                {
                    if (Engine.IsRunning)
                    {
                        timer_StopExecution.Start();
                    }

                    toolStripMenuItem_StationID.Text = string.Empty;
                    _OpenSequenceFile(openFileDialog.FileName);

                    toolStripStatusLabel_Info.Text = openFileDialog.FileName;
                }
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException ioe && AuthService?.CurrentAuthType >= AuthType.Engineer)
                {
                    if (MessageBox.Show("New Item in Sequnce Found. Do you want to merge into.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        var sar = Engine.AnalyzeScript();

                        ToolboxService.SpecMergeDialog(TF_Spec.LoadFromXml(Path.Combine(Path.GetDirectoryName(Engine.ScriptFilePath), TF_Spec.DefaultFileName)), sar.Spec);

                        return;
                    }
                }

                toolStripStatusLabel_Info.Text = $"Open Sequence Failed. Err: {ex.Message}";
                TF_Extension.UILog(this, toolStripStatusLabel_Info.Text);
                tb_SN.Enabled = false;
                btn_OK.Enabled = false;
            }
        }

        private string SubSnType;
        private int SubSnLength;

        private void _OpenSequenceFile(string filepath)
        {
            tb_SN.Enabled = false;
            btn_OK.Enabled = false;

            Engine.LoadScriptFile(filepath);

            if (Engine.IsOriginalModel || Engine.CustomizeInputSn)
            {
            }
            else
            {
                tb_SN.Enabled = true;
                btn_OK.Enabled = true;
            }

            if (GlobalConfiguration.Default.General.RunMode == RunMode.Batch)
            {
                var format = GlobalConfiguration.Default.General.SubSN;

                if (string.IsNullOrEmpty(format))
                {
                    format = "d2";
                }

                SubSnType = format.ElementAt(0).ToString();
                SubSnLength = int.Parse(format.ElementAt(1).ToString());
            }

            if (!Engine.IsOriginalModel)
            {
                if (string.IsNullOrEmpty(toolStripMenuItem_StationID.Text))
                {
                    toolStripMenuItem_StationId_Click(null, null); // Confirm StationId
                }
            }

            Engine.StartExecution();
            Initialize();

            DateTime t0 = DateTime.Now;
            timer_seq_init.Stop();
            timer_seq_init.Start();

            toolStripStatusLabel_SequenceName.Tag = filepath;
            toolStripStatusLabel_SequenceName.Text = Path.GetFileName(filepath);
            SequnceFileDir = Path.GetDirectoryName(filepath);
        }

        private void _OpenPspFile(string filepath)
        {
            if (Directory.Exists(PspTemporaryPath))
            {
                Directory.Delete(PspTemporaryPath, true);
            }

            TestCore.Misc.PspFile psp = new TestCore.Misc.PspFile(filepath);

            PspTemporaryPath = Path.Combine(RemoteServiceConfig.TempDownloadDir, Path.GetFileNameWithoutExtension(filepath));

            try
            {
                GlobalConfiguration.Default.Reload(Directory.GetFiles(PspTemporaryPath).First(x => Path.GetFileName(x).ToLower() == "system.xml"));
            }
            catch
            {
            }

            var entry = Path.Combine(PspTemporaryPath, psp.SetupConfig.EntryPoint);

            if (!File.Exists(entry))
            {
                entry = Directory.GetFiles(PspTemporaryPath).FirstOrDefault(x => Path.GetExtension(x).ToLower() == ".seq");
                TF_Base.StaticLog(string.Format("PSP Entry {0} not found", entry));
            }

            _OpenSequenceFile(entry);

            toolStripStatusLabel_Info.Text = "Open PspFile OK";
        }
        #region Form_Event
        private void TYM_TS_GUI_SizeChanged(object sender, System.EventArgs e)
        {
            if (Engine?.SlotCtrls?.FirstOrDefault() != null)
            {
                Toucan_Utility.AutoResizePanelCtrl(panel1, Engine.SlotCtrls);
            }
        }

        private void TYM_TS_GUI_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            if (MessageBox.Show("Are You Sure To Exit?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    ToolboxService?.CloseToolBoxUI();

                    if (Engine.FinishTest(-1) <= 0)
                    {
                        if (MessageBox.Show("Close Execution Failed. Click OK to Terminate it?", "Warn") == DialogResult.Yes)
                        {
                            Engine.StopExecution();
                        }
                    }
                    Engine.StopEngine();
                }
                finally
                {
                    if (Directory.Exists(PspTemporaryPath))
                    {
                        Directory.Delete(PspTemporaryPath, true);
                    }
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void SetSoftwareStatus()
        {
            switch (SwStatus)
            {
                case SoftwareStatus.CouldBeUpdated:
                    toolStripStatusLabel_RavenAddr.Text = "New Version Available";
                    toolStripStatusLabel_RavenAddr.BackColor = Color.GreenYellow;
                    break;

                case SoftwareStatus.Effective:
                    toolStripStatusLabel_RavenAddr.Text = "Effective";
                    toolStripStatusLabel_RavenAddr.BackColor = Color.LightGreen;
                    break;

                case SoftwareStatus.NoVersionStored:
                    toolStripStatusLabel_RavenAddr.Text = "No Version Stored";
                    toolStripStatusLabel_RavenAddr.BackColor = Color.LightYellow;
                    break;

                case SoftwareStatus.OnDebug:
                    toolStripStatusLabel_RavenAddr.Text = "On Debug";
                    toolStripStatusLabel_RavenAddr.BackColor = Color.GreenYellow;
                    break;

                case SoftwareStatus.ServerUnreached:
                    toolStripStatusLabel_RavenAddr.Text = "Server Unreached";
                    toolStripStatusLabel_RavenAddr.BackColor = Color.LightYellow;
                    break;
            }
        }

        private void TYM_TS_GUI_Load(object sender, EventArgs e)
        {
            if (File.Exists(BasePath)) return;
            UseWaitCursor = true;
        }
#endregion

        private void ExecutionRestartAction(object sender, EventArgs e)
        {
            MessageBox.Show("Reserved");
        }        

        private SlotInfo CurrentSlotCtrl;

        private void btn_OK_Click(object sender, EventArgs e)
        {
            try
            {
                if (Engine.SlotCtrls is null || Engine.SlotCtrls.Length == 0)
                {
                    MessageBox.Show("Please Load Sequence first");
                    return;
                }

                string context = GlobalConfiguration.Default.General.AutoCapitalSn ? tb_SN.Text.ToUpper() : tb_SN.Text;

                if (GlobalConfiguration.Default.General.AutoCapitalSn)
                {
                    tb_SN.Text = context;
                }

                TF_Base.StaticLog($"Action: OK Clicked: {context}");

                switch (GlobalConfiguration.Default.General.InputSnMode)
                {
                    case InputSnMode.Inherit:
                        InputSnInInherit(context);
                        break;

                    case InputSnMode.Normal:
                        InputSnInNormal(context);
                        break;

                    case InputSnMode.PostfixByMainSn:
                        InputSnInPostfixByMainSn(context);
                        break;

                    case InputSnMode.Simple:
                        InputSnInSimple(context);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Execution not ready. Please Input Later. Ex: {0}", ex));
                TF_Base.StaticLog(ex.ToString());
            }
            finally
            {
                tb_SN.Focus();
                tb_SN.SelectAll();
            }
        }

        #region Interaction flow
        public virtual void InputSnInInherit(string context)
        {
            if (Toucan_Utility.IsSerialNumber(context) < 0) return; 

            foreach (var slot in Engine.SlotCtrls)
            {
                if (CheckSlotCtrl(slot) < 0)
                {
                    return;
                }

                if (slot.EnableTest)
                {
                    slot.Result.Status = TF_TestStatus.WAIT_DUT;
                    slot.SetSerialNumber(context);
                    SendStartMessage(slot.Index);
                }
            }

            //if (GlobalConfiguration.Default.General.RunMode == TestCore.RunMode.Batch)
            if(Engine.IsBatchModel)
            {
                SendStartMessage(-1);
            }

            tb_SN.Clear();
        }

        public virtual void InputSnInSimple(string context)
        {
            if (Toucan_Utility.IsSerialNumber(context) < 0) return;

            if (CurrentSlotCtrl is null)
            {
                CurrentSlotCtrl = Engine.SlotCtrls.FirstOrDefault(x => x.EnableTest);
            }

            if (CurrentSlotCtrl is null)
            {
                MessageBox.Show("Unavailable Slot. Please Enable any Slot and Rescan the SerialNumber.\r\n无可用槽位，请使能任一槽位并重新输入");
            }
            else
            {
                if (CheckSlotCtrl(CurrentSlotCtrl) < 0) return;

                var funccode = Toucan_Utility.IsFunction(context);
                if (funccode != BarcodeFunction.ILLEGAL_CMD)
                {
                    switch (funccode)
                    {
                        case BarcodeFunction.TESTFINISH:
                            CurrentSlotCtrl.Result.End();
                            CurrentSlotCtrl.Result.Status = TF_TestStatus.PASSED;
                            return;

                        case BarcodeFunction.TERMINATE:
                            CurrentSlotCtrl.Result.End();
                            CurrentSlotCtrl.Result.Status = TF_TestStatus.TERMINATED;
                            return;

                        case BarcodeFunction.TESTSTART:
                            CurrentSlotCtrl.Result?.Begin();
                            CurrentSlotCtrl.Result.Status = TF_TestStatus.TESTING;
                            return;

                        case BarcodeFunction.ENTER_VERIFICATION:
                            Engine.IsForVerification = true;
                            return;

                        case BarcodeFunction.QUIT_VERIFICATION:
                            Engine.IsForVerification = false;
                            return;

                        default:
                            break;
                    }
                }

                CurrentSlotCtrl.SetSerialNumber(context);

                CurrentSlotCtrl.Result.Status = TF_TestStatus.WAIT_DUT;

                if (SendStartMessage(CurrentSlotCtrl.Index) > 0)
                {
                    toolStripStatusLabel_Info.Text = $"SN {context} --> Slot {CurrentSlotCtrl.Index}";

                    CurrentSlotCtrl?.Deactivate();

                    var islast = Engine.SlotCtrls.LastOrDefault(x => x.EnableTest)?.Index == CurrentSlotCtrl.Index;

                    if (islast)
                    {
                        CurrentSlotCtrl = null;

                        //if (GlobalConfiguration.Default.General.RunMode == TestCore.RunMode.Batch)
                        if(Engine.IsBatchModel)
                        {
                            SendStartMessage(-1);
                        }
                    }
                    else
                    {
                        int startidx = CurrentSlotCtrl.Index + 1;
                        CurrentSlotCtrl = null;
                        for (int i = startidx; i < Engine.SlotCtrls.Length; i++)
                        {
                            if (Engine.SlotCtrls[i].EnableTest)
                            {
                                CurrentSlotCtrl = Engine.SlotCtrls[i];
                                CurrentSlotCtrl.Activate();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    toolStripStatusLabel_Info.Text = $"Set SN {context} in Slot {CurrentSlotCtrl.Index} Failed. Please Input Again";
                }

                tb_SN.Clear();
            }
        }

        public virtual void InputSnInPostfixByMainSn(string context)
        {
            if (Toucan_Utility.IsSerialNumber(context) < 0) return;

            foreach (var slot in Engine.SlotCtrls)
            {
                if (slot.EnableTest)
                {
                    if (slot.Result.Status == TF_TestStatus.TEST_INIT)
                    {
                        MessageBox.Show(string.Format("Socket {0} is initializing, please wait...\r\n槽位{0}正在初始化，请稍后...", slot.Index));
                        return;
                    }
                    else if (slot.Result.Status == TF_TestStatus.TESTING)
                    {
                        MessageBox.Show(string.Format("Socket {0} is testing, please wait...\r\n槽位{0}正在初始化，请稍后...", slot.Index));
                        return;
                    }
                    else if (slot.Result.Status == TF_TestStatus.WAIT_DUT)
                    {
                        MessageBox.Show(string.Format("Socket {0} is Waiting DUT, please wait...\r\n槽位{0}正在上传Log或等待PreUUT执行，请稍后...", slot.Index));
                        return;
                    }
                }
            }

            const int SubSnLength = 2;  // TODO

            if (GlobalConfiguration.Default.General.RunMode == TestCore.RunMode.Batch)
            {
                foreach (var slot in Engine.SlotCtrls)
                {
                    if (slot.EnableTest)
                    {
                        if (GlobalConfiguration.Default.General.RE_SubSn is null)
                        {
                            slot.SetSerialNumber(context + (slot.Index + 1).ToString().PadLeft(SubSnLength, '0'));
                        }
                        else
                        {
                            MessageBox.Show("Not support Regex in InputSnInPostfixByMainSn yet");
                            return;
                        }
                    }
                }

                SendStartMessage();
            }
            else
            {
                foreach (var slot in Engine.SlotCtrls)
                {
                    if (slot.EnableTest)
                    {
                        if (GlobalConfiguration.Default.General.RE_SubSn is null)
                        {
                            slot.SetSerialNumber(context + (slot.Index + 1).ToString().PadLeft(SubSnLength, '0'));
                        }
                        else
                        {
                            MessageBox.Show("Not support Regex in InputSnInPostfixByMainSn yet");
                            return;
                        }

                        SendStartMessage(slot.Index);
                    }
                }
            }

            tb_SN.Clear();
        }

        public virtual void InputSnInNormal(string context)
        {
            if (Engine.SlotCtrls.Length == 1)
            {
                CurrentSlotCtrl = Engine.SlotCtrls[0];

                tb_SN.Focus();
                tb_SN.SelectAll();
            }
            else
            {
                var slotid =  Toucan_Utility.IsSocketNumber(context);

                if (slotid >= 0)
                {
                    CurrentSlotCtrl?.Deactivate();
                    CurrentSlotCtrl = Engine.SlotCtrls.FirstOrDefault(x => x.Index == slotid);
                    
                    if (CurrentSlotCtrl is SlotInfo slot)
                    {
                        if (slot.Enabled == false)
                        {
                            MessageBox.Show("当前槽位已不可用，请联系工程师 或 使用其他槽位继续测试。\r\n", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            CurrentSlotCtrl = null;
                            return;
                        }
                        else if (!slot.EnableTest)
                        {
                            MessageBox.Show("当前槽位已被禁用，请联系工程师使能该槽位 或 使用其他槽位继续测试。\r\n", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            CurrentSlotCtrl = null;
                            return;
                        }

                        slot.Activate();
                        return;
                    }
                }
            }

            var funccode = Toucan_Utility.IsFunction(context);
            if (funccode != BarcodeFunction.ILLEGAL_CMD)
            {
                switch (funccode)
                {
                    case BarcodeFunction.TESTFINISH:
                        CurrentSlotCtrl.Result.End();
                        CurrentSlotCtrl.Result.Status = TF_TestStatus.PASSED;
                        return;

                    case BarcodeFunction.TERMINATE:
                        CurrentSlotCtrl.Result.End();
                        CurrentSlotCtrl.Result.Status = TF_TestStatus.TERMINATED;
                        return;

                    case BarcodeFunction.TESTSTART:
                        CurrentSlotCtrl.Result?.Begin();
                        CurrentSlotCtrl.Result.Status = TF_TestStatus.TESTING;
                        return;

                    case BarcodeFunction.ENTER_VERIFICATION:
                        Engine.IsForVerification = true;
                        return;

                    case BarcodeFunction.QUIT_VERIFICATION:
                        Engine.IsForVerification = false;
                        return;

                    default:
                        break;
                }
            }

            if (CheckSlotCtrl(CurrentSlotCtrl) < 0) return;

            if (Toucan_Utility.IsSerialNumber(context) >= 0)
            { 
                if (Engine.SlotCtrls.Length > 1)
                {
                    CurrentSlotCtrl.Deactivate();
                }

                CurrentSlotCtrl.SetSerialNumber(context);

                CurrentSlotCtrl.Result.Status = TF_TestStatus.WAIT_DUT;
                SendStartMessage(CurrentSlotCtrl.Index);

                CurrentSlotCtrl = null;

                if (GlobalConfiguration.Default.General.RunMode == TestCore.RunMode.Batch)
                {
                    if (Engine.SlotCtrls.All(x => !x.Enabled || x.Result.Status == TF_TestStatus.WAIT_DUT))
                    {
                        SendStartMessage(-1);
                    }
                }
            }

            tb_SN.Focus();
            tb_SN.Clear();
        }

        private int CheckSlotCtrl(SlotInfo slot)
        {
            if (slot == null)
            {
                MessageBox.Show(string.Format("Warning!!! Please Scan the SocketIndex Barcode First.\nYour Input is {0}", tb_SN.Text), "Warning");
                tb_SN.Focus();
                tb_SN.SelectAll();
                return -1;
            }

            if (slot.EnableTest)
            {
                if (slot.Result.Status == TF_TestStatus.TEST_INIT)
                {
                    MessageBox.Show(string.Format("Socket {0} is initializing, please wait...\r\n槽位{0}正在初始化，请稍后...", slot.Index));
                    return -2;
                }
                else if (slot.Result.Status == TF_TestStatus.TESTING)
                {
                    MessageBox.Show(string.Format("Socket {0} is testing, please wait...\r\n槽位{0}正在测试中，请稍后...", slot.Index));
                    return -3;
                }
                else if (slot.Result.Status == TF_TestStatus.WAIT_DUT)
                {
                    MessageBox.Show(string.Format("Socket {0} is Waiting DUT, please wait...\r\n槽位{0}正在上传Log或等待PreUUT执行，请稍后...", slot.Index));
                    return -4;
                }
            }
            else
            {
                return -5;
            }

            return 1;
        }

        protected virtual int SendStartMessage(int slotindex = -1)
        {
            return Engine.StartNewTest("Joey.Zhou", slotindex);
        }
        #endregion
    }
}
