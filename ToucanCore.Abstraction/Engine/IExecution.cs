using System;
using System.Collections.Generic;
using System.Text;
using TestCore.Data;

namespace ToucanCore.Abstraction.Engine
{
    /// <summary>
    /// Interface of Execution
    /// </summary>
    public interface IExecution : IDebugable, IDisposable
    {
        /// <summary>
        /// Test Engine
        /// </summary>
        IEngine Engine { get; }

        ///// <summary>
        ///// Model of Execution
        ///// </summary>
        IModel Model { get; }

        ///// <summary>
        ///// 
        ///// </summary>
        string Name { get; }

        /// <summary>
        /// Stack of Step, for tracing
        /// </summary>
        //Stack<IStep> StepStack { get; }

        /// <summary>
        /// the Active slot index of Execution
        /// </summary>
        int SlotIndex { get; }

        /// <summary>
        /// For internal test
        /// </summary>
        bool IsForVerification { get; set; }

        /// <summary>
        /// If Break on the first step
        /// </summary>
        bool BreakOnFirstStep { get; set; }

        /// <summary>
        /// If Break when Failue
        /// </summary>
        bool BreakOnFailure { get; set; }

        /// <summary>
        /// If goto Cleanup when Failure
        /// </summary>
        bool GotoCleanupOnFailure { get; set; }
        /// <summary>
        /// 
        /// </summary>
        bool DisableResults { get; set; }
        /// <summary>
        /// the Action When Error happened. TODO
        /// </summary>
        int ActionOnError { get; set; }

        /// <summary>
        /// Slot Count, for the Socket count might not be same as Config
        /// </summary>
        int SocketCount { get; }

        /// <summary>
        /// Result Template
        /// </summary>
        TF_Result Template { get; }

        //IReadOnlyCollection<SlotInfo> SlotCtrls { get; }

        /// <summary>
        /// Test Result for slot, might redirect to DUT or REF or VER
        /// </summary>
        IReadOnlyList<TF_Result> Results { get; }

        ///// <summary>
        ///// Test Result for Normal Test
        ///// </summary>
        //IReadOnlyList<TF_Result> ResultsDut { get; }

        ///// <summary>
        ///// Test Result for REF Test
        ///// </summary>
        //IReadOnlyList<TF_Result> ResultsRef { get; }

        ///// <summary>
        ///// Test Result for Ver Test
        ///// </summary>
        //IReadOnlyList<TF_Result> ResultsVer { get; }

        ExecutionMode ExecutionMode { get; }

        /// <summary>
        /// Execution Model Type
        /// </summary>
        ModelType ModelType { get; }

        /// <summary>
        /// Start the Execution
        /// </summary>
        /// <returns></returns>
        int Start();

        int StartNewTest(int slotIndex = 0);
        //int StartNewTest(string sn, int slotIndex = 0);

        /// <summary>
        /// End the Execution
        /// </summary>
        /// <returns></returns>
        int Stop();

        /// <summary>
        /// Terminate the Execution
        /// </summary>
        /// <returns></returns>
        int Terminate();

        /// <summary>
        /// Abort the Execution
        /// </summary>
        /// <returns></returns>
        int Abort();

        IScript GetScript();

        void ShowVariables(int slot);
        object GetVariable(string name);
        void SetVariable(string name, object value);

        int EnableSlot(int slotindex, bool status = true);

        string Workbase { get; }

        event EventHandler ExecutionStarted;
        event EventHandler ExecutionStopped;

        /// <summary>
        /// event after Instrument Initialized.
        /// </summary>
        event EventHandler<TF_Result> OnPreUUTLoop;

        /// <summary>
        /// event when do the preaction for Test 
        /// </summary>
        event EventHandler<TF_Result> OnPreUUTing;

        /// <summary>
        /// event after the preaction for test has been done
        /// </summary>
        event EventHandler<TF_Result> OnPreUUTed;

        /// <summary>
        /// Call when Uut Identified. the MES checkstation would be injected into it
        /// </summary>
        event EventHandler<TF_Result> OnUutIdentified;

        event EventHandler<TF_Result> OnUutPassed;
        event EventHandler<TF_Result> OnUutFailed;
        event EventHandler<TF_Result> OnError;
        /// <summary>
        /// Call when Test End
        /// </summary>
        event EventHandler<TF_Result> OnTestCompleted;

        event EventHandler<TF_Result> OnPostUUTing;
        event EventHandler<TF_Result> OnPostUUTed;
        event EventHandler<TF_Result> OnPostUUTLoop;
        ///// <summary>
        ///// whether the Execution could be start a new test. 
        ///// </summary>
        //int AllowStartTest { get; }

        int SwitchExecutionMode(ExecutionMode mode);
    }

    public interface IExecution<T> : IExecution where T : IScript
    {
        T Script { get; }
    }

    /// <summary>
    /// if the class be debugable
    /// </summary>
    public interface IDebugable
    {
        /// <summary>
        /// Break the context
        /// </summary>
        void Break();

        /// <summary>
        /// Resume the context
        /// </summary>
        void Resume();

        /// <summary>
        /// For Debug, Step In source code
        /// </summary>
        void StepIn();

        /// <summary>
        /// For debug, Step over the step
        /// </summary>
        void StepOver();

        /// <summary>
        /// For debug, Step out from source code
        /// </summary>
        void StepOut();
    }

    public enum ExecutionMode
    {
        Normal = 0,
        Reference = 1,
        Verification = 2,

        SinglePass = 10,
        Demo = 15,
    }
}
