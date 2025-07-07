using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Ctrls;
using TestCore.Data;
using TestCore;

namespace Toucan
{
    public abstract class TestEngine : TF_Base
    {
        public string Name { get; protected set; }
        public abstract string Version { get; }
        public abstract string UserName { get; set; }

        public abstract string FileFilter { get; }

        public event EventHandler OnAsyncInitializated;
        public event EventHandler OnAsyncEngineStarted;
        public event EventHandler OnExecutionInitialized;

        public bool IsInitialized { get; protected set; }
        public bool IsRunning { get; protected set; }
        public bool IsReadyToRun { get; protected set; }

        public virtual bool BreakOnFirstStep { get; set; }
        public virtual bool BreakOnFirstFailure { get; set; }
        public virtual bool AlwaysGotoCleanupOnFailure { get; set; }
        public virtual bool DisableResults { get; set; }
        public virtual int ActionOnError { get; set; }

        public virtual System.Windows.Forms.ToolStripItem[] MenuItems { get; protected set; }

        public abstract int SetUserName(string username);

        public bool StopEngineWhenAllExecutionCompleted { get; set; }

        public int SlotCount { get; protected set; }
        public TF_Result[] Results { get; protected set; }
        public SlotInfo[] SlotCtrls { get; protected set; }

        public TF_Result ResultTemplate { get; protected set; }

        public bool IsOriginalModel { get; set; }

        public abstract int Initialize();

        public bool CustomizeInputSn { get; protected set; }

        public bool IsForVerification { get; set; }

        public async Task<int> InitAsyn()
        {
            //var d = Task.Run(()=> { CheckEnvironment(); });
            var rs = await Task.Run((Func<int>)Initialize);

            OnAsyncInitializated(this, new EventArgs());

            return rs;
        }

        public abstract int StartEngine();

        public async Task<int> StartEngineAsyn()
        {
            int rs = -1;
            try
            {
                rs = await Task.Run((Func<int>)StartEngine);
            }
            catch (Exception ex)
            {
                Error(ex);
                throw ex;
            }
            finally
            {
                OnAsyncEngineStarted(this, new EventArgs());
            }
            return rs;
        }

        public string ScriptFilePath { get; set; }
        /// <summary>
        /// Load Sequence File
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract int LoadScriptFile(string path);
        public abstract int FormatScript(out string formatlog);
        public abstract int SaveScriptAs(string dest);
        public abstract ScriptAnalysisResult AnalyzeScript();
        public abstract int ApplySpecification(string specpath);

        public abstract int StartExecution();
        public abstract int StopExecution();

        public abstract int ResumeAll();
        public abstract int TerminateAll();
        public abstract int AbortAll();

        public abstract int StopEngine();

        public abstract int StartNewTest(string sn, int slot);
        public abstract int FinishTest(int slot);

        public abstract int SetModulePath(string path);
        public abstract int SetModulePath(RunMode mode, string path);
        public abstract string GetModulePath();

        public TestCore.Services.IReportService ReportService { get; set; }

        protected virtual void ActionOnExecutionInitialized()
        {
            OnExecutionInitialized?.Invoke(this, new EventArgs());
        }

        public const string MCAST_ADDR = "239.8.8.88";
        public const int MCAST_PORT = 5499;
        public const int MCAST_TTL = 9;

        //public static void EdpServiceBroadcast()
        //{
        //    throw new NotImplementedException();
        //    //Socket edp = new Socket(SocketType.Dgram, ProtocolType.Udp);
        //    //edp.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, MCAST_TTL);
        //    //System.Net.EndPoint multicast = new IPEndPoint(IPAddress.Parse(MCAST_ADDR), MCAST_PORT);

        //    //edp.SendTo(new byte[0], multicast);
        //}
    }
}
