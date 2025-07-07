using NationalInstruments.TestStand.Interop.API;
using NationalInstruments.TestStand.Interop.UI;
using NationalInstruments.TestStand.Interop.UI.Ax;
using NationalInstruments.TestStand.Interop.UI.Support;
using NationalInstruments.TestStand.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TestCore.Properties;

namespace TsEngine.UIs
{
    public partial class ExecutionUI : Form
    {
        public AxApplicationMgr TS_AppMgr = new AxApplicationMgr();
        public AxSequenceFileViewMgr AxSequenceFileMgr = new AxSequenceFileViewMgr();

        private AxExecutionViewMgr[] ExecutionViewMgrs; // just a axHost
        private AxSequenceView[] AxExecutionViews;  // True Execution UI

        //private AxButton[] AxBreaks;
        private AxButton[] AxBreakResumes;

        private AxSequenceView AxSequenceFileView;  // True File UI
        private AxListBox axSequencesList;
        private AxVariablesView axSequenceVariable;

        private AxVariablesView[] AxVariablesViews;
        private Execution execution;
        public ExecutionUI()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            FormClosing += ExecutionUI_FormClosing;
            
            //var resources = new System.ComponentModel.ComponentResourceManager(GetType());

            //Ts_AppMgr = new AxApplicationMgr();
            //AxSequenceFileMgr = new AxSequenceFileViewMgr();
            AxSequenceFileView = new AxSequenceView();
            axSequencesList = new AxListBox();
            axSequenceVariable = new AxVariablesView();

            TS_AppMgr.BeginInit();
            AxSequenceFileMgr.BeginInit();
            AxSequenceFileView.BeginInit();
            axSequencesList.BeginInit();
            axSequenceVariable.BeginInit();

            Controls.Add(TS_AppMgr);
            Controls.Add(AxSequenceFileMgr);
            splitContainer1.Panel1.Controls.Add(axSequencesList);
            splitContainer1.Panel2.Controls.Add(axSequenceVariable);
            splitContainer_File.Panel2.Controls.Add(AxSequenceFileView);

            TS_AppMgr.Enabled = true;
            AxSequenceFileView.Enabled = true;
            axSequencesList.Dock = DockStyle.Fill;
            axSequenceVariable.Dock = DockStyle.Fill;
            AxSequenceFileView.Dock = DockStyle.Fill;
            AxSequenceFileView.CreateContextMenu += new NationalInstruments.TestStand.Interop.UI.Ax._SequenceViewEvents_CreateContextMenuEventHandler(AxSequenceFileView_CreateContextMenu);
            AxSequenceFileMgr.Enabled = true;

            axSequenceVariable.EndInit();
            axSequencesList.EndInit();
            AxSequenceFileView.EndInit();
            AxSequenceFileMgr.EndInit();
            TS_AppMgr.EndInit();

            AxSequenceFileMgr.StepGroupMode = StepGroupModes.StepGroupMode_AllGroups;

            AxSequenceFileMgr.ConnectSequenceList(axSequencesList).SetColumnVisible(SeqListConnectionColumns.SeqListConnectionColumn_Comments, true);
            AxSequenceFileMgr.ConnectVariables(axSequenceVariable);
            AxSequenceFileMgr.ConnectSequenceView(AxSequenceFileView);

            TS_AppMgr.LoginOnStart = false;
            if (!TS_AppMgr.IsStarted)
            {
                TS_AppMgr.Start();
            }
        }

