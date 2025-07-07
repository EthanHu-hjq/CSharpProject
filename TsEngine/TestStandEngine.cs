using NationalInstruments.TestStand.Interop.API;
using NationalInstruments.TestStand.Interop.UI.Ax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using TestCore.Services;
using ToucanCore;
using ToucanCore.Abstraction.Engine;
using TsEngine.UIs;

namespace TsEngine
{
    public partial class TestStandEngine : TF_Base, ToucanCore.Abstraction.Engine.IEngine<Execution, Script>
    {
        public const string ConstName = "TestStand Engine";

        public static string CalibrationBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Calibration");
        public static string ReferenceBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Reference");
        public static string VerificationBase { get; } = System.IO.Path.Combine(ServiceStatic.RootDataDir, $"{ConstName}_Verification");

        internal Form Parent { get; } = new Form();  //Host for WPF
        internal System.ComponentModel.ComponentResourceManager Resources { get; }
        public string Name => ConstName;

        public string Version { get; private set; } = "1.0";

        public string UserName { get; set; }

        public string StationId { get; set; }

        public string FileFilter => "TestStand Sequence|*.seq";

        public bool IsInitialized { get; protected set; }

        public bool IsStarted { get; protected set; }

        //public bool IsRunning { get; protected set; }

        public bool IsForVerification { get; set; }

        public bool BreakOnFirstStep { get; set; }

        public bool BreakOnFailure { get; set; }

        public bool GotoCleanupOnFailure { get; set; }

        public bool DisableResults { get; set; }

        public int ActionOnError { get; set; }

        private Dictionary<string, object> _Variables = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> Variables { get => _Variables; }

        public IReadOnlyCollection<Execution> Executions { get=> _Executions; }
        private List<Execution> _Executions = new List<Execution>();

        public IReadOnlyCollection<Script> Scripts { get; } = new List<Script>();

        public IModel Model => throw new NotImplementedException();

        private bool _UiVisible;
        public bool UiVisible { get=>_UiVisible; set { ShowTestUI(value); } }

        public bool IsEditMode { get; set; }

        private void ShowTestUI(bool value)
        {
            _UiVisible = value;

            if (value)
            {
                TestStandHelper.ExecutionUi.InitialExecution(Executions.FirstOrDefault(), IsEditMode);
                TestStandHelper.ExecutionUi.Show();
            }
            else
            {
                TestStandHelper.ExecutionUi.Hide();
            }
        }

        public event EventHandler OnEngineInitialized;
        public event EventHandler OnEngineStarted;
        public event EventHandler OnEngineStopped;
        public event EventHandler<IExecution> OnExecutionCreated;
        public event EventHandler<IExecution> OnExecutionStarted;
        public event EventHandler<IExecution> OnExecutionStopped;
        public event EventHandler<Tuple<TF_Result, string>> OnReportGenerated;
        public event EventHandler CalibrationExpired;
        public event EventHandler CalibrationExpiring;
        public event EventHandler<IScript> OnScriptOpened;

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

        public int AbortAll()
        {
            TestStandHelper.Engine.AbortAll();
            return 1;
        }

        public void Dispose()
        {
            TestStandHelper._ExecutionUi?.ForceClose();
            TestStandHelper._ExecutionUi?.Dispose();
        }

        public int FormatScript()
        {
            throw new NotImplementedException();
        }

        public int GenerateReport(TF_Result rs, string basepath)
        {
            throw new NotImplementedException();
        }

        public TestStandEngine()
        {
            Execution.StaticEngine = Script.StaticEngine = this;
        }

        public int Initialize()
        {
            if (IsInitialized) return 1;
            
            IsInitialized = true;
            return 1;
        }

        public IScript LoadScriptFile(string path)
        {
            if(Executions?.Count > 0 )
            {
                foreach(var exec in Executions)
                {
                    exec.Stop();
                }

                _Executions.Clear();
            }

            Script script = new Script();
            script.Open(path);
            OnScriptOpened?.Invoke(this, script);

            return script;
        }

        public int Login(string username, string password)
        {
            if (Enum.TryParse(password, out AuthType type))
            {
                //if (type > AuthType.Maintainer)
                //{
                    if (TestStandHelper.TS_AppMgr.GetEngine().CurrentUser is null)
                    {
                        var userobj = TestStandHelper.TS_AppMgr.GetEngine().GetUser("administrator");
                        //TS_AppMgr.GetEngine().DisplayLoginDialog("Login", "administrator", "", true, out User userobj);
                        TestStandHelper.TS_AppMgr.GetEngine().CurrentUser = userobj;
                        TestStandHelper.TS_AppMgr.GetEngine().CurrentUser.FullName = username;
                    }
                //}
            }

            return 1;
        }

