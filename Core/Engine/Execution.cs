using Mes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestCore;
using TestCore.Data;
using static ToucanCore.HAL.StartTrigger_Fixture;
using ToucanCore.Driver;
using ToucanCore.HAL;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms.Design;
//using System.Windows.Forms;
using TestCore.Abstraction.Process;
using System.Timers;
using ToucanCore.Abstraction.HAL;
using ToucanCore.Abstraction.Engine;
using System.Globalization;
using TestCore.Configuration;

namespace ToucanCore.Engine
{
    // Add business logic of Execution for specified test engine
    public sealed class Execution : TF_Base, IExecution
    {
        public IExecution Exec { get; }
        public IMes Mes { get; private set; }
        public ToucanCore.Engine.Script Script { get; }

        public event EventHandler<DutMessage> DutSnUpdated;

        public int SocketCount => Exec.SocketCount;
        public IReadOnlyList<TF_Result> Results => Exec.Results;

        public event EventHandler ReferenceCompleted;
        public event EventHandler VerificationCompleted;

        public IEngine Engine => Exec.Engine;

        public IModel Model => Exec.Model;

        public string Name => Exec.Name;

        public int SlotIndex { get; set; }

        public bool IsForVerification { get => Exec.IsForVerification; set => Exec.IsForVerification = value; }
        public bool BreakOnFirstStep { get => Exec.BreakOnFirstStep; set => Exec.BreakOnFirstStep = value; }
        public bool BreakOnFailure { get => Exec.BreakOnFailure; set => Exec.BreakOnFailure = value; }
        public bool GotoCleanupOnFailure { get => Exec.GotoCleanupOnFailure; set => Exec.GotoCleanupOnFailure = value; }
        public bool DisableResults { get => Exec.DisableResults; set => Exec.DisableResults = value; }
        public int ActionOnError { get => Exec.ActionOnError; set => Exec.ActionOnError = value; }

        public TF_Result Template => Exec.Template;

        public ModelType ModelType => Exec.ModelType;

        public string Workbase => Exec.Workbase;

        /// <summary>
        /// For Multiple Slot
        /// this might not be same as the sn in Results, for it might be update by sn queue
        /// </summary>
        public string[] DutSnWaitForTest { get; }

        public ExecutionMode ExecutionMode => Exec.ExecutionMode;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="execution"></param>
        public Execution(IExecution execution, ToucanCore.Engine.Script script = null)
        {
            Exec = execution;
            Script = script;

            DutSnWaitForTest = new string[execution.SocketCount];

            if (Script.SFCsConfig?.EnableSfc == true)
            {
                Mes = MesManager.GetMesInstance(Script.StationConfig.Location, Script.StationConfig.Vendor);

                if (Mes is null)
                {
                    Warn($"No Mes found for {Script.StationConfig.Vendor}, disable MES");
                    foreach(var rs in Exec.Results)
                    {
                        rs.IsSFC = false;
                    }
                }
                else
                {
                    string data = string.Empty;
                    if (Script.SystemConfig.SFCs.SfcsUploadData)
                    {
                        if (Script.SystemConfig.SFCs.SfcsDataMode.Equals("JDM", StringComparison.OrdinalIgnoreCase))
                        {
                            execution.Template.GenerateSfcHeader_JDM(out data);
                        }
                        else
                        {
                            execution.Template.GenerateSFCHeader(out data);
                        }
                    }

                    Mes.Initialize(Script.SystemConfig.SFCs, data);
                    Info($"Initialize MES Data Header: {data}");

                    if(Mes is IInternalMes tymmes)
                    {
                        tymmes.GuiVersion = System.Windows.Forms.Application.ProductVersion;  // Could get wpf exec ver as well
                        tymmes.Operator = execution.Template.Operator;
                    }
                }
            }

            if (Script.Spec?.Secondary != null)
            {
                execution.OnTestCompleted += VerifySecondarySpec;
            }

            execution.OnPreUUTed += Execution_OnPreUUTed;
            execution.OnTestCompleted += Execution_OnTestCompleted;

            if (Script.GoldenSampleSpec != null)
            {
                if (EngineUtilities.CheckContainLimit(execution.Template.StepDatas, Script.GoldenSampleSpec.Limit))
                {
                    execution.OnTestCompleted += PickGoldenSample;
                }
                else
                {
                    throw new InvalidDataException("Golden Sample does not match current spec. Please check");
                }
            }

            if (Script.SystemConfig?.General?.CustomizeInputSn == true)
            {
                if (string.IsNullOrEmpty(Script.SystemConfig.SFCs.GetDsnApi))
                {
                    execution.OnUutIdentified += Execution_OnUutIdentified;
                }
                else
                {
                    execution.OnUutIdentified += Execution_OnUutIdentified_PalletSn;
                }
            }

            // For Restart, this event register later then create Execution, so it need execute at first
            Exec.ExecutionStarted += (obj, evt) => { ApplyHardwareSetting(); };
            Exec.ExecutionStopped += (obj, evt) => { RemoveHardwareSetting(); };
            Exec.ExecutionStopped += Exec_ExecutionStopped;
            //ApplyHardwareSetting();
            ApplyHooks();
        }

        private void Exec_ExecutionStopped(object sender, EventArgs e)
        {
        }

        private void Execution_DutSnUpdated(object sender, DutMessage e)
        {
            DutSnWaitForTest[e.SocketIndex] = null;
        }

        private void Execution_OnPreUUTed(object sender, TF_Result e)
        {
            DutSnWaitForTest[e.SocketIndex] = null;
        }

