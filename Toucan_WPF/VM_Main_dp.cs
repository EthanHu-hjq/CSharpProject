using MahApps.Metro.Controls.Dialogs;
using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TestCore;
using Toucan_WPF.ViewModels;
using ToucanCore.Abstraction.Engine;
using TsEngine.UIs;

namespace Toucan_WPF
{
    public partial class VM_Main
    {
        #region UserInfo
        public bool IsTester
        {
            get
            {
                return (bool)GetValue(IsTesterProperty);
            }
            set { SetValue(IsTesterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsTester.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsTesterProperty =
            DependencyProperty.Register("IsTester", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));



        public bool IsLineLeader
        {
            get { return (bool)GetValue(IsLineLeaderProperty); }
            set { SetValue(IsLineLeaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsLineLeader.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsLineLeaderProperty =
            DependencyProperty.Register("IsLineLeader", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));



        public bool IsMaintainer
        {
            get { return (bool)GetValue(IsMaintainerProperty); }
            set { SetValue(IsMaintainerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMaintainer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMaintainerProperty =
            DependencyProperty.Register("IsMaintainer", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));

        public bool IsEngineer
        {
            get { return (bool)GetValue(IsEngineerProperty); }
            set { SetValue(IsEngineerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEngineer.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEngineerProperty =
            DependencyProperty.Register("IsEngineer", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));

        public bool IsAdmin
        {
            get { return (bool)GetValue(IsAdminProperty); }
            set { SetValue(IsAdminProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsAdmin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsAdminProperty =
            DependencyProperty.Register("IsAdmin", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));



        public string UserName
        {
            get { return (string)GetValue(UserNameProperty); }
            set { SetValue(UserNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UserName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register("UserName", typeof(string), typeof(VM_Main), new PropertyMetadata("User"));

        public AuthType CurrentAuthType
        {
            get { return (AuthType)GetValue(CurrentAuthTypeProperty); }
            set { SetValue(CurrentAuthTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentAuthTypeProperty =
            DependencyProperty.Register("CurrentAuthType", typeof(AuthType), typeof(VM_Main), new PropertyMetadata(AuthType.Anonymous));
        #endregion

        #region Test

        public static IEngine Engine { get; set; }

        public ObservableCollection<IScript> Scripts
        {
            get { return (ObservableCollection<IScript>)GetValue(ScriptsProperty); }
            set { SetValue(ScriptsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Scripts.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScriptsProperty =
            DependencyProperty.Register("Scripts", typeof(ObservableCollection<IScript>), typeof(VM_Main), new PropertyMetadata(new ObservableCollection<IScript>()));

        public ToucanCore.Engine.Script ActiveScript
        {
            get { return (ToucanCore.Engine.Script)GetValue(ActiveScriptProperty); }
            set { SetValue(ActiveScriptProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveScript.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveScriptProperty =
            DependencyProperty.Register("ActiveScript", typeof(ToucanCore.Engine.Script), typeof(VM_Main), new PropertyMetadata(null));

        public ObservableCollection<VM_Execution> Executions
        {
            get { return (ObservableCollection<VM_Execution>)GetValue(ExecutionsProperty); }
            set { SetValue(ExecutionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Executions.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExecutionsProperty =
            DependencyProperty.Register("Executions", typeof(ObservableCollection<VM_Execution>), typeof(VM_Main), new PropertyMetadata(new ObservableCollection<VM_Execution>()));

        public VM_Execution ActiveExecution
        {
            get { return (VM_Execution)GetValue(ActiveExecutionProperty); }
            set { SetValue(ActiveExecutionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Execution.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveExecutionProperty =
            DependencyProperty.Register("ActiveExecution", typeof(VM_Execution), typeof(VM_Main), new PropertyMetadata(null));

        public string InputText
        {
            get { return (string)GetValue(InputTextProperty); }
            set { SetValue(InputTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputTextProperty =
            DependencyProperty.Register("InputText", typeof(string), typeof(VM_Main), new PropertyMetadata(null));


        public bool EnableInputText
        {
            get { return (bool)GetValue(EnableInputTextProperty); }
            set { SetValue(EnableInputTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableInputText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableInputTextProperty =
            DependencyProperty.Register("EnableInputText", typeof(bool), typeof(VM_Main), new PropertyMetadata(true));


        public string PreviousSn1
        {
            get { return (string)GetValue(PreviousSn1Property); }
            set { SetValue(PreviousSn1Property, value); }
        }

        // Using a DependencyProperty as the backing store for PreviewSN1.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviousSn1Property =
            DependencyProperty.Register("PreviousSn1", typeof(string), typeof(VM_Main), new PropertyMetadata("Previous1"));

        public string PreviousSn2
        {
            get { return (string)GetValue(PreviousSn2Property); }
            set { SetValue(PreviousSn2Property, value); }
        }

        // Using a DependencyProperty as the backing store for PreviewSn2.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviousSn2Property =
            DependencyProperty.Register("PreviousSn2", typeof(string), typeof(VM_Main), new PropertyMetadata("Previous2"));

        public string PreviousSn3
        {
            get { return (string)GetValue(PreviousSn3Property); }
            set { SetValue(PreviousSn3Property, value); }
        }

        // Using a DependencyProperty as the backing store for PreviousSn3.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviousSn3Property =
            DependencyProperty.Register("PreviousSn3", typeof(string), typeof(VM_Main), new PropertyMetadata("Previous3"));

        public string PreviousSn4
        {
            get { return (string)GetValue(PreviousSn4Property); }
            set { SetValue(PreviousSn4Property, value); }
        }

        // Using a DependencyProperty as the backing store for PreviousSn4.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviousSn4Property =
            DependencyProperty.Register("PreviousSn4", typeof(string), typeof(VM_Main), new PropertyMetadata("Previous4"));





        public bool EngineUiVisible
        {
            get { return (bool)GetValue(EngineUiVisibleProperty); }
            set { SetValue(EngineUiVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EngineUiVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EngineUiVisibleProperty =
            DependencyProperty.Register("EngineUiVisible", typeof(bool), typeof(VM_Main), new PropertyMetadata(false, EngineVisibleChanged));

        private static void EngineVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VM_Main vm)
            {
                //using (ExecutionUI ui = new ExecutionUI())
                //{
                //    ui.ShowDialog();
                //}

                if (Engine is null) return;

                if(!Engine.UiVisible)
                {
                    Engine.IsEditMode = vm.IsMaintainer;
                }

                if (Engine.IsInitialized)
                {
                    Engine.UiVisible = (bool)e.NewValue;
                }
                else
                {
                    if (vm.EngineUiVisible != Engine.UiVisible)
                    {
                        vm.EngineUiVisible = Engine.UiVisible;
                    }
                }
            }
        }

        public string EngineVersion
        {
            get { return (string)GetValue(EngineVersionProperty); }
            set { SetValue(EngineVersionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EngineVersion.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EngineVersionProperty =
            DependencyProperty.Register("EngineVersion", typeof(string), typeof(VM_Main), new PropertyMetadata(string.Empty));

        public bool IsReference
        {
            get { return (bool)GetValue(IsReferenceProperty); }
            set { SetValue(IsReferenceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsReference.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsReferenceProperty =
            DependencyProperty.Register("IsReference", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));

        public bool IsVerification
        {
            get { return (bool)GetValue(IsVerificationProperty); }
            set { SetValue(IsVerificationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsVerification.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsVerificationProperty =
            DependencyProperty.Register("IsVerification", typeof(bool), typeof(VM_Main), new PropertyMetadata(false));
        #endregion



        public int PassCnt
        {
            get { return (int)GetValue(PassCntProperty); }
            set { SetValue(PassCntProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PassCnt.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PassCntProperty =
            DependencyProperty.Register("PassCnt", typeof(int), typeof(VM_Main), new PropertyMetadata(0));

        public int FailCnt
        {
            get { return (int)GetValue(FailCntProperty); }
            set { SetValue(FailCntProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FailCnt.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FailCntProperty =
            DependencyProperty.Register("FailCnt", typeof(int), typeof(VM_Main), new PropertyMetadata(0));

        public int TotalCnt
        {
            get { return (int)GetValue(TotalCntProperty); }
            set { SetValue(TotalCntProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TotalCnt.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TotalCntProperty =
            DependencyProperty.Register("TotalCnt", typeof(int), typeof(VM_Main), new PropertyMetadata(0));

        public double Yield
        {
            get { return (double)GetValue(YieldProperty); }
            set { SetValue(YieldProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Yield.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YieldProperty =
            DependencyProperty.Register("Yield", typeof(double), typeof(VM_Main), new PropertyMetadata(double.NaN));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(VM_Main), new PropertyMetadata(null));

        public string WarningMessage
        {
            get { return (string)GetValue(WarningMessageProperty); }
            set { SetValue(WarningMessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarningMessage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarningMessageProperty =
            DependencyProperty.Register("WarningMessage", typeof(string), typeof(VM_Main), new PropertyMetadata(null));

        public bool AttachResultInChart
        {
            get { return (bool)GetValue(AttachResultInChartProperty); }
            set { SetValue(AttachResultInChartProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AttachResultInChart.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AttachResultInChartProperty =
            DependencyProperty.Register("AttachResultInChart", typeof(bool), typeof(VM_Main), new PropertyMetadata(VM_Slot.AttachResultInChart, AttachResultInChartChanged));

        private static void AttachResultInChartChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool newbool)
            {
                VM_Slot.AttachResultInChart = newbool;
            }
        }

        //public bool EnableCsvReport
        //{
        //    get { return (bool)GetValue(EnableCsvReportProperty); }
        //    set { SetValue(EnableCsvReportProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for EnableCsvReport.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty EnableCsvReportProperty =
        //    DependencyProperty.Register("EnableCsvReport", typeof(bool), typeof(VM_Main), new PropertyMetadata(false, EnableCsvReportChanged));

        //private static void EnableCsvReportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if(d is VM_Main vm)
        //    {
        //        if (VM_Main.Engine is null && vm.EnableCsvReport == true)
        //        {
        //        }
        //    }
        //}

        public bool EnableCsvReport { get; set; }
    }
}