        public IScript NewScript(TestCore.Configuration.GlobalConfiguration config = null)
        {
            throw new NotImplementedException();
        }

        public int ResumeAll()
        {
            throw new NotImplementedException();
        }

        public int SetModulePath(string modulepath)
        {
            throw new NotImplementedException();
        }

        public int StartCalibration()
        {
            MessageBox.Show("Not Support Yet");
            return 1;
        }

        public int StartEngine()
        {
            Info("Starting Engine");
            TestStandHelper.Engine.AutoLoginSystemUser = true;

            //TS_AppMgr.LoginOnStart = true;
            //TS_AppMgr.Start();
            if (Environment.Is64BitProcess != TestStandHelper.Engine.Is64Bit)
            {
                MessageBox.Show("Current App is x64, but Current Test Engine is not x64. the test might not works");
                Application.Exit();
            }

            TestStandHelper.Engine.StationOptions.AutoLoginSystemUser = true;

            Pre_BreakOnFirstStep = TestStandHelper.TS_AppMgr.BreakOnFirstStep;

            Pre_BreakOnError = TestStandHelper.Engine.BreakOnRTE;
            Pre_DisableResults = TestStandHelper.Engine.DisableResults;
            Pre_AlwaysGotoCleanupOnFailure = TestStandHelper.Engine.AlwaysGotoCleanupOnFailure;
            Pre_RTEOptions = TestStandHelper.Engine.RTEOption;
            Pre_BreakOnSequenceFailure = TestStandHelper.Engine.StationOptions.BreakOnSequenceFailure;
            Pre_BreakOnStepFailure = TestStandHelper.Engine.StationOptions.BreakOnStepFailure;
            //Pre_BreakpointsEnabled = Engine.StationOptions.BreakpointsEnabled;
            Pre_UseStationModel = TestStandHelper.Engine.StationOptions.UseStationModel;
            Pre_TracingEnable = TestStandHelper.Engine.TracingEnabled;

            TestStandHelper.TS_AppMgr.BreakOnFirstStep = false;
            TestStandHelper.Engine.BreakOnRTE = false;
            TestStandHelper.Engine.StationOptions.BreakOnStepFailure = false;
            TestStandHelper.Engine.StationOptions.BreakOnSequenceFailure = false;
            //Engine.StationOptions.BreakpointsEnabled = false;
            TestStandHelper.Engine.TracingEnabled = true;

            TestStandHelper.Engine.StationOptions.AlwaysGotoCleanupOnFailure = GlobalConfiguration.Default.General.AutoCleanupWhenFailure;
            TestStandHelper.Engine.StationOptions.DisableResults = false;
            TestStandHelper.Engine.StationOptions.RTEOption = NationalInstruments.TestStand.Interop.API.RTEOptions.RTEOption_Continue;
            TestStandHelper.Engine.StationOptions.UseStationModel = true;

            if (TestStandHelper.Engine.StationOptions.SeqFileVersionAutoIncrementOpt == FileVersionAutoIncrement.FileVersionInc_None)
            {
                TestStandHelper.Engine.StationOptions.SeqFileVersionAutoIncrementOpt = FileVersionAutoIncrement.FileVersionInc_Build;
            }

            TestStandHelper.TS_ZeroTime = TestStandHelper.TS_ZeroTime.AddSeconds(TestStandHelper.Engine.SecondsAtStartIn1970UniversalCoordinatedTime + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalSeconds);

            IsStarted = true;

            TestStandHelper.TS_AppMgr.StartExecution += TS_AppMgr_StartExecution;
            TestStandHelper.TS_AppMgr.EndExecution += TS_AppMgr_EndExecution;
            TestStandHelper.TS_AppMgr.AfterUIMessageEvent += TS_AppMgr_AfterUIMessageEvent;
            
            Version = TestStandHelper.TS_AppMgr.GetEngine().VersionString;
            OnEngineStarted?.Invoke(this, null);
            Info("Engine Started");
            return 1;
        }

