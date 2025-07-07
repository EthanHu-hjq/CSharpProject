using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestCore;
using TestCore.Configuration;
using TestCore.Data;
using Toucan_WPF.Ctrls;
using Toucan_WPF.UIs;
using ToucanCore.Engine;
using System.Threading;
using System.Windows.Shapes;
using ToucanCore;
using ToucanCore.UIs;
using MahApps.Metro.Controls.Dialogs;
using ToucanCore.HAL;
using Mes;
using TsEngine;
using ToucanCore.Configuration;
using TestCore.Services;
using System.Windows.Markup;
using ToucanCore.Driver;
using TestCore.Abstraction.Process;
using ToucanCore.Misc;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.HAL;
using ToucanCore.Abstraction.Engine;
using ControlzEx.Standard;


namespace Toucan_WPF.ViewModels
{
    public class VM_Execution : DependencyObject
    {
        public string Name { get; } = Guid.NewGuid().ToString();

        #region Property

        public TF_Result Template
        {
            get { return (TF_Result)GetValue(TemplateProperty); }
            set { SetValue(TemplateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Template.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.Register("Template", typeof(TF_Result), typeof(VM_Execution), new PropertyMetadata(null));

        public string Customer
        {
            get { return (string)GetValue(CustomerProperty); }
            set { SetValue(CustomerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Customer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CustomerProperty =
            DependencyProperty.Register("Customer", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        public string Project
        {
            get { return (string)GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Project.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register("Project", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        public string Product
        {
            get { return (string)GetValue(ProductProperty); }
            set { SetValue(ProductProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Product.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProductProperty =
            DependencyProperty.Register("Product", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        public string Station
        {
            get { return (string)GetValue(StationProperty); }
            set { SetValue(StationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationProperty =
            DependencyProperty.Register("Station", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        public string Location
        {
            get { return (string)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register("Location", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        public string StationId
        {
            get { return (string)GetValue(StationIdProperty); }
            set { SetValue(StationIdProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StationId.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationIdProperty =
            DependencyProperty.Register("StationId", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        public int SlotColumns
        {
            get { return (int)GetValue(SlotColumnsProperty); }
            set { SetValue(SlotColumnsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlotColumns.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotColumnsProperty =
            DependencyProperty.Register("SlotColumns", typeof(int), typeof(VM_Execution), new PropertyMetadata(1));

        public int SlotRows
        {
            get { return (int)GetValue(SlotRowsProperty); }
            set { SetValue(SlotRowsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlotRows.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotRowsProperty =
            DependencyProperty.Register("SlotRows", typeof(int), typeof(VM_Execution), new PropertyMetadata(1));

        public int ActiveSlotIndex
        {
            get { return (int)GetValue(ActiveSlotIndexProperty); }
            set { SetValue(ActiveSlotIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveSlotIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveSlotIndexProperty =
            DependencyProperty.Register("ActiveSlotIndex", typeof(int), typeof(VM_Execution), new PropertyMetadata(-1));

        public bool EnableUserInteraction
        {
            get { return (bool)GetValue(EnableUserInteractionProperty); }
            set { SetValue(EnableUserInteractionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableUserInteraction.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableUserInteractionProperty =
            DependencyProperty.Register("EnableUserInteraction", typeof(bool), typeof(VM_Execution), new PropertyMetadata(false));

        public string WaterPrint
        {
            get { return (string)GetValue(WaterPrintProperty); }
            set { SetValue(WaterPrintProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WaterPrint.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WaterPrintProperty =
            DependencyProperty.Register("WaterPrint", typeof(string), typeof(VM_Execution), new PropertyMetadata());

        public string WarningMessage
        {
            get { return (string)GetValue(WarningMessageProperty); }
            set { SetValue(WarningMessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarningMessage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarningMessageProperty =
            DependencyProperty.Register("WarningMessage", typeof(string), typeof(VM_Execution), new PropertyMetadata(null));

        #endregion

        public ToucanCore.Engine.Execution Execution { get;  }
        public string Workbase { get; private set; }

        public string ReferenceBase { get; private set; }
        public string CalibrationBase { get; private set; }

        public VM_Slot[] Slots { get; private set; }  // Might update from script

        public DelegateCommand ShowVariables { get; }

        public DelegateCommand StartNewTest { get; }

        public DelegateCommand Start { get; }
        public DelegateCommand Stop { get; }

        /// <summary>
        /// Reference is get the reference data, which is based on logic stations
        /// </summary>
        public DelegateCommand RunReference { get; }
        /// <summary>
        /// Calibration is get the calibration data, which is based on physical equipments
        /// </summary>
        public DelegateCommand RunCalibration { get; }

        public DelegateCommand ActivateSlot { get; }

        public DelegateCommand UpdateDutSn { get; }
        public DelegateCommand SwitchNormalMode { get; }
        public DelegateCommand SwitchReferenceMode { get; }
        public DelegateCommand SwitchVerificationMode { get; }

        public ExecutionMode ExecutionMode => Execution.ExecutionMode;
        //public StringBuilder WarningMessage { get; } = new StringBuilder();
        //public bool PromptWarning { get; set; }

        public void UpdateUI(GlobalConfiguration sc)
        {
            if (sc.Station.IsValid)
            {
                Customer = sc.Station?.CustomerName;
                Project = sc.Station?.ProjectName;
                Product = sc.Station?.ProductName;
                Station = sc.Station?.StationName;

                if (sc.Station?.Location == TestCore.Location.Vendor)
                {
                    Location = sc.Station?.Vendor;
                }
                else
                {
                    Location = sc.Station?.Location.ToString();
                }

                StationId = sc.Station?.StationID;

                Execution.Template.StationConfig = sc.Station;
            }
        }

        public IMes Mes => Execution?.Mes;
        public ToucanCore.Engine.Script Script { get; }
        public VM_Main Parent { get; set; }

        public event EventHandler Initialized;

        public VM_Execution(IExecution execution, ToucanCore.Engine.Script script)
        {
            StartNewTest = new DelegateCommand(cmd_StartNewTest);
            UpdateDutSn = new DelegateCommand(cmd_UpdateDutSn);     // Active update the serialnumber

            Start = new DelegateCommand(cmd_Start);
            Stop = new DelegateCommand(cmd_Stop);
            
            ShowVariables = new DelegateCommand(cmd_ShowVariables);
            ActivateSlot = new DelegateCommand(cmd_ActivateSlot);

            SwitchNormalMode = new DelegateCommand(cmd_SwitchNormalMode);
            SwitchReferenceMode = new DelegateCommand(cmd_SwitchReferenceMode);
            SwitchVerificationMode = new DelegateCommand(cmd_SwitchVerificationMode);

            Script = script; // new ToucanCore.Engine.Script(execution.GetScript());
            Execution = new ToucanCore.Engine.Execution(execution, Script);

            Template = execution.Template; // Engine, TODO
            Workbase = Script.BaseDirectory;

            Execution.DutSnUpdated += Execution_DutSnUpdated;

            Customer = Template.StationConfig?.CustomerName;
            Project = Template.StationConfig?.ProjectName;
            Product = Template.StationConfig?.ProductName;
            Station = Template.StationConfig?.StationName;

            StationId = Template.StationConfig?.StationID;

            if (Template.StationConfig?.Location == TestCore.Location.Vendor)
            {
                Location = Template.StationConfig?.Vendor;
            }
            else
            {
                Location = Template.StationConfig?.Location.ToString();
            }

            if (Script.SystemConfig is null)
            {
                EnableUserInteraction = true;
            }
            else
            {
                EnableUserInteraction = !Script.SystemConfig.General.CustomizeInputSn;// TODO. Process for CustomizeInputSn
            }

            if (Script.HardwareConfig?.Fixture != null)
            {
                if (Script.HardwareConfig.Fixture.AutoDutIn)
                {
                    if (Script.HardwareConfig.Fixture.AutoDutOut)
                    {
                        WarningMessage = Application.Current.FindResource("Warn_AutoDutInOutEnabled").ToString();
                    }
                    else
                    {
                        WarningMessage = Application.Current.FindResource("Warn_AutoDutInEnabled").ToString();
                    }
                }
                else
                {
                    if (Script.HardwareConfig.Fixture.AutoDutIn)
                    {
                        WarningMessage = Application.Current.FindResource("Warn_AutoDutOutEnabled").ToString();
                    }
                    else
                    {
                        WarningMessage = null;
                    }
                }
            }
        }

        public void Initialize()
        {
            VM_Slot.ShowFailureChart = Execution.Exec.SocketCount <= 4;
            Slots = new VM_Slot[Execution.Exec.SocketCount];

            SlotUiConfig slotuiconfig = SlotUiConfig.GetStationUiConfig(Execution.Template.StationConfig);

            for (int i = 0; i < Slots.Length; i++)
            {
                Slots[i] = new VM_Slot(Execution.Results[i], slotuiconfig) { SlotIndex = i, SlotName = $"{i}#" };
                Slots[i].Execution = Execution;
                Slots[i].Result.TestStart += ResultStarted;
            }

            if (Execution.Exec.SocketCount == 1)
            {
                ActivateSlot.Execute(0);
            }

            Initialized?.Invoke(this, null);
        }

        // For clear the Next SN when sn in Testing
        private void ResultStarted(object sender, EventArgs args)
        {
            Dispatcher.Invoke(() =>
            {
                if(sender is TF_Result rs)
                {
                    Slots[rs.SocketIndex].SerialNumberNext = Execution.DutSnWaitForTest[rs.SocketIndex];
                }
            });
        }

        private void Execution_DutSnUpdated(object sender, DutMessage e)
        {
            Dispatcher.Invoke(() =>
            {
                Slots[e.SocketIndex].SerialNumberNext = Execution.DutSnWaitForTest[e.SocketIndex];
            });
        }

        private void cmd_SwitchNormalMode(object sender)
        {
            try
            {
                Execution.SwitchExecutionMode(ExecutionMode.Normal);
                WaterPrint = string.Empty;
                for (int i = 0; i < Execution.SocketCount; i++)
                {
                    Slots[i].Result = Execution.Results[i];
                }
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", $"Switch to {ExecutionMode.Normal} Failed. Err {ex}");
            }
        }

        private void cmd_SwitchReferenceMode(object sender)
        {
            try
            {
                Execution.SwitchExecutionMode(ExecutionMode.Reference);
                WaterPrint = "Reference";
                for (int i = 0; i < Execution.SocketCount; i++)
                {
                    Slots[i].Result = Execution.Results[i];
                }
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", $"Switch to {ExecutionMode.Reference} Failed. Err {ex}");
            }
        }

        private void cmd_SwitchVerificationMode(object sender)
        {
            try
            { 
                Execution.SwitchExecutionMode(ExecutionMode.Verification);
                WaterPrint = "Verification";
                for (int i = 0; i < Execution.SocketCount; i++)
                {
                    Slots[i].Result = Execution.Results[i];
                }
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", $"Switch to {ExecutionMode.Verification} Failed. Err {ex}");
            }
        }

        private void cmd_ActivateSlot(object obj)
        {
            if(obj is int idx)
            {
                for (int i = 0; i < Slots.Length; i++)
                {
                    Slots[i].IsActive = false;
                }

                if (idx >= 0 && idx < Slots.Length)
                {
                    Slots[idx].IsActive = true;
                }

                ActiveSlotIndex = Execution.SlotIndex = idx;
            }
        }

        /// <summary>
        /// for the TestStand variable 
        /// </summary>
        /// <param name="obj"></param>
        private void cmd_ShowVariables(object obj)
        {
            if (ActiveSlotIndex >= 0)
            {
                Execution.Exec.ShowVariables(ActiveSlotIndex);
            }
            else
            {
                MessageBox.Show("No Active Slot", "Warning");
            }
        }


        /// <summary>
        /// if obj is null. clean up the sn, including the sn in queue
        /// </summary>
        /// <param name="obj"></param>
        private void cmd_UpdateDutSn(object obj)
        {
            try
            {
                if (obj is string sn)
                {
                    if (ActiveSlotIndex >= 0)
                    {
                        Execution.UpdateDutSn(ActiveSlotIndex, sn);
                        //Slots[ActiveSlotIndex].SerialNumber = Execution.Results[ActiveSlotIndex].SerialNumber;
                    }
                }
                else if (obj is Tuple<int, string> t_sn)
                {
                    ActiveSlotIndex = Execution.SlotIndex = t_sn.Item1;
                    sn = t_sn.Item2;

                    Execution.UpdateDutSn(ActiveSlotIndex, sn);
                    //Slots[ActiveSlotIndex].SerialNumber = Execution.Results[ActiveSlotIndex].SerialNumber;
                }

                Slots[ActiveSlotIndex].SerialNumberNext = Execution.DutSnWaitForTest[ActiveSlotIndex];
            }
            catch (Exception ex)
            {
                DialogCoordinator.Instance.ShowModalMessageExternal(this, "Error", $"Update DUT SN Denied\r\n{ex.Message}");
                
                this.UILog(ex);
            }
        }

        private void cmd_StartNewTest(object obj)
        {
            try
            {
                if (obj is string sn)
                {
                    if (ActiveSlotIndex >= 0)
                    {
                        if (Execution.Exec.Template.GeneralConfig.AutoCapitalSn)
                        {
                            sn = sn.ToUpper();
                        }

                        Slots[ActiveSlotIndex].SerialNumber = sn;
                        Execution.Exec.StartNewTest(ActiveSlotIndex);
                    }
                }
                else if (obj is Tuple<int, string> t_sn)
                {
                    ActiveSlotIndex = Execution.SlotIndex = t_sn.Item1;
                    sn = t_sn.Item2;
                    if (Execution.Exec.Template.GeneralConfig.AutoCapitalSn)
                    {
                        sn = sn.ToUpper();
                    }

                    Slots[ActiveSlotIndex].SerialNumber = sn;
                    Execution.Exec.StartNewTest(ActiveSlotIndex);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Start New Test Failed\r\n{ex}", "Error");
            }
        }

        private void cmd_Start(object obj)
        {
            if (Execution?.IsStarted == false)
            {
                if (Execution.Start() > 0)
                {
                    foreach (var slot in Slots)
                    {
                        slot.Start.Execute(null);
                    }
                }
            }
        }

        private void cmd_Stop(object obj)
        {
            foreach(var slot in Slots)
            {
                slot.Stop.Execute(null);
            }

            Execution.Stop();
        }
    }
}