        public string SnInQueue { get; private set; }
        /// <summary>
        /// Update the DUT SN for slot, if slot is under test, make it as SN in Queue
        /// SN Queue only works when trigger is enable and slot count is 1
        /// if sn is null. clean up the sn, including the sn in queue
        /// <param name="obj"></param>
        /// </summary>
        /// <param name="slotindex"></param>
        /// <param name="sn"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void UpdateDutSn(int slotindex, string sn)
        {
            var activeslot = Results[slotindex];

            if (activeslot.Status == TF_TestStatus.IDLE ||
                activeslot.Status == TF_TestStatus.PASSED ||
                activeslot.Status == TF_TestStatus.FAILED ||
                activeslot.Status == TF_TestStatus.ERROR ||
                activeslot.Status == TF_TestStatus.WAIVE ||
                (Exec.ModelType == ModelType.Batch && activeslot.Status == TF_TestStatus.WAIT_DUT))
            {
                if(sn is null)
                {
                    DutSnWaitForTest[slotindex] = null;
                    //SnInQueue = null;

                    IsLoopStartStarted = false;
                }
                else if(DutSnWaitForTest[slotindex] is null)
                {
                    Execution_OnUutIdentified_Manual(sn, new DutMessage() { SocketIndex = slotindex, Message = "" });
                    activeslot.SerialNumber = sn;
                    DutSnWaitForTest[slotindex] = sn;
                    DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = slotindex, Message = $"SN Update to {sn}" });
                }
                else
                {
                    throw new InvalidOperationException($"Current slot {slotindex} sn has been updated to {DutSnWaitForTest[slotindex]}, Please Clean up it at first");
                }
            }
            else if (!((Script.HardwareConfig?.StartTrigger ?? StartTrigger_None.Instance) is StartTrigger_None) && Exec.SocketCount == 1)
            {
                //TODO, SN Queue, Only Enable when trigger is not None and CusotmerInputSn is false

                if (DutSnWaitForTest.Contains(sn))
                {
                    throw new InvalidOperationException($"Duplicated SN {sn} in slot {slotindex}. SN exist in {Array.IndexOf(DutSnWaitForTest, sn)}");
                }

                //SnInQueue = sn;
                Execution_OnUutIdentified_Manual(sn, new DutMessage() { SocketIndex = 0, Message = "" });
                DutSnWaitForTest[0] = sn;
            }
            else
            {
                throw new InvalidOperationException($"Current slot {slotindex} status {activeslot.Status}, could not Start Test");
            }
        }

        public event EventHandler ExecutionStarted
        {
            add
            {
                Exec.ExecutionStarted += value;
            }

            remove
            {
                Exec.ExecutionStarted -= value;
            }
        }

        public event EventHandler ExecutionStopped
        {
            add
            {
                Exec.ExecutionStopped += value;
            }

            remove
            {
                Exec.ExecutionStopped -= value;
            }
        }

        public event EventHandler<TF_Result> OnPreUUTLoop
        {
            add
            {
                Exec.OnPreUUTLoop += value;
            }

            remove
            {
                Exec.OnPreUUTLoop -= value;
            }
        }

        public event EventHandler<TF_Result> OnPreUUTing
        {
            add
            {
                Exec.OnPreUUTing += value;
            }

            remove
            {
                Exec.OnPreUUTing -= value;
            }
        }

        public event EventHandler<TF_Result> OnPreUUTed
        {
            add
            {
                Exec.OnPreUUTed += value;
            }

            remove
            {
                Exec.OnPreUUTed -= value;
            }
        }

        public event EventHandler<TF_Result> OnUutIdentified
        {
            add
            {
                Exec.OnUutIdentified += value;
            }

            remove
            {
                Exec.OnUutIdentified -= value;
            }
        }

        public event EventHandler<TF_Result> OnUutPassed
        {
            add
            {
                Exec.OnUutPassed += value;
            }

            remove
            {
                Exec.OnUutPassed -= value;
            }
        }

        public event EventHandler<TF_Result> OnUutFailed
        {
            add
            {
                Exec.OnUutFailed += value;
            }

            remove
            {
                Exec.OnUutFailed -= value;
            }
        }

        public event EventHandler<TF_Result> OnError
        {
            add
            {
                Exec.OnError += value;
            }

            remove
            {
                Exec.OnError -= value;
            }
        }

        public event EventHandler<TF_Result> OnTestCompleted
        {
            add
            {
                Exec.OnTestCompleted += value;
            }

            remove
            {
                Exec.OnTestCompleted -= value;
            }
        }

        public event EventHandler<TF_Result> OnPostUUTing
        {
            add
            {
                Exec.OnPostUUTing += value;
            }

            remove
            {
                Exec.OnPostUUTing -= value;
            }
        }

        public event EventHandler<TF_Result> OnPostUUTed
        {
            add
            {
                Exec.OnPostUUTed += value;
            }

            remove
            {
                Exec.OnPostUUTed -= value;
            }
        }

        public event EventHandler<TF_Result> OnPostUUTLoop
        {
            add
            {
                Exec.OnPostUUTLoop += value;
            }

            remove
            {
                Exec.OnPostUUTLoop -= value;
            }
        }

        private void ApplyHooks()
        {
            var hooks = Enum.GetValues(typeof(ProcessHook));

            foreach (ProcessHook hook in hooks)
            {
                var p = System.IO.Path.Combine(Exec.Workbase, "Hooks", $"{hook}.bat");

                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = p;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.StartInfo.WorkingDirectory = System.IO.Path.Combine(Exec.Workbase, "Hooks");
                if (System.IO.File.Exists(p))
                {
                    switch (hook)
                    {
                        case ProcessHook.OnInitializing:
                            Exec.ExecutionStarted += (object sender, EventArgs e) => { process.Start(); };
                            break;
                        case ProcessHook.OnPreUUTLoop:
                            Exec.OnPreUUTLoop += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnPreUUTing:
                            Exec.OnPreUUTing += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnPreUUTed:
                            Exec.OnPreUUTed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnUUTIdentified:
                            Exec.OnUutIdentified += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnUUTPassed:
                            Exec.OnUutPassed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnUUTFailed:
                            Exec.OnUutFailed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnError:
                            Exec.OnError += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnTestCompleted:
                            Exec.OnTestCompleted += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnQuit:
                            Exec.ExecutionStopped += (object sender, EventArgs e) => { process.Start(); };
                            break;
                        case ProcessHook.OnPostUUTing:
                            Exec.OnPostUUTing += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnPostUUTed:
                            Exec.OnPostUUTed += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                        case ProcessHook.OnPostUUTLoop:
                            Exec.OnPostUUTLoop += (object sender, TF_Result rs) => { process.StartInfo.Arguments = rs.SocketId; process.Start(); };
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Verify MES, call when manually input DUT SN
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dutmsg"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void Execution_OnUutIdentified_Manual(object sender, DutMessage dutmsg)
        {
            var e = this.Results[dutmsg.SocketIndex];

            string sn = null;
            if(sender is string str)
            {
                sn = str;
            }
            else
            {
                sn = e.SerialNumber;
            }

            try
            {
                if (sn?.Length > 2 && e.IsSFC)
                {
                    if (e.SFCsConfig.PersitPartNo is null) 
                    { 
                        e.PartNo = Mes?.GetPartNo(e.SFCsConfig, sn);
                    } 
                    else
                    {
                        e.PartNo = e.SFCsConfig.PersitPartNo;
                    }

                    if (e.SFCsConfig.PersitLineNo is null)
                    {
                        e.LineNo = Mes?.GetLineNo(e.SFCsConfig, sn);
                    }
                    else
                    {
                        e.LineNo = e.SFCsConfig.PersitLineNo;
                    }

                    if (!e.SFCsConfig.DisableCheckStation) Mes?.CheckStation(e.SFCsConfig, sn, e.LineNo);
                }

                //Message = $"{e.SerialNumber} Identified, Start To Run";
            }
            catch (Exception ex)
            {
                //e.SerialNumber = null;  // set sn to be null, otherwise the trigger will still start a new test  // skip for SN queue might make testing sn be null 
                //SnInQueue = null;
                throw new InvalidOperationException($"Validate DUT SN Failed. {ex.Message}");
            }
        }

        /// <summary>
        /// Verify MES, call when get DUT SN from Mes API with Pallet SN
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dutmsg"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void Execution_OnUutIdentified_PalletSn(object sender, TF_Result e)
        {
            try
            {
                if (e.SerialNumber.Length > 2)
                {
                    e.SerialNumber = Mes?.ExecMesApi(e.SFCsConfig, e.SFCsConfig.GetDsnApi, e.SerialNumber);

                    Execution_OnUutIdentified(sender, e);
                }
            }
            catch (Exception ex)
            {
                e.ErrorMessage = new ErrorMsg((int)ErrorCode.MesError, ex.Message);
                e.Status = TF_TestStatus.ERROR;
                e.SerialNumber = null;
            }
        }

        /// <summary>
        /// Verify MES, call when get DUT SN from engine, traditionnally it is test with auto sn reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="dutmsg"></param>
        private void Execution_OnUutIdentified(object sender, TF_Result e)
        {
            try
            {
                if (e.SerialNumber.Length > 2 && e.IsSFC)
                {
                    if (e.SFCsConfig.PersitPartNo is null) e.PartNo = Mes.GetPartNo(e.SFCsConfig, e.SerialNumber);

                    if (e.SFCsConfig.PersitLineNo is null) e.LineNo = Mes.GetLineNo(e.SFCsConfig, e.SerialNumber);

                    if (!e.SFCsConfig.DisableCheckStation) Mes.CheckStation(e.SFCsConfig, e.SerialNumber, e.LineNo);
                }

                //Message = $"{e.SerialNumber} Identified, Start To Run";
            }
            catch (Exception ex)
            {
                e.ErrorMessage = new ErrorMsg((int)ErrorCode.MesError, ex.Message);
                e.Status = TF_TestStatus.ERROR;
                e.SerialNumber = null;
                throw ex;  // execution need this exception to reaction; 
            }
        }

        /// <summary>
        /// Commit MES when Test Completed. If enable PostCheckStation, recheck after commit with 500ms delay and then restart test if MES allow test
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Execution_OnTestCompleted(object sender, TF_Result e)
        {
            try
            {
                //DutSnWaitForTest[e.SocketIndex] = null;

                if (e.SerialNumber?.Length > 2)
                {
                    if (e.IsSFC)
                    {
                        if (e.Status == TF_TestStatus.PASSED || e.Status == TF_TestStatus.FAILED || e.Status == TF_TestStatus.WAIVE) // SFC does not support || e.Status == TF_TestStatus.ERROR)
                        {
                            Mes?.CommitMesResult(e, e.SpecialData, e.ExtColumns, e.ExtValues);
                            
                            if (e.SFCsConfig.PostCheckStation)  // For repeat test
                            {
                                try
                                {
                                    Thread.Sleep(500);
                                    Mes.CheckStation(e.SFCsConfig, e.SerialNumber, e.LineNo);

                                    //If no exception, which means the DUT could be retested
                                    if (sender is IExecution exec)
                                    {
                                        StartNewTest(exec.Results.ToList().IndexOf(e));
                                    }
                                }
                                catch
                                {

                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                e.ErrorMessage = new ErrorMsg((int)ErrorCode.MesError, ex.Message);
                e.Status = TF_TestStatus.ERROR;
            }
        }

        /// <summary>
        /// ONLY Normoal Process trig picking golden sample
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PickGoldenSample(object sender, TF_Result e)
        {
            if (Script.GoldenSampleSpec is null) return;
            if (ExecutionMode == ExecutionMode.Normal)
            {
                if (e.SerialNumber?.Length > 2 && (e.Status == TF_TestStatus.PASSED || e.Status == TF_TestStatus.WAIVE))
                {
                    if (e.StepDatas.VerifyExternalLimit(Script.GoldenSampleSpec.Limit, out Nest<TF_StepData> defectitem))
                    {
                        MessageBox.Show($"The SN {e.SerialNumber} in slot {e.SocketIndex} PASS the golden sample specification", "Golden Sample Detected", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }

        /// <summary>
        /// Check Spec if failed before Post Action for MES status might be updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void VerifySecondarySpec(object sender, TF_Result e)
        {
            if (ExecutionMode == ExecutionMode.Normal && e.Status == TF_TestStatus.FAILED)
            {
                int grade = 1;
                TF_Spec secondary = e.Specification.Secondary;
                do
                {
                    if (e.StepDatas.VerifySecondaryLimit(secondary.Limit))
                    {
                        e.Grade = secondary.Grade;
                        e.Status = TF_TestStatus.WAIVE;
                        break;
                    }
                    grade++;
                    secondary = secondary.Secondary;
                }
                while (secondary != null);
            }
            else
            {
                e.Grade = e.Specification.Grade;
            }
        }

        private void RelayOnUutIdentified(object sender, TF_Result e) 
        {
            Info($"RelayOnUutIdentified {e.SocketIndex}, {Script.HardwareConfig.UutIdentifiedRoute[e.SocketIndex]}");
            if (Script.HardwareConfig.UutIdentifiedRoute[e.SocketIndex] >= 0)
            {
                Script.HardwareConfig.RelayArray.SetRelay(Script.HardwareConfig.UutIdentifiedRoute[e.SocketIndex], Script.HardwareConfig.SlotMasks[e.SocketIndex]);
            }
        }
        private void RelayOnPreUUTed(object sender, TF_Result e) 
        {
            Info($"RelayOnPreUUTed {e.SocketIndex}, {Script.HardwareConfig.PreUutRoute[e.SocketIndex]}");
            if (Script.HardwareConfig.PreUutRoute[e.SocketIndex] >= 0)
            {
                Script.HardwareConfig.RelayArray.SetRelay(Script.HardwareConfig.PreUutRoute[e.SocketIndex], Script.HardwareConfig.SlotMasks[e.SocketIndex]);
            }
        }
        private void RelayOnPostUUTed(object sender, TF_Result e)
        {
            Info($"RelayOnPostUUTed {e.SocketIndex}, {Script.HardwareConfig.PostUutRoute[e.SocketIndex]}");
            if (Script.HardwareConfig.PostUutRoute[e.SocketIndex] >= 0)
            {
                Script.HardwareConfig.RelayArray.SetRelay(Script.HardwareConfig.PostUutRoute[e.SocketIndex], Script.HardwareConfig.SlotMasks[e.SocketIndex]);
            }
        }
        private void RelayOnPostUUTLoop(object sender, TF_Result e) { Script.HardwareConfig.RelayArray.Close(); }
        private void RelayOnQuit(object sender, EventArgs e) { Script.HardwareConfig.RelayArray.Clear(); }

        /// <summary>
        /// loop Check if DUT Present, and then request DUT In and Close Front Door, and then loop check DUT Ready with 30s over time
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void Fixture_OnPreUUTed(object sender, TF_Result e)
        {
            Script.HardwareConfig.Fixture.GetStateDutPresent(out bool dutpresent, e.SocketIndex);
            while (!dutpresent)
            {
                Script.HardwareConfig.Fixture.GetStateDutPresent(out dutpresent, e.SocketIndex);

                if (!dutpresent) Thread.Sleep(200);
            }

            if (Script.HardwareConfig.Fixture.AutoDutIn)
            {
                Script.HardwareConfig.Fixture.DutIn(e.SocketIndex);
                Script.HardwareConfig.Fixture.CloseFrontDoor(e.SocketIndex);
            }

            bool dutready = false;

            DateTime dt = DateTime.Now;
            while (DateTime.Now.Subtract(dt).TotalSeconds < 30 && !dutready)
            {
                Script.HardwareConfig.Fixture.CheckDutReady(out dutready, e.SocketIndex);
            }

            if (!dutready)
            {
                throw new InvalidOperationException($"Test Fixture Error on slot {e.SocketIndex}, DUT Not Ready");
            }
        }

        private void FixtureOnPostUUTed(object sender, TF_Result rs)
        {
            Info($"FixtureOnPostUUTed running");
            Script.HardwareConfig.Fixture.OpenFrontDoor(rs.SocketIndex);
            Script.HardwareConfig.Fixture.DutOut(rs.SocketIndex);
        }

        private void LockableFixturePreUUT(object sender, TF_Result rs)
        {
            if (Script.HardwareConfig.Fixture is ILockableFixture lf)
            {
                lf.SetLockState(rs.SocketIndex, true);  // lock the fixture when test start
            }
        }

        private void LockableFixturePostUUT(object sender, TF_Result rs)
        {
            if(Script.HardwareConfig.Fixture is ILockableFixture lf)
            {
                lf.SetLockState(rs.SocketIndex, false);  // Unlock the fixture when test done
            }
        }
        

        private void FixtureOnPostUUTLoop(object sender, TF_Result e) { Script.HardwareConfig.Fixture.Close(); }
        private void FixtureOnQuit(object sender, EventArgs e) { Script.HardwareConfig.Fixture.Clear(); }

        /// <summary>
        /// ATC04 need Skip DUT when ERROR. particular MES Error, for it is automation line, the return DUT need to pass through all previous station
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="rs"></param>
        private void ATC04ErrorHandler(object sender, TF_Result rs)
        {
            if (rs.ErrorMessage.Code == (int)ErrorCode.MesError)
            {
                ((ATC04)Script.HardwareConfig.Fixture).SkipDut(rs.SocketIndex);
            }
            else
            {
                FixtureOnPostUUTed(sender, rs);
            }
        }

        /// <summary>
        /// Do NOT use DialogCoordinator for is might call from other window, which might not register it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FixtureStartTrigged(object sender, DutMessage e)
        {
            Info($"Fixture Trigged {e.SocketIndex}");
            if (e.SocketIndex < 0 || e.SocketIndex >= Exec.SocketCount)   // might using N Socket tester in N-1 socket software
            { }
            else
            {
                // If No SN Update, Do Not Test
                if (!Script.SystemConfig.General.CustomizeInputSn)
                {
                    if (Script.HardwareConfig.Fixture.SocketCount == 1 && Script.SystemConfig.General.SocketCount > 1)
                    {
                        foreach (var rs in Results)
                        {
                            if (DutSnWaitForTest[rs.SocketIndex] != null)
                            {
                                if (rs.Status == TF_TestStatus.IDLE || rs.Status == TF_TestStatus.PASSED || rs.Status == TF_TestStatus.FAILED || rs.Status == TF_TestStatus.ERROR || rs.Status == TF_TestStatus.WAIVE)
                                {
                                    rs.SerialNumber = DutSnWaitForTest[rs.SocketIndex];
                                    DutSnWaitForTest[rs.SocketIndex] = null;
                                    //SnInQueue = null;
                                    //DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = rs.SocketIndex, Message = $"SN Update to {rs.SerialNumber}" });
                                }
                                else
                                {
                                    return;
                                }
                            }
                            else
                            {
                                switch (Results[rs.SocketIndex].Status)
                                {
                                    case TF_TestStatus.IDLE:
                                    case TF_TestStatus.PASSED:
                                    case TF_TestStatus.WAIVE:
                                    case TF_TestStatus.FAILED:
                                    case TF_TestStatus.ERROR:
                                    case TF_TestStatus.TERMINATED:
                                    case TF_TestStatus.ABORT:
                                        break;
                                    default:
                                        Debug($"Slot {rs.SocketIndex} status: {rs.Status}. Trig ignored");
                                        return;
                                }
                            }

                            if (rs.SerialNumber is null)
                            {
                                continue;
                            }

                            bool state;

                            Script.HardwareConfig.Fixture.GetStateSafety(out state, rs.SocketIndex);
                            while (!state)
                            {
                                if (MessageBox.Show("Fixture Safety Rejected, Please confirm the safety sensor is ready and continue testing, otherwise skip this test", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                {
                                    Thread.Sleep(100);
                                    Script.HardwareConfig.Fixture.GetStateSafety(out state, rs.SocketIndex);
                                }
                            }

                            StartNewTest(rs.SocketIndex);
                        }
                    }
                    else
                    {
                        var rs = Results[e.SocketIndex];
                        if (DutSnWaitForTest[e.SocketIndex] != null)
                        {
                            if (rs.Status == TF_TestStatus.IDLE || rs.Status == TF_TestStatus.PASSED || rs.Status == TF_TestStatus.FAILED || rs.Status == TF_TestStatus.ERROR || rs.Status == TF_TestStatus.WAIVE)
                            {
                                rs.SerialNumber = DutSnWaitForTest[e.SocketIndex];
                                DutSnWaitForTest[rs.SocketIndex] = null;
                                //SnInQueue = null;
                                //DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = rs.SocketIndex, Message = $"SN Update to {rs.SerialNumber}" });
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            switch (Results[e.SocketIndex].Status)
                            {
                                case TF_TestStatus.IDLE:
                                case TF_TestStatus.PASSED:
                                case TF_TestStatus.WAIVE:
                                case TF_TestStatus.FAILED:
                                case TF_TestStatus.ERROR:
                                case TF_TestStatus.TERMINATED:
                                case TF_TestStatus.ABORT:
                                    break;
                                default:
                                    Debug($"Slot {e.SocketIndex} status: {rs.Status}. Trig ignored");
                                    return;
                            }
                        }

                        if (rs.SerialNumber is null)
                        {
                            return;
                        }

                        bool state;

                        Script.HardwareConfig.Fixture.GetStateSafety(out state, e.SocketIndex);
                        while (!state)
                        {
                            if (MessageBox.Show("Fixture Safety Rejected, Please confirm the safety sensor is ready and continue testing, otherwise skip this test", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                            {
                                Thread.Sleep(100);
                                Script.HardwareConfig.Fixture.GetStateSafety(out state, e.SocketIndex);
                            }
                        }

                        StartNewTest(e.SocketIndex);
                    }
                }
                else
                {
                    bool state;

                    Script.HardwareConfig.Fixture.GetStateSafety(out state, e.SocketIndex);
                    while (!state)
                    {
                        if (MessageBox.Show("Fixture Safety Rejected, Please confirm the safety sensor is ready and continue testing, otherwise skip this test", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            Thread.Sleep(100);
                            Script.HardwareConfig.Fixture.GetStateSafety(out state, e.SocketIndex);
                        }
                    }

                    StartNewTest(e.SocketIndex);
                }
            }
        }

        private void StartTrigged(object sender, DutMessage e)
        {
            if (e.SocketIndex < 0)
            {
                for (int i = 0; i < Results.Count; i++)
                {
                    switch (Results[i].Status)
                    {
                        case TF_TestStatus.IDLE:
                        case TF_TestStatus.PASSED:
                        case TF_TestStatus.WAIVE:
                        case TF_TestStatus.FAILED:
                        case TF_TestStatus.ERROR:
                        case TF_TestStatus.TERMINATED:
                        case TF_TestStatus.ABORT:
                            StartNewTest(SlotIndex);
                            break;
                        default:
                            Debug($"Slot {i} status: {Results[i].Status}. Trig ignored");
                            break;
                    }
                }
            }
            else
            {
                switch (Results[SlotIndex].Status)
                {
                    case TF_TestStatus.IDLE:
                    case TF_TestStatus.PASSED:
                    case TF_TestStatus.WAIVE:
                    case TF_TestStatus.FAILED:
                    case TF_TestStatus.ERROR:
                    case TF_TestStatus.TERMINATED:
                    case TF_TestStatus.ABORT:
                        StartNewTest(SlotIndex);
                        break;
                    default:
                        Debug($"Slot {SlotIndex} status: {Results[e.SocketIndex].Status}. Trig ignored");
                        break;
                }
                
            }
        }

        private void StartTriggedWithSnQueue(object sender, DutMessage e)
        {
            if (e.SocketIndex < 0)
            {
                for (int i = 0; i < SocketCount; i++)
                {
                    var rs = Results[i];

                    if (DutSnWaitForTest[i] != null)
                    {
                        if (rs.Status == TF_TestStatus.IDLE || rs.Status == TF_TestStatus.PASSED || rs.Status == TF_TestStatus.FAILED || rs.Status == TF_TestStatus.ERROR || rs.Status == TF_TestStatus.WAIVE)
                        {
                            rs.SerialNumber = DutSnWaitForTest[i];

                            DutSnWaitForTest[i] = null;
                            DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = rs.SocketIndex, Message = $"SN Update to {rs.SerialNumber}" });
                            //SnInQueue = null;
                            StartNewTest(i);
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        StartTrigged(sender, e);
                    }
                }
            }
            else
            {
                var rs = Results[e.SocketIndex];

                if (DutSnWaitForTest[e.SocketIndex] != null)
                {
                    if (rs.Status == TF_TestStatus.IDLE || rs.Status == TF_TestStatus.PASSED || rs.Status == TF_TestStatus.FAILED || rs.Status == TF_TestStatus.ERROR || rs.Status == TF_TestStatus.WAIVE)
                    {
                        rs.SerialNumber = DutSnWaitForTest[e.SocketIndex];

                        DutSnWaitForTest[rs.SocketIndex] = null;
                        DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = rs.SocketIndex, Message = $"SN Update to {rs.SerialNumber}" });
                        //SnInQueue = null;
                        StartNewTest(e.SocketIndex);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    StartTrigged(sender, e);
                }
            }
        }

        private void PostUUTedWithSnQueue_DoorClosed(object sender, TF_Result rs)
        {
            if (DutSnWaitForTest[rs.SocketIndex] is null) return;

            bool state = true;
            try
            {
                Debug($"Start Loop Fetch Fixture Door Close {state} for Start SN in Queue");
                while (state && IsLoopStartStarted) // the Dirve might need request, otherwise no response, so periodly request state till state right
                {
                    ((StartTrigger_Fixture)Script.HardwareConfig?.StartTrigger).Fixture.GetStateFrontDoorClose (out state, rs.SocketIndex);

                    Thread.Sleep(100);
                }
                Debug($"Fixture Door Close {state}");
            }
            catch (Exception ex)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.ToString(), "Check Door Open Failed");
                });
            }

            LoopOnDoorClosed(this, new DutMessage() { SocketIndex = rs.SocketIndex, Message = "Handle Sn In Queue" });
        }

        private void PostUUTedWithSnQueue_DutReady(object sender, TF_Result rs)
        {
            if (DutSnWaitForTest[rs.SocketIndex] is null) return;

            bool state = true;
            try
            {
                Debug($"Start Loop Fetch Fixture Door Close {state} for Start SN in Queue");
                while (state && IsLoopStartStarted) // the Dirve might need request, otherwise no response, so periodly request state till state right
                {
                    ((StartTrigger_Fixture)Script.HardwareConfig?.StartTrigger).Fixture.GetStateFrontDoorClose(out state, rs.SocketIndex);

                    Thread.Sleep(100);
                }
                Debug($"Fixture Door Close {state}");
            }
            catch (Exception ex)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                {
                    MessageBox.Show(ex.ToString(), "Check Door Open Failed");
                });
            }

            LoopOnDutReady(this, new DutMessage() { SocketIndex = rs.SocketIndex, Message = "Handle Sn In Queue" });
        }

        private void SnReaderOnDutPresent(object sender, DutMessage dutmsg)
        {
            int idx = dutmsg.SocketIndex;
            if (idx < 0 && Exec.SocketCount <= 1) idx = 0;
            var rs = Exec.Results[dutmsg.SocketIndex];

            rs.SerialNumber = Script.HardwareConfig.SerialNumberReader.ReadSerialNumber();
            DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = dutmsg.SocketIndex, Message = $"SN Update to {rs.SerialNumber}" });
        }

        private void SnReaderOnPostUUTLoop(object sender, TF_Result e) { Script.HardwareConfig.SerialNumberReader.Close(); }

        private void SnReaderOnDutInDone(object sender, DutMessage dutmsg)
        {
            int idx = dutmsg.SocketIndex;
            if (idx < 0 && Exec.SocketCount <= 1) idx = 0;
            var rs = Exec.Results[dutmsg.SocketIndex];

            var rtn = Script.HardwareConfig.SerialNumberReader.ReadSerialNumber();

            if (string.IsNullOrEmpty(Script.SystemConfig.SFCs.GetDsnApi))
            {
                rs.SerialNumber = rtn;
            }
            else
            {
                rs.SerialNumber = Mes?.ExecMesApi(Script.SystemConfig.SFCs, Script.SystemConfig.SFCs.GetDsnApi, rtn);
            }
            
            DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = dutmsg.SocketIndex, Message = $"SN Update to {rs.SerialNumber}" });
        }

        private bool IsLoopStartStarted = false;
        //private System.Timers.Timer[] FixtureTriggerTimers;

        private void LoopOnDoorClosed(object sender, DutMessage dutmsg)
        {
            //FixtureTriggerTimers[dutmsg.SocketIndex].Start();
            if (IsLoopStartStarted) return;
            Task.Run(() =>
            {
                IsLoopStartStarted = true;
                bool state = false;
                try
                {
                    Debug($"Start Loop Fetch Fixture Slot {dutmsg.SocketIndex} Status {state}");
                    while (!state && IsLoopStartStarted) // the Dirve might need request, otherwise no response, so periodly request state till state right
                    {
                        ((StartTrigger_Fixture)Script.HardwareConfig?.StartTrigger).Fixture.GetStateFrontDoorClose(out state, dutmsg.SocketIndex);

                        Thread.Sleep(20);  // TODO, Execution Terminated   // APGUI timeout 10ms to detect the trigger
                    }
                    Debug($"Fixture Status {state}");
                }
                catch (Exception ex)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        MessageBox.Show(ex.ToString(), "Check Door Closed Failed");
                    });
                }
                finally
                {
                    IsLoopStartStarted = false;
                }
            }
            );
        }

        //private void FixtureTriggerTimer_DoorClosed_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    lock (Exec)
        //    {
        //        if (sender is System.Timers.Timer timer)
        //        {
        //            for (int i = 0; i < SocketCount; i++)
        //            {
        //                if (timer == FixtureTriggerTimers[i])
        //                {
        //                    bool state;
        //                    ((StartTrigger_Fixture)Script.HardwareConfig?.StartTrigger).Fixture.GetStateFrontDoorClose(out state, i);
                            
        //                    if (state)
        //                    {
        //                        timer.Stop();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private void FixtureTriggerTimer_DutReady_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    lock (Exec)
        //    {
        //        if (sender is System.Timers.Timer timer)
        //        {
        //            for (int i = 0; i < SocketCount; i++)
        //            {
        //                if (timer == FixtureTriggerTimers[i])
        //                {
        //                    bool state;
        //                    ((StartTrigger_Fixture)Script.HardwareConfig?.StartTrigger).Fixture.CheckDutReady(out state, i);

        //                    if (state)
        //                    {
        //                        timer.Stop();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        private void LoopOnDutReady(object sender, DutMessage dutmsg)
        {
            //FixtureTriggerTimers[dutmsg.SocketIndex].Start();
            if (IsLoopStartStarted) return;
            Task.Run(() =>
            {
                bool state = false;
                IsLoopStartStarted = true;
                try
                {
                    while (!state && IsLoopStartStarted)  // the Dirve might need request, otherwise no response, so periodly request state till state right
                    {
                        ((StartTrigger_Fixture)Script.HardwareConfig?.StartTrigger).Fixture.CheckDutReady(out state, dutmsg.SocketIndex);

                        Thread.Sleep(100);  // TODO, Execution Terminated
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() =>
                    {
                        MessageBox.Show(ex.ToString(), "Check Dut Ready Failed");
                    });
                }
                finally
                {
                    IsLoopStartStarted = false;
                }
            }
            );
        }

        public void RemoveHardwareSetting()
        {
            IsLoopStartStarted = false;   // Quit the loop start if it runs

            bool isvalid_relayarray = !(Script.HardwareConfig?.RelayArray is null || Script.HardwareConfig?.RelayArray is RelayArray_None);
            bool isvalid_fixture = !(Script.HardwareConfig?.Fixture is null || Script.HardwareConfig?.Fixture is Fixture_None);
            bool isnull_starttrigger = Script.HardwareConfig?.StartTrigger is null || Script.HardwareConfig.StartTrigger is StartTrigger_None;
            bool isnull_dutsnreader = Script.HardwareConfig?.SerialNumberReader is null || Script.HardwareConfig?.SerialNumberReader is SerialNumberReader_None;

            if (isvalid_relayarray)
            {
                Script.HardwareConfig.RelayArray.Close();
                Script.HardwareConfig.RelayArray.Clear();

                Exec.OnUutIdentified -= RelayOnUutIdentified;
                Exec.OnPreUUTed -= RelayOnPreUUTed;
                Exec.OnPostUUTed -= RelayOnPostUUTed;
                Exec.OnPostUUTLoop -= RelayOnPostUUTLoop;

                Exec.ExecutionStopped -= RelayOnQuit;
            }

            if (isvalid_fixture)
            {
                Script.HardwareConfig.Fixture.Close();
                Script.HardwareConfig.Fixture.Clear();

                if (Script.HardwareConfig.Fixture.AutoDutOut)
                {
                    if (Script.HardwareConfig.Fixture is ATC04 atc04)    // TODO: Retest support
                    {
                        Exec.OnError -= ATC04ErrorHandler;
                    }
                    else
                    {
                        Exec.OnPostUUTed -= FixtureOnPostUUTed;
                    }
                }
                Exec.OnPostUUTLoop -= FixtureOnPostUUTLoop;

                if (Script.HardwareConfig.Fixture is ILockableFixture lf)
                {
                    Exec.OnPreUUTed -= LockableFixturePreUUT;
                    Exec.OnPostUUTed -= LockableFixturePostUUT;
                }

                if (!isnull_dutsnreader)   // Depends on fixture
                {
                    Script.HardwareConfig?.SerialNumberReader.Close();
                    Script.HardwareConfig?.SerialNumberReader.Clear();

                    Task monitortask = null; // Monitor Fixture Status, for trigger read sn
                    monitortask?.Dispose();
                    if (Script.HardwareConfig.TrigOnDutPresent)
                    {
                        Script.HardwareConfig.Fixture.OnDutPresent -= SnReaderOnDutPresent;
                    }
                    else
                    {
                        Script.HardwareConfig.Fixture.DutInDone -= SnReaderOnDutInDone;
                    }

                    /// TODO: Execution Stop has not defined clear currently
                    //Execution.ExecutionStopped += (sender, e) => { monitortask.Dispose(); };

                    //Exec.OnPostUUTLoop -= SnReaderOnPostUUTLoop;
                }

                Exec.ExecutionStopped -= FixtureOnQuit;
            }

            if (isnull_starttrigger)
            {
                DutSnUpdated -= Execution_DutSnUpdated;
                DutSnUpdated -= StartTrigged;

                if (isvalid_fixture)
                {
                    Exec.OnPreUUTed -= Fixture_OnPreUUTed;
                }
            }
            else
            {
                if (Script.HardwareConfig?.StartTrigger is StartTrigger_Fixture trigfixture && isvalid_fixture)
                {
                    //trigfixture.Fixture = Script.HardwareConfig?.Fixture;
                    //Script.HardwareConfig.StartTrigger.StartTrigged = FixtureStartTrigged;

                    //foreach(var timer in FixtureTriggerTimers)
                    //{
                    //    timer.Enabled = false;
                    //    timer.Dispose();
                    //}

                    if (Enum.TryParse(trigfixture.Source, out FixtureStartTriggerType sttype))
                    {
                        switch (sttype)
                        {
                            case FixtureStartTriggerType.Door_Closing:
                            case FixtureStartTriggerType.Door_Closed:
                                DutSnUpdated -= LoopOnDoorClosed;

                                break;

                            case FixtureStartTriggerType.Dut_Ready:
                                DutSnUpdated -= LoopOnDutReady;
                                break;
                        }
                    }
                }
                else
                {
                    // Fixture Trigger, which means when test started, the fixture is supposed to be ready
                    // execution.OnPreUUTed += Fixture_OnPreUUTed;
                    Script.HardwareConfig.StartTrigger.StartTrigged = null;
                }

                Script.HardwareConfig?.StartTrigger.Clear();
                //if (Script.SystemConfig.General.SocketCount == 1)
                //{
                //    Exec.OnTestCompleted -= HandleSnInQueue;
                //}
            }
        }

        /// <summary>
        /// Apply the Hardware event into Execution
        /// </summary>
        public void ApplyHardwareSetting()
        {
            bool isvalid_relayarray = !(Script.HardwareConfig?.RelayArray is null || Script.HardwareConfig?.RelayArray is RelayArray_None);
            bool isvalid_fixture = (!(Script.HardwareConfig?.Fixture is null || Script.HardwareConfig?.Fixture is Fixture_None));// && Script.HardwareConfig?.StartTrigger is StartTrigger_Fixture); ; ;
            bool isnull_starttrigger = Script.HardwareConfig?.StartTrigger is null || Script.HardwareConfig.StartTrigger is StartTrigger_None;
            bool isnull_dutsnreader = Script.HardwareConfig?.SerialNumberReader is null || Script.HardwareConfig?.SerialNumberReader is SerialNumberReader_None;

            if (isvalid_relayarray)
            {
                if (Script.HardwareConfig.RelayArray is RelayArray_Proxy proxy)
                {
                    proxy.Workbase = Exec.Workbase;
                }

                Script.HardwareConfig.RelayArray.Initialize();
                Script.HardwareConfig.RelayArray.Open();

                Exec.OnUutIdentified += RelayOnUutIdentified;
                Exec.OnPreUUTed += RelayOnPreUUTed;
                Exec.OnPostUUTed += RelayOnPostUUTed;  // should before Fixture post uut
                Exec.OnPostUUTLoop += RelayOnPostUUTLoop;

                Exec.ExecutionStopped += RelayOnQuit;
            }

            if (isvalid_fixture)
            {
                Script.HardwareConfig.Fixture.Initialize();
                Script.HardwareConfig.Fixture.Open();

                if (Script.HardwareConfig.Fixture is ILockableFixture lf)
                {
                    Exec.OnPreUUTed += LockableFixturePreUUT;
                    Exec.OnPostUUTed += LockableFixturePostUUT;
                }

                if (Script.HardwareConfig.Fixture.AutoDutOut)
                {
                    Exec.OnPostUUTed += FixtureOnPostUUTed;
                    if (Script.HardwareConfig.Fixture is ATC04 atc04)    // TODO: Retest support
                    {
                        Exec.OnError += ATC04ErrorHandler;
                    }
                }
                
                Exec.OnPostUUTLoop += FixtureOnPostUUTLoop;

                if (!isnull_dutsnreader)   // Depends on fixture
                {
                    Script.HardwareConfig?.SerialNumberReader.Initialize();
                    Script.HardwareConfig?.SerialNumberReader.Open();

                    Task monitortask = null; // Monitor Fixture Status, for trigger read sn
                    if (Script.HardwareConfig.TrigOnDutPresent)
                    {
                        Script.HardwareConfig.Fixture.OnDutPresent += SnReaderOnDutPresent;

                        monitortask = Task.Run(() =>
                        {
                            for (; ; )
                            {
                                for (int slotidx = 0; slotidx < Exec.SocketCount; slotidx++)
                                {
                                    if (Exec.Results[slotidx].Status != TF_TestStatus.TESTING)
                                    {
                                        try
                                        {
                                            Script.HardwareConfig.Fixture.GetStateDutPresent(out bool state, slotidx);
                                        }
                                        catch (Exception ex)
                                        {
                                            Exec.Results[slotidx].ErrorMessage = new ErrorMsg((int)ErrorCode.FixtureTriggerError, ex.Message);
                                            Exec.Results[slotidx].End(TF_TestStatus.ERROR);
                                        }
                                    }
                                    Thread.Sleep(50);
                                }
                            }
                        });
                    }
                    else
                    {
                        Script.HardwareConfig.Fixture.DutInDone += SnReaderOnDutInDone;

                        monitortask = Task.Run(() =>
                        {
                            for (; ; )
                            {
                                for (int slotidx = 0; slotidx < Exec.SocketCount; slotidx++)
                                {
                                    if (Exec.Results[slotidx].Status != TF_TestStatus.TESTING)
                                    {
                                        try
                                        {
                                            Script.HardwareConfig.Fixture.CheckDutReady(out bool state, slotidx);
                                        }
                                        catch (Exception ex)
                                        {
                                            Exec.Results[slotidx].ErrorMessage = new ErrorMsg((int)ErrorCode.FixtureTriggerError, ex.Message);
                                            Exec.Results[slotidx].End(TF_TestStatus.ERROR);
                                        }
                                    }

                                    Thread.Sleep(50);
                                }
                            }
                        });
                    }

                    /// TODO: Execution Stop has not defined clear currently
                    Exec.ExecutionStopped += (sender, e) => { monitortask?.Dispose(); Script.HardwareConfig?.SerialNumberReader.Close(); Script.HardwareConfig?.SerialNumberReader.Clear(); };

                    //execution.OnPreUUTed += (obj, rs) =>
                    //{
                    //    rs.SerialNumber = Script.HardwareConfig.SerialNumberReader.ReadSerialNumber();
                    //    DutSnUpdated?.Invoke(this, new DutMessage() { SocketIndex = ActiveSlotIndex, Message = $"SN Update to {rs.SerialNumber}" });
                    //};

                    //Exec.OnPostUUTLoop += SnReaderOnPostUUTLoop;
                }

                Exec.ExecutionStopped += FixtureOnQuit;
            }

            if (isnull_starttrigger)
            {
                DutSnUpdated += Execution_DutSnUpdated;
                DutSnUpdated += StartTrigged;

                if (isvalid_fixture)
                {
                    Exec.OnPreUUTed += Fixture_OnPreUUTed;
                }
            }
            else
            {
                if (Script.HardwareConfig?.StartTrigger is StartTrigger_Fixture trigfixture && isvalid_fixture)
                {
                    trigfixture.Fixture = Script.HardwareConfig?.Fixture;
                    Script.HardwareConfig.StartTrigger.StartTrigged = FixtureStartTrigged;
                    //FixtureTriggerTimers = new System.Timers.Timer[SocketCount];

                    //for (int i = 0; i < SocketCount; i++)
                    //{
                    //    FixtureTriggerTimers[i] = new System.Timers.Timer() { AutoReset = true, Enabled = false};
                    //}

                    if (Enum.TryParse(trigfixture.Source, out FixtureStartTriggerType sttype)) // Start query state, to make it trigged
                    {
                        switch (sttype)
                        {
                            case FixtureStartTriggerType.Door_Closing:
                            case FixtureStartTriggerType.Door_Closed:
                                DutSnUpdated += LoopOnDoorClosed;

                                //for (int i = 0; i < SocketCount; i++)
                                //{
                                //    FixtureTriggerTimers[i].Interval = 20;
                                //    FixtureTriggerTimers[i].Elapsed += FixtureTriggerTimer_DoorClosed_Elapsed;
                                //}
                                break;

                            case FixtureStartTriggerType.Dut_Ready:
                                DutSnUpdated += LoopOnDutReady;
                                //for (int i = 0; i < SocketCount; i++)
                                //{
                                //    FixtureTriggerTimers[i].Elapsed += FixtureTriggerTimer_DutReady_Elapsed;
                                //}
                                break;
                        }
                    }

                    if (Script.SystemConfig.General.SocketCount == 1)
                    {
                        switch (sttype)
                        {
                            case FixtureStartTriggerType.Door_Closing:
                            case FixtureStartTriggerType.Door_Closed:
                                Exec.OnPostUUTed += PostUUTedWithSnQueue_DoorClosed;
                                break;

                            case FixtureStartTriggerType.Dut_Ready:
                                Exec.OnPostUUTed += PostUUTedWithSnQueue_DutReady;
                                break;
                        }
                    }
                }
                else
                {
                    // Fixture Trigger, which means when test started, the fixture is supposed to be ready
                    // execution.OnPreUUTed += Fixture_OnPreUUTed;

                    if (Script.SystemConfig.General.SocketCount == 1)
                    {
                        Script.HardwareConfig.StartTrigger.StartTrigged = StartTriggedWithSnQueue;
                    }
                    else
                    {
                        Script.HardwareConfig.StartTrigger.StartTrigged = StartTrigged;
                    }
                }

                Script.HardwareConfig.StartTrigger.Initialize();

                //if (Script.SystemConfig.General.SocketCount == 1)
                //{
                //    Exec.OnPostUUTed += HandleSnInQueue;   // the new test should after the PostUUT, not when test completed, for there is post process depends on the sn
                //}
            }
        }

        //public void ApplySystemSetting()
        //{
        //    if (Script.SystemConfig is null)
        //    {
        //    }
        //    else
        //    {
        //        if (Script.SystemConfig.SFCs.EnableSfc)
        //        {
        //            Mes = MesManager.GetMesInstance(Script.SystemConfig.Station.Location, Script.SystemConfig.Station.Vendor);

        //            if (Mes is null)
        //            {
        //                Warn($"No Mes found for {Script.SystemConfig.Station.Vendor}, disable MES");
        //                foreach (var rs in Exec.Results)
        //                {
        //                    rs.IsSFC = false;
        //                }
        //            }
        //            else
        //            {
        //                string data = string.Empty;
        //                if (Script.SystemConfig.SFCs.SfcsUploadData)
        //                {
        //                    if (Script.SystemConfig.SFCs.SfcsDataMode.Equals("JDM", StringComparison.OrdinalIgnoreCase))
        //                    {
        //                        Exec.Template.GenerateSfcHeader_JDM(out data);
        //                    }
        //                    else
        //                    {
        //                        Exec.Template.GenerateSFCHeader(out data);
        //                    }
        //                }

        //                Mes.Initialize(Script.SystemConfig.SFCs, data);
        //            }
        //        }

        //        // This should be before the normal Execution_OnTestCompleted, for it will update the Test Status
        //        if (Script.Spec.Secondary != null)
        //        {
        //            Exec.OnTestCompleted += VerifySecondarySpec;
        //        }

        //        Exec.OnTestCompleted += Execution_OnTestCompleted;

        //        if (Script.GoldenSampleSpec != null)
        //        {
        //            if (EngineUtilities.CheckContainLimit(Exec.Template.StepDatas, Script.GoldenSampleSpec.Limit))
        //            {
        //                Exec.OnTestCompleted += PickGoldenSample;
        //            }
        //            else
        //            {
        //                throw new InvalidDataException("Golden Sample does not match current spec. Please check");
        //            }
        //        }

        //        if (Script.SystemConfig.General.CustomizeInputSn)
        //        {
        //            if (string.IsNullOrEmpty(Script.SystemConfig.SFCs.GetDsnApi))
        //            {
        //                Exec.OnUutIdentified += Execution_OnUutIdentified;
        //            }
        //            else
        //            {
        //                Exec.OnUutIdentified += Execution_OnUutIdentified_PalletSn;
        //            }
        //        }
        //    }
        //}

        public bool IsStarted = false;
        public int Start()
        {
            if(!IsStarted)
            {
                var rtn = Exec.Start();
                IsStarted = true;
                return rtn;
            }
            else
            {
                return 1;
            }
        }

        public int StartNewTest(int slotIndex = 0)
        {
            SlotIndex = SlotIndex;
            return Exec.StartNewTest(slotIndex);
        }


        public int Stop()
        {
            if(IsStarted)
            {
                var rtn = Exec.Stop();
                IsStarted = false;
                return rtn;
            }
            else
            { return 1; }
        }

        public int Terminate()
        {
            return Exec.Terminate();
        }

        public int Abort()
        {
            return Exec.Abort();
        }

        public IScript GetScript()
        {
            return Exec.GetScript();
        }

        public void ShowVariables(int slot)
        {
            Exec.ShowVariables(slot);
        }

        public int EnableSlot(int slotindex, bool status = true)
        {
            return Exec.EnableSlot(slotindex, status);
        }

        public void Break()
        {
            Exec.Break();
        }

        public void Resume()
        {
            Exec.Resume();
        }

        public void StepIn()
        {
            Exec.StepIn();
        }

        public void StepOver()
        {
            Exec.StepOver();
        }

        public void StepOut()
        {
            Exec.StepOut();
        }

        public void Dispose()
        {
            Exec.Dispose();
            RemoveHardwareSetting();
        }

        bool IsGoldenSampleHandlerRegistered = false;
        public int SwitchExecutionMode(ExecutionMode mode)
        {
            var rtn = Exec.SwitchExecutionMode(mode);

            if (Script.HardwareConfig?.Fixture is ILockableFixture ilf)
            {
                ilf.SetLockState(SlotIndex, false);
            }

            if (mode == ExecutionMode.Reference || mode == ExecutionMode.Verification)
            {
                if (Script.GoldenSamples.Count > 0 && !IsGoldenSampleHandlerRegistered)
                {
                    OnUutIdentified += ValidateGoldenSamples;
                    IsGoldenSampleHandlerRegistered = true;
                }

                if (Script.HardwareConfig?.Fixture is ILockableFixture)
                {
                    OnUutIdentified += UnlockFixture;
                }
            }
            else
            {
                if(IsGoldenSampleHandlerRegistered)
                {
                    OnUutIdentified -= ValidateGoldenSamples;
                    IsGoldenSampleHandlerRegistered = false;
                }

                if (Script.HardwareConfig?.Fixture is ILockableFixture)
                {
                    OnUutIdentified -= UnlockFixture;
                }
            }

            return rtn;    
        }

        private void UnlockFixture(object sender, TF_Result e)
        {
            if (Script.HardwareConfig?.Fixture is ILockableFixture ilf)
            {
                ilf.SetLockState(SlotIndex, false);
            }
        }

        private void ValidateGoldenSamples(object sender, TF_Result e)
        {
            if (!Script.GoldenSamples.Contains(e.SerialNumber))
            {
                var msg = $"{e.SerialNumber} is not in Golden Sample List {string.Join(",", Script.GoldenSamples)}";
                e.ErrorMessage = new ErrorMsg((int)ErrorCode.InvalidOperation, msg);
                e.Status = TF_TestStatus.ERROR;
                //throw new InvalidOperationException(msg);
            }
        }

        public object GetVariable(string name)
        {
            throw new NotImplementedException();
        }

        public void SetVariable(string name, object value)
        {
            throw new NotImplementedException();
        }
    }
}