        private void TS_AppMgr_AfterUIMessageEvent(object sender, _ApplicationMgrEvents_AfterUIMessageEventEvent e)
        {
            string tempstr;

            Execution exec = Executions.FirstOrDefault();
            try
            {
                switch (e.uiMsg.Event)
                {
                    case UIMessageCodes.UIMsg_ReportChanged:
                        if (e.uiMsg.ActiveXData is Report report)
                        {
                            if (exec.IsOriginalModel)
                            { }
                            else if (string.IsNullOrEmpty(report.Location))
                            { }
                            else
                            {
                                var filename = Path.GetFileName(report.Location);
                                foreach (var queue in exec.SlotReportQueues)
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
                                                    //var reportname = TF_Utility.TsReportNameTransformer(report.Location, rs);  // For support SN contains "_"
                                                    var reportname = rs.GenerateReportName("xml"); // for support a certain name before commit result into MES  
                                                    dest = Path.Combine(Path.GetDirectoryName(report.Location), reportname);

                                                    File.Move(report.Location, dest);
                                                }
                                                catch (InvalidOperationException ioe)
                                                {
                                                    Warn(ioe);
                                                }

                                                rs.RawFile = dest;
                                                OnReportGenerated?.Invoke(this, new Tuple<TF_Result, string>(rs, dest));
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
                            exec.Results[(int)e.uiMsg.NumericData].Status = TF_TestStatus.TEST_INIT;

                            if (exec.IsOriginalModel) { }
                            else if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            {
                                try
                                {
                                    if (exec.IsSequentialModel)
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
                                        additional.NewSubProperty("Version", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Time", PropertyValueTypes.PropValType_String, false, "", 0);

                                        additional.NewSubProperty("Attach", PropertyValueTypes.PropValType_Container, false, "", 0);
                                        var attach = additional.GetPropertyObject("Attach", 0);
                                        foreach (var item in exec.Template.AttachProperties)
                                        {
                                            attach.NewSubProperty(item.Key, PropertyValueTypes.PropValType_String, false, "", 0);
                                        }

                                        additional.NewSubProperty("NAS_ExtFile", PropertyValueTypes.PropValType_String, false, "", 0);

                                        //if(exec.Script.SystemConfig?.General?.ReferencePeriod > 0)
                                        //{
                                        //    additional.NewSubProperty("IsRef", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                        //}

                                        //if (exec.Script.SystemConfig?.General?.ReferencePeriod > 0)
                                        //{
                                        //    additional.NewSubProperty("IsVer", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                        //}

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
                                        tempsc.Caller.Locals.SetFlags("UUT.AdditionalData.Version", 0, 0x2000);

                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.GuiVersion", 0, Application.ProductVersion);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.SpecVersion", 0, exec.Template.Specification.Version);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Customer", 0, exec.Template.StationConfig.CustomerName);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Product", 0, exec.Template.StationConfig.ProductName);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Station", 0, exec.Template.StationConfig.StationName);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Version", 0, exec.Template.TestSoftwareVersion);
                                    }
                                    else if (exec.IsParallelModel || exec.IsBatchModel)
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
                                        additional.NewSubProperty("Version", PropertyValueTypes.PropValType_String, false, "", 0);
                                        additional.NewSubProperty("Time", PropertyValueTypes.PropValType_String, false, "", 0);

                                        additional.NewSubProperty("Attach", PropertyValueTypes.PropValType_Container, false, "", 0);
                                        var attach = additional.GetPropertyObject("Attach", 0);
                                        foreach (var item in exec.Template.AttachProperties)
                                        {
                                            attach.NewSubProperty(item.Key, PropertyValueTypes.PropValType_String, false, "", 0);
                                        }
                                        additional.NewSubProperty("NAS_ExtFile", PropertyValueTypes.PropValType_String, false, "", 0);

                                        //if (exec.Script.SystemConfig?.General?.ReferencePeriod > 0)
                                        //{
                                        //    additional.NewSubProperty("IsRef", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                        //}

                                        //if (exec.Script.SystemConfig?.General?.ReferencePeriod > 0)
                                        //{
                                        //    additional.NewSubProperty("IsVer", PropertyValueTypes.PropValType_Boolean, false, "", 0);
                                        //}

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
                                        tempsc.Caller.Parameters.SetFlags("TestSocket.UUT.AdditionalData.Version", 0, 0x2000);

                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.GuiVersion", 0, Application.ProductVersion);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.SpecVersion", 0, exec.Template.Specification.Version);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.Customer", 0, exec.Template.StationConfig.CustomerName);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.Product", 0, exec.Template.StationConfig.ProductName);
                                        tempsc.Caller.Parameters.SetValString("TestSocket.UUT.AdditionalData.Station", 0, exec.Template.StationConfig.StationName);
                                        tempsc.Caller.Locals.SetValString("UUT.AdditionalData.Version", 0, exec.Template.TestSoftwareVersion);
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
                            int slot = (int)e.uiMsg.NumericData;
                            IReadOnlyList<TF_Result> rss = null;
                            if (exec.ExecutionMode == ExecutionMode.Reference)
                            {
                                rss = exec.ResultsRef;
                            }
                            else if (exec.ExecutionMode == ExecutionMode.Verification)
                            {
                                rss = exec.ResultsVer;
                            }
                            else
                            {
                                rss = exec.Results;
                            }

                            foreach (var rs in rss)
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
                            TF_Result result = null;
                            if (exec.ExecutionMode == ExecutionMode.Reference)
                            {
                                result = exec.ResultsRef[(int)e.uiMsg.NumericData];
                            }
                            else if (exec.ExecutionMode == ExecutionMode.Verification)
                            {
                                result = exec.Results[(int)e.uiMsg.NumericData];
                            }
                            else
                            {
                                result = exec.Results[(int)e.uiMsg.NumericData];
                            }
                            result.SerialNumber = null;

                            if (result.Status == TF_TestStatus.TEST_INIT)
                            {
                                result.Status = TF_TestStatus.IDLE;
                            }
                            else if (result.Status == TF_TestStatus.TESTING)  // Handing the test finished
                            {
                                result.Status = TF_TestStatus.WAIT_DUT;
                            }
                            else
                            {
                                exec.TrigPostUUTed(result);
                            }

                            if (exec.IsOriginalModel)
                            { }
                            else if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            {
                                if (exec.IsSequentialModel || exec.IsParallelModel)
                                {
                                    tempsc.Locals.SetValString("UUT.AdditionalData.SFCs_ExtValue", 0, "");
                                    tempsc.Locals.SetValString("UUT.AdditionalData.SFCs_BarcodePart", 0, "");

                                    if(exec.Script.SystemConfig.General.CustomizeInputSn)
                                    {
                                        tempsc.Locals.SetValString("UUT.SerialNumber", 0, "");
                                        tempsc.Locals.SetValString("UUT.PartNumber", 0, "");

                                        if(tempsc.SequenceErrorOccurred)
                                        {
                                            tempsc.SequenceErrorOccurred = false;
                                            tempsc.SequenceErrorCode = 0;
                                            tempsc.SequenceErrorMessage = string.Empty;
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    // After UIMsg_ModelState_Identified.
                    case UIMessageCodes.UIMsg_ModelState_BeginTesting:
                        if (e.uiMsg.NumericData >= 0)
                        {
                            TF_Result result = null;
                            if (exec.ExecutionMode == ExecutionMode.Reference)
                            {
                                result = exec.ResultsRef[(int)e.uiMsg.NumericData];
                            }
                            else if (exec.ExecutionMode == ExecutionMode.Verification)
                            {
                                result = exec.ResultsVer[(int)e.uiMsg.NumericData];
                            }
                            else
                            {
                                result = exec.Results[(int)e.uiMsg.NumericData];
                            }

                            if (exec.Script.SystemConfig.General.CustomizeInputSn && result.ErrorMessage.Code == -23) { }
                            else
                            {
                                result.Begin();
                                if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                                {
                                    tempsc.Caller.SequenceErrorMessage = String.Empty;
                                }
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
                            TF_Result rs = null;
                            if (exec.ExecutionMode == ExecutionMode.Reference)
                            {
                                rs = exec.ResultsRef[slot];
                            }
                            else if (exec.ExecutionMode == ExecutionMode.Verification)
                            {
                                rs = exec.ResultsVer[slot];
                            }
                            else
                            {
                                rs = exec.Results[slot];
                            }

                            if (exec.IsOriginalModel)
                            {
                                rs.SerialNumber = e.uiMsg.StringData;
                            }
                            else if (exec.Script.SystemConfig.General.CustomizeInputSn)
                            {
                                rs.SerialNumber = e.uiMsg.StringData;
                                rs.ErrorMessage = default(ErrorMsg);
                                try
                                {
                                    exec.CustomizeInputSn(slot);
                                }
                                catch (Exception ex)
                                {
                                    Warn($"CustomizeInputSn Start Test Err: {ex}");

                                    if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                                    {
                                        tempsc.Caller.NextStepIndex = tempsc.Caller.Sequence.GetStepIndex("Model Plugins - Post UUT", StepGroups.StepGroup_Main);
                                        tempsc.Caller.SequenceErrorMessage = ex.Message;
                                        tempsc.Caller.SequenceErrorCode = -23;
                                        tempsc.Caller.SequenceErrorOccurred = true;
                                    }
                                }
                            }
                            else
                            {
                                exec.TrigPreUUTed(rs);
                                if (exec.IsSequentialModel)
                                {
                                    exec.SlotBlockQueues[slot] = new NamedPipeQueueServer($"{exec.PipeKey}_-1");
                                    exec.SlotBlockQueues[slot].Initialize();
                                }
                                else if (exec.IsParallelModel)
                                {
                                    exec.SlotBlockQueues[slot] = new NamedPipeQueueServer($"{exec.PipeKey}_{slot}");
                                    exec.SlotBlockQueues[slot].Initialize();
                                }
                            }
                        }
                        else // For Batch
                        {
                            if (exec.IsOriginalModel || exec.Script.SystemConfig.General.CustomizeInputSn) { }
                            else
                            {
                                exec.SlotBlockQueues[0] = new NamedPipeQueueServer($"{exec.PipeKey}_Batch");
                                exec.SlotBlockQueues[0].Initialize();
                            }
                        }

                        break;

                    case UIMessageCodes.UIMsg_Trace:
                        //e.uiMsg.ActiveXData is null in Trace
                        break;

                    // After MainSeq Finished, Before PostUUT
                    //case UIMessageCodes.UIMsg_ModelState_TestingComplete:
                    //break;

                    // New Customer Event, For Fetch Test Data, include REF/VER
                    case UIMessageCodes.UIMsg_UserMessageBase + 10:
                        if (e.uiMsg.NumericData >= 0)
                        {
                            //TestStandUserEvent?.Invoke(this, new Tuple<int, double, string, object>((int)e.uiMsg.Event, e.uiMsg.NumericData, e.uiMsg.StringData, e.uiMsg.ActiveXData));

                            if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                            {
                                TF_Result result = null;
                                if (exec.ExecutionMode == ExecutionMode.Reference)
                                {
                                    result = exec.ResultsRef[(int)e.uiMsg.NumericData];
                                }
                                else if (exec.ExecutionMode == ExecutionMode.Verification)
                                {
                                    result = exec.ResultsVer[(int)e.uiMsg.NumericData];
                                }
                                else
                                {
                                    result = exec.Results[(int)e.uiMsg.NumericData];
                                }

                                if (tempsc.Caller.Sequence.GetStepByName("MainSequence Callback", StepGroups.StepGroup_Main).LastStepResult is PropertyObject rsseqpo)
                                {
                                    var rslist_mainseq = rsseqpo.GetValVariant("TS.SequenceCall.ResultList", 0) as IEnumerable<object>;

                                    result.StartTime = TestStandHelper.TS_ZeroTime.AddSeconds(rsseqpo.GetValNumber("TS.StartTime", 0));
                                    result.EndTime = result.StartTime.AddSeconds(rsseqpo.GetValNumber("TS.TotalTime", 0));

                                    //Parallel.ForEach(rslist_mainseq, (object rs) =>
                                    //{
                                    //    if (rs is PropertyObject po)
                                    //    {
                                    //        try
                                    //        {
                                    //            TestStandHelper.StepRecordToResult(po, result.StepDatas);
                                    //        }
                                    //        catch (Exception ex)
                                    //        {
                                    //            Warn(ex);
                                    //        }
                                    //    }
                                    //});

                                    foreach(var rs in rslist_mainseq)   // the loop will be as a same level item in data structure, Parallel will make use failed item as result data 
                                    {
                                        if (rs is PropertyObject po)
                                        {
                                            try
                                            {
                                                TestStandHelper.StepRecordToResult(po, result.StepDatas);
                                            }
                                            catch (Exception ex)
                                            {
                                                Warn(ex);
                                            }
                                        }
                                    }

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

                                    var extra_defectcode = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.ExtraDefectCode", 0);
                                    if (rs_sts == TF_TestStatus.FAILED)  // Refresh Defect if set Extra Defect
                                    {
                                        if (!string.IsNullOrEmpty(extra_defectcode))
                                        {
                                            var extra_defectname = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.ExtraDefectName", 0);

                                            result.Defect.Clear();
                                            result.Defect.Add(new Defect(extra_defectcode, extra_defectname, null));
                                        }
                                        else
                                        {
                                            var defect = result.Defect.FirstOrDefault();
                                            tempsc.Caller.Locals.SetValString("UUT.AdditionalData.ExtraDefectCode", 0, defect.Code);
                                            tempsc.Caller.Locals.SetValString("UUT.AdditionalData.ExtraDefectName", 0, defect.Desc);
                                        }
                                    }

                                    for (int i = 0; i < result.AttachProperties.Count; i++)
                                    {
                                        var key = result.AttachProperties.Keys.ElementAt(i);
                                        try
                                        {
                                            result.AttachProperties[key] = tempsc.Caller.Locals.GetValString($"UUT.AdditionalData.Attach.{key}", 0);
                                        }
                                        catch (System.Runtime.InteropServices.COMException)
                                        {
                                            // there might be temporary Attach Properties such as Tag has not inited into execution
                                        }
                                    }

                                    if ((rs_sts == TF_TestStatus.PASSED || rs_sts == TF_TestStatus.FAILED) && !exec.IsOriginalModel)
                                    {
                                        result.SpecialData = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.SFCs_BarcodePart", 0);
                                        result.ExtValues = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.SFCs_ExtValue", 0);
                                        result.ExtColumns = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.SFCs_ExtColumn", 0);
                                    }

                                    if (result.SerialNumber?.Length > 2)
                                    {
                                        var temprs = (TF_Result)result.Clone();
                                        temprs.Status = rs_sts;
                                        if (!result.GeneralConfig.DisableRemoteReport)
                                        {
                                            temprs.AdditionalFiles = result.AdditionalFiles = tempsc.Caller.Locals.GetValString("UUT.AdditionalData.NAS_ExtFile", 0).Split(';');
                                        }

                                        exec.SlotReportQueues[(int)e.uiMsg.NumericData].Enqueue(temprs);
                                    }

                                    result.End(rs_sts);

                                    if (rs_sts == TF_TestStatus.FAILED)  // Refresh Defect if set Extra Defect
                                    {
                                        if (string.IsNullOrEmpty(extra_defectcode))
                                        { 
                                            var defect = result.Defect.FirstOrDefault();
                                            tempsc.Caller.Locals.SetValString("UUT.AdditionalData.ExtraDefectCode", 0, defect.Code);
                                            tempsc.Caller.Locals.SetValString("UUT.AdditionalData.ExtraDefectName", 0, defect.Desc);
                                        }
                                    }

                                    //lock (writtingTymReportLock)
                                    //{
                                    //    result.ExportTestDataCSV();
                                    //    result.ExportTestTimeCsv();
                                    //}
                                }
                                else
                                {
                                    Warn("No Test data found");
                                }
                            }
                        }
                        break;

                    
                    case UIMessageCodes.UIMsg_EndExecution:
                        //if (e.uiMsg.NumericData >= 0)
                        //{
                        //    if (e.uiMsg.ActiveXData is SequenceContext tempsc)
                        //    {

                        //    }
                        //}
                        break;

                    case UIMessageCodes.UIMsg_RuntimeError:
                        break;

                    case UIMessageCodes.UIMsg_ModelState_PostProcessingComplete:
                        //if (e.uiMsg.NumericData >= 0)
                        //{
                        //    Executions.FirstOrDefault().Results[(int)e.uiMsg.NumericData].End(TF_TestStatus.TERMINATED);
                        //}
                        break;

                    case UIMessageCodes.UIMsg_EndFileExecution:
                        if (e.uiMsg.ActiveXData is SequenceFile seqfile)
                        {
                            if(seqfile == exec.Script._SequenceFile)
                            {
                                for (int i = 0; i < exec.SocketCount; i++)
                                {
                                    var occur = exec.SlotExecutions[i]?.ErrorObject?.GetValBoolean("Occurred", 0);
                                    
                                    if(occur == true & exec.Results[i].Status == TF_TestStatus.TEST_INIT)
                                    {
                                        var errormsg = exec.SlotExecutions[0].ErrorObject.GetValString("Msg", 0);
                                        var code = exec.SlotExecutions[0].ErrorObject.GetValNumber("Code", 0);

                                        exec.Results[(int)e.uiMsg.NumericData].ErrorMessage = new ErrorMsg() { Occurred = true, Code = (int)code, Info = $"Execution Abort by Error, {errormsg}" };
                                        exec.Results[(int)e.uiMsg.NumericData].End(TF_TestStatus.ABORT);
                                    }
                                }
                                
                            }
                            //if (e.uiMsg.NumericData >= 0)
                            //{
                            //    if (exec.Results[(int)e.uiMsg.NumericData].Status == TF_TestStatus.TEST_INIT)
                            //    {
                            //        exec.Results[(int)e.uiMsg.NumericData].ErrorMessage = new ErrorMsg() { Info = seq.SequenceErrorMessage, Occurred = true };
                            //        exec.Results[(int)e.uiMsg.NumericData].End(TF_ItemStatus.Abort);
                            //    }
                            //}
                        }
                        break;

                    default:
                        // offset. 100, Update Config. 200. Call APx, 
                        //
                        if (e.uiMsg.Event >= UIMessageCodes.UIMsg_UserMessageBase)   
                        {
                            TestStandUserEvent?.Invoke(this, new Tuple<int, double, string, object>((int)e.uiMsg.Event, e.uiMsg.NumericData, e.uiMsg.StringData, e.uiMsg.ActiveXData));
                        }

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
        public event EventHandler<Tuple<int, double, string, object>> TestStandUserEvent;
        private void TS_AppMgr_EndExecution(object sender, _ApplicationMgrEvents_EndExecutionEvent e)
        {
            
        }

        private int existExecutionCount = 0;
        private void TS_AppMgr_StartExecution(object sender, _ApplicationMgrEvents_StartExecutionEvent e)
        {
            SequenceContext sequence = e.thrd.GetSequenceContext(0, out int frameid);

            Info($"{sequence.CallStackName}, {e.exec.DisplayName}");
            //var exec = Executions.FirstOrDefault(x => ((Script)x.Script)._SequenceFile == sequence.SequenceFile);
            ////The sequence is probably from Model Sequence;
            var exec = Executions.LastOrDefault();
            try
            {
                string modeltype = string.Empty;
                // Only Main Execution contains locals.metadata
                try
                {
                    modeltype = sequence.Locals.GetValString("ModelData.ModelType", 0);
                    if (modeltype == "Sequential")
                    {
                        exec.IsSequentialModel = true;
                        exec.IsBatchModel = false;
                        exec.IsParallelModel = false;
                        exec.SocketCount = 1;

                        exec.ActionOnInitialized();
                        exec.SlotSequenceContexts[0] = sequence;
                        exec.SlotExecutions = new NationalInstruments.TestStand.Interop.API.Execution[1] { e.exec };

                        OnExecutionCreated?.Invoke(this, exec);  // for the the result template will not determinated when Ts Execution Created, move it here
                        OnExecutionStarted?.Invoke(this, exec);  // this will trig UI update
                        exec.TrigExecutionStarted();    // this will trig the 3rd engine update data
                    }
                    else if (modeltype == "Batch")
                    {
                        exec.IsSequentialModel = false;
                        exec.IsBatchModel = true;
                        exec.IsParallelModel = false;
                        exec.HostExecution = e.exec;
                        exec.HostSequenceContext = sequence;
                    }
                    else if (modeltype == "Parallel")
                    {
                        exec.IsSequentialModel = false;
                        exec.IsBatchModel = false;
                        exec.IsParallelModel = true;
                        exec.HostExecution = e.exec;
                        exec.HostSequenceContext = sequence;
                    }

                    existExecutionCount = TestStandHelper.TS_AppMgr.Executions.Count;
                }
                //catch(CalibrationDataExpiredException cdeex)
                //{
                //    MessageBox.Show($"Calibration Data Expired. {cdeex.Message}");
                //}
                catch(Exception)   // init other thread
                {
                    int slotIndex = -1;
                    try
                    {
                        slotIndex = (int)sequence.Parameters.GetValNumber("TestSocket.Index", 0);
                    }
                    catch
                    {
                        Warn($"Get TestSocket.Index Failed");
                    }

                    if (exec.IsBatchModel)
                    {
                        if (slotIndex == 0)
                        {
                            var SlotCount = (int)sequence.Parameters.GetValNumber("ModelData.ModelOptions.NumTestSockets", 0);
                            exec.SocketCount = SlotCount;
                            Info($"ModelOptions.NumTestSockets {exec.SocketCount}");

                            exec.ActionOnInitialized();
                            exec.SlotExecutions = new NationalInstruments.TestStand.Interop.API.Execution[SlotCount];
                            
                            OnExecutionCreated?.Invoke(this, exec);
                            OnExecutionStarted?.Invoke(this, exec);
                            exec.TrigExecutionStarted();
                        }

                        var idx = TestStandHelper.TS_AppMgr.Executions.Count - existExecutionCount - 1;
                        exec.SlotSequenceContexts[idx] = sequence;
                        exec.SlotExecutions[idx] = e.exec;
                    }
                    else if (exec.IsParallelModel)
                    {
                        if (slotIndex == 0)
                        {
                            var SlotCount = (int)sequence.Parameters.GetValNumber("ModelData.ModelOptions.NumTestSockets", 0);
                            Info($"ModelOptions.NumTestSockets {SlotCount}");
                            exec.SocketCount = SlotCount;

                            exec.ActionOnInitialized();
                            exec.SlotExecutions = new NationalInstruments.TestStand.Interop.API.Execution[SlotCount];
                            
                            OnExecutionCreated?.Invoke(this, exec);
                            OnExecutionStarted?.Invoke(this, exec);
                            exec.TrigExecutionStarted();
                        }

                        var idx = TestStandHelper.TS_AppMgr.Executions.Count - existExecutionCount - 1;
                        exec.SlotSequenceContexts[idx] = sequence;
                        exec.SlotExecutions[idx] = e.exec;
                    }
                }
            }
            catch (Exception ex)
            {
                Error($"Start Execution Error: {ex}");
            }
        }

        public IExecution CreateExecution(IScript script, string sequencename = null)
        {
            if (script is Script tsscript)
            {
                try
                {
                    var seq = script.Sequences?.FirstOrDefault(x => x.Name == sequencename);

                    if (sequencename == null)
                    {
                        //seq = script.Sequences.;
                    }
                }
                catch
                {
                    Error($"Activate seq {sequencename} failed");
                }

                Execution exec = new Execution(tsscript);
                ((List<Execution>)Executions).Add(exec);

                //OnExecutionCreated?.Invoke(this, exec);
                exec.Start();  // for the the result template will not determinated when Ts Execution Created, need start here

                return exec;
            }
            else
            {
                throw new InvalidOperationException($"Engine {Name} does not support {script.FilePath} in type {script.GetType()}");
            }
        }

        public IExecution StartExecution(IScript script, string sequencename = null)
        {
            if (script is Script tsscript)
            {
                try
                {
                    var seq = script.Sequences?.FirstOrDefault(x => x.Name == sequencename);

                    if (sequencename == null)
                    {
                        //seq = script.Sequences.;
                    }
                }
                catch
                {
                    Error($"Activate seq {sequencename} failed");
                }

                Execution exec = new Execution(tsscript);
                ((List<Execution>)Executions).Add(exec);

                //OnExecutionCreated?.Invoke(this, exec);

                exec.Start();

                return exec;
            }
            else
            {
                throw new InvalidOperationException($"Engine {Name} does not support {script.FilePath} in type {script.GetType()}");
            }
        }

        internal const string MainSequenceName = "MainSequence";
        internal const string ReferenceSequenceName = "REF";
        internal const string VerificationSequenceName = "VER";
        public IExecution StartReferenceExecution(IScript script)
        {
            if (script is Script tsscript)
            {
                if (script.Sequences.FirstOrDefault(x => x.Name == ReferenceSequenceName) is Sequence seq)
                {
                    var exec = Executions.FirstOrDefault() as Execution;

                    if (exec is null) throw new InvalidOperationException("Please Wait Execution Start");

                    //exec.SetReferenceMode(true);

                    Execution rtn = new Execution(tsscript);
                    //rtn.Name = ReferenceSequenceName;
                    ((List<Execution>)Executions).Add(rtn);
                    return rtn;
                }
                else
                {
                    throw new InvalidOperationException("No REF Sequence Detected");
                }
            }
            else
            {
                throw new InvalidDataException("Script is not a legal TestStand Sequence File");
            }
        }

        public IExecution StartVerificationExecution(IScript script)
        {
            if (script is Script tsscript)
            {
                if (script.Sequences.FirstOrDefault(x => x.Name == VerificationSequenceName) is Sequence seq)
                {
                    foreach (var exec in Executions)
                    {
                        if (exec.Script != script) continue;
                        exec.Stop();

                        if (exec.IsOriginalModel || exec.Script.SystemConfig.General.CustomizeInputSn)
                        {
                            MessageBox.Show("There might be conflict on hardware, please QUIT the current execution at first", "Warning");
                        }
                    }

                    foreach (var exec in Executions)
                    {
                        if (exec.Script != script) continue;
                        exec.TS_Execution.GetStates(out ExecutionRunStates runState, out ExecutionTerminationStates termState);

                        if (runState != ExecutionRunStates.ExecRunState_Stopped)
                        {

                        }
                    }

                    Execution rtn = new Execution(tsscript);
                    //rtn.Name = ReferenceSequenceName;
                    ((List<Execution>)Executions).Add(rtn);
                    return rtn;
                }
                else
                {
                    throw new InvalidOperationException("No VER Sequence Detected");
                }
            }
            else
            {
                throw new InvalidDataException("Script is not a legal TestStand Sequence File");
            }
        }

        public int StopEngine()
        {
            if (!IsStarted) return 1;

            DateTime t0 = DateTime.Now;

            var incomplete_exec_cnt = 0;
            do
            {
                incomplete_exec_cnt = TestStandHelper.TS_AppMgr.Executions.NumIncomplete;
                if (incomplete_exec_cnt <= 0)
                {
                    break;
                }
                else
                {
                    if (MessageBox.Show($"There are {incomplete_exec_cnt} incomplete, Click Yes to check if it is complete, or No to terminate all and quit", "Warning", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        for (int i = 0; i < TestStandHelper.TS_AppMgr.Executions.Count; i++)
                        {
                            TestStandHelper.TS_AppMgr.Executions[0].Abort();
                        }
                        
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }
            }
            while (incomplete_exec_cnt > 0);

            TestStandHelper.TS_AppMgr.GetCommand(NationalInstruments.TestStand.Interop.UI.Support.CommandKinds.CommandKind_CloseCompletedExecutions, 0);

            TestStandHelper.TS_AppMgr.CloseAllExecutions();
            TestStandHelper.TS_AppMgr.Shutdown();
            return 1; 
        }

        public int StopExecution(IExecution exec)
        {
            return exec.Stop();
        }

        public int TerminateAll()
        {
            TestStandHelper.Engine.TerminateAll();
            return 1;
        }

        public int ApplyCalibration()
        {
            return 1;
        }

        public void ShowEngineSettingDialog()
        {
            throw new NotImplementedException();
        }
    }
}
