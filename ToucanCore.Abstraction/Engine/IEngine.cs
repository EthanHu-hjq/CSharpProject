using System;
using System.Collections.Generic;
using System.Text;
using TestCore.Data;

namespace ToucanCore.Abstraction.Engine
{
    /// <summary>
    /// Interface of Test Engine
    /// </summary>
    public interface IEngine : IDisposable
    {
        /// <summary>
        /// Engine Name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Engine Version
        /// </summary>
        string Version { get; }

        /// <summary>
        /// User who login in the Engine
        /// </summary>
        string UserName { get; }

        /// <summary>
        /// Station Id. or physical station name
        /// </summary>
        string StationId { get; set; }

        /// <summary>
        /// XX File|*.XX
        /// </summary>
        string FileFilter { get; }
        #region event
        /// <summary>
        /// 
        /// </summary>
        event EventHandler OnEngineInitialized;

        /// <summary>
        /// Event On Engine Started
        /// </summary>
        event EventHandler OnEngineStarted;

        /// <summary>
        /// Event On Engine Stopped
        /// </summary>
        event EventHandler OnEngineStopped;

        event EventHandler<IScript> OnScriptOpened;

        /// <summary>
        /// Event On Execution Created.
        /// For this event will be trigged on APP site to loading appendix file and data
        /// </summary>
        event EventHandler<IExecution> OnExecutionCreated;

        /// <summary>
        /// Event On Execution Started
        /// For this event will be trigged after Execution Data structured, but Execution.ExecutionStarted is when execution started already
        /// </summary>
        event EventHandler<IExecution> OnExecutionStarted;

        /// <summary>
        /// Event On Execution Stopped
        /// </summary>
        event EventHandler<IExecution> OnExecutionStopped;

        /// <summary>
        /// When Report Generated
        /// </summary>
        event EventHandler<Tuple<TF_Result, string>> OnReportGenerated;
        #endregion

        #region Global Execution Property
        /// <summary>
        /// If Engine initialized Ok
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// If Engine is Started
        /// </summary>
        bool IsStarted { get; }

        ///// <summary>
        ///// If there is Execution Running
        ///// </summary>
        //bool IsRunning { get; }

        /// <summary>
        /// For internal test
        /// </summary>
        bool IsForVerification { get; set; }

        /// <summary>
        /// If Break on the first step
        /// </summary>
        bool BreakOnFirstStep { get; }

        /// <summary>
        /// If Break when Failue
        /// </summary>
        bool BreakOnFailure { get; }

        /// <summary>
        /// If goto Cleanup when Failure
        /// </summary>
        bool GotoCleanupOnFailure { get; }

        /// <summary>
        /// Disable to generate Results Report
        /// </summary>
        bool DisableResults { get; }

        /// <summary>
        /// the Action When Error happened. TODO
        /// </summary>
        int ActionOnError { get; }

        /// <summary>
        /// Variable for Engine
        /// </summary>
        IReadOnlyDictionary<string, object> Variables { get; }
        #endregion

        /// <summary>
        /// Login into Engine with UserName and Password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        int Login(string username, string password);

        /// <summary>
        /// Engine Initialize
        /// </summary>
        /// <returns></returns>
        int Initialize();

        /// <summary>
        /// Initialize the Engine in async
        /// </summary>
        /// <returns></returns>
        //Task<int> InitializeAsyn();

        /// <summary>
        /// Start the Engine
        /// </summary>
        /// <returns></returns>
        int StartEngine();

        /// <summary>
        /// Start the Engine in async
        /// </summary>
        /// <returns></returns>
        //Task<int> StartEngineAsyn();

        /// <summary>
        /// Stop the Engine
        /// </summary>
        /// <returns></returns>
        int StopEngine();

        /// <summary>
        /// Load Sequence File
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        IScript LoadScriptFile(string path);

        /// <summary>
        /// New Script, 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        IScript NewScript(TestCore.Configuration.GlobalConfiguration config = null);

        /// <summary>
        /// Format Script
        /// </summary>
        /// <returns></returns>
        int FormatScript();

        ///// <summary>
        ///// Apply the Specification to the template
        ///// </summary>
        ///// <param name="specpath"></param>
        ///// <returns></returns>
        //int ApplySpecification(string specpath);

        IExecution CreateExecution(IScript script, string sequencename = null);

        /// <summary>
        /// Start Execution with sequence name
        /// if seq is entrypoint, start with model, otherwise just run seq
        /// this will make Execution Started
        /// </summary>
        /// <param name="script"></param>
        /// <param name="sequencename"></param>
        /// <returns></returns>
        IExecution StartExecution(IScript script, string sequencename = null);

        IExecution StartReferenceExecution(IScript script);

        IExecution StartVerificationExecution(IScript script);

        /// <summary>
        /// Stop Execution, TODO: Sequence File. Move to Execution
        /// </summary>
        /// <returns></returns>
        int StopExecution(IExecution exec);

        /// <summary>
        /// Resume all Executions
        /// </summary>
        /// <returns></returns>
        int ResumeAll();

        /// <summary>
        /// Terminate all Executions
        /// </summary>
        /// <returns></returns>
        int TerminateAll();

        /// <summary>
        /// Abort All Executions. Try to kill all the thread of executions
        /// </summary>
        /// <returns></returns>
        int AbortAll();

        /// <summary>
        /// Test Model For execute sequence
        /// </summary>
        IModel Model { get; }

        /// <summary>
        /// Switch to Make Engine UI visible or not
        /// </summary>
        bool UiVisible { get; set; }

        /// <summary>
        /// If Engine UI in edit mode
        /// </summary>
        bool IsEditMode { get; set; }

        int SetModulePath(string modulepath);

        int GenerateReport(TF_Result rs, string basepath);

        /// <summary>
        /// Calibration is not an Execution, for it probably could be run without script
        /// </summary>
        /// <returns></returns>
        int StartCalibration();

        /// <summary>
        /// Calibration for hardware, which means this calib will share across project
        /// this should be executed before each execution, for that might need set cal data into context
        /// If there is't, return 1. otherwise tick a timer
        /// </summary>
        /// <returns></returns>
        int ApplyCalibration();

        /// <summary>
        /// Calibration Expired
        /// </summary>
        event EventHandler CalibrationExpired;

        /// <summary>
        /// Calibration Expiring Warning
        /// </summary>
        event EventHandler CalibrationExpiring;

        void ShowEngineSettingDialog();
    }

    public interface IEngine<T, V> : IEngine where T : IExecution<V> where V : IScript
    {
        /// <summary>
        /// Executions
        /// </summary>
        IReadOnlyCollection<T> Executions { get; }

        /// <summary>
        /// Scripts already openned
        /// </summary>
        IReadOnlyCollection<V> Scripts { get; }
    }
}