        bool IsForceClose = false;
        private void ExecutionUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!IsForceClose)
            {
                e.Cancel = true;
                Hide();
            }
        }

        public void ForceClose()
        {
            IsForceClose = true;
            Close();
        }

        public void InitialExecution(Execution exec, bool iseditable = true)
        {
            if (execution == exec) return;

            if (ExecutionViewMgrs != null)
            {
                foreach (var ev in ExecutionViewMgrs)
                {
                    ev.Dispose();
                }
            }

            if (AxExecutionViews != null)
            {
                foreach (var ev in AxExecutionViews)
                {
                    ev.Dispose();
                }
            }

            if (AxVariablesViews != null)
            {
                foreach (var ev in AxVariablesViews)
                {
                    ev.Dispose();
                }
            }

            execution = exec;

            ExecutionViewMgrs = new NationalInstruments.TestStand.Interop.UI.Ax.AxExecutionViewMgr[exec.SocketCount];
            AxExecutionViews = new AxSequenceView[exec.SocketCount];
            AxVariablesViews = new AxVariablesView[exec.SocketCount];
            //AxBreaks = new AxButton[exec.SocketCount];
            AxBreakResumes = new AxButton[exec.SocketCount];

            TabControl tabctrl = new TabControl();
            tabctrl.Dock = DockStyle.Fill;

            AxSequenceFileView.Enabled = iseditable;

            for (int i = 0; i < exec.SocketCount; i++)
            {
                TabPage tabpage = new TabPage();
                tabpage.Text = $"Slot {i}";

                SplitContainer split_exec = new SplitContainer();
                split_exec.Dock = DockStyle.Fill;
                split_exec.SplitterDistance = 640;

                ExecutionViewMgrs[i] = new NationalInstruments.TestStand.Interop.UI.Ax.AxExecutionViewMgr();
                AxExecutionViews[i] = new NationalInstruments.TestStand.Interop.UI.Ax.AxSequenceView();
                AxVariablesViews[i] = new AxVariablesView();
                //AxBreaks[i] = new AxButton();
                AxBreakResumes[i] = new AxButton();

                AxExecutionViews[i].BeginInit();
                AxVariablesViews[i].BeginInit();
                ExecutionViewMgrs[i].BeginInit();
                //AxBreaks[i].BeginInit();
                AxBreakResumes[i].BeginInit();

                AxExecutionViews[i].Dock = DockStyle.Fill;
                AxVariablesViews[i].Dock = DockStyle.Fill;
                AxVariablesViews[i].Enabled = false;
                //AxBreaks[i].Dock = DockStyle.Bottom;
                AxBreakResumes[i].Dock = DockStyle.Bottom;

                Controls.Add(ExecutionViewMgrs[i]);
                split_exec.Panel1.Controls.Add(AxExecutionViews[i]);
                //splitContainer_Exec.Panel2.Controls.Add(AxBreaks[i]);
                split_exec.Panel2.Controls.Add(AxBreakResumes[i]);
                split_exec.Panel2.Controls.Add(AxVariablesViews[i]);

                ExecutionViewMgrs[i].EndInit();
                AxVariablesViews[i].EndInit();
                AxExecutionViews[i].EndInit();
                //AxBreaks[i].EndInit();
                AxBreakResumes[i].EndInit();

                ExecutionViewMgrs[i].StepGroupMode = StepGroupModes.StepGroupMode_AllGroups;
                ExecutionViewMgrs[i].ConnectExecutionView(AxExecutionViews[i]);
                ExecutionViewMgrs[i].ConnectVariables(AxVariablesViews[i]);
                //ExecutionViewMgrs[i].ConnectCommand(AxBreaks[i], CommandKinds.CommandKind_Break, 0, 0);
                ExecutionViewMgrs[i].ConnectCommand(AxBreakResumes[i], CommandKinds.CommandKind_BreakResume, 0, 0);

                ExecutionViewMgrs[i].Execution = exec.SlotExecutions[i];
                //ExecutionViewMgrs[i].Trace += ExecutionUI_Trace;

                AxVariablesViews[i].Enabled = iseditable;
                AxBreakResumes[i].Enabled = iseditable;


                tabpage.Controls.Add(split_exec);
                tabctrl.TabPages.Add(tabpage);
            }

            AxSequenceFileMgr.SequenceFile = exec.Script._SequenceFile;

            tabPage1.Controls.Clear();
            tabPage1.Controls.Add(tabctrl);
        }

        private void AxSequenceFileView_CreateContextMenu(object sender, _SequenceViewEvents_CreateContextMenuEvent e)
        {
            Commands cmds = TS_AppMgr.NewCommands();
            int unused;

            // insert items for the specified command or command set in the context menu
            cmds.InsertKind(CommandKinds.CommandKind_DefaultSequenceListContextMenu_Set, AxSequenceFileMgr, -1, "", "", out unused);
            Menus.RemoveInvalidShortcutKeys(cmds);  // remove any shortcuts that .NET does not support
            cmds.InsertIntoWin32Menu(e.menuHandle, -1, true, true);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var sf = TS_AppMgr.GetEngine().GetSequenceFileEx(ofd.FileName);
                    AxSequenceFileMgr.SequenceFile = sf;
                }
            }
        }



        //private void ExecutionUI_Trace(object sender, _ExecutionViewMgrEvents_TraceEvent e)
        //{
        //    if (e.exec.DisplayName != null)
        //    { 
        //    }
        //}
    }
}
