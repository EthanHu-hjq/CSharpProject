using ApEngine.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestCore;
using TestCore.Base;
using ToucanCore.Abstraction.Engine;

namespace ApEngine.UIs
{
    /// <summary>
    /// Interaction logic for SequenceCalibration.xaml
    /// </summary>
    public partial class ScriptCalibration : Window
    {
        public static ApxEngine Engine { get; set; }

        public int ValidDay
        {
            get { return (int)GetValue(ValidDayProperty); }
            set { SetValue(ValidDayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidDay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidDayProperty =
            DependencyProperty.Register("ValidDay", typeof(int), typeof(ScriptCalibration), new PropertyMetadata(0));

        public int ValidHour
        {
            get { return (int)GetValue(ValidHourProperty); }
            set { SetValue(ValidHourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidHour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidHourProperty =
            DependencyProperty.Register("ValidHour", typeof(int), typeof(ScriptCalibration), new PropertyMetadata(0));

        public int WarnDay
        {
            get { return (int)GetValue(WarnDayProperty); }
            set { SetValue(WarnDayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarnDay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarnDayProperty =
            DependencyProperty.Register("WarnDay", typeof(int), typeof(ScriptCalibration), new PropertyMetadata(0));

        public int WarnHour
        {
            get { return (int)GetValue(WarnHourProperty); }
            set { SetValue(WarnHourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarnHour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarnHourProperty =
            DependencyProperty.Register("WarnHour", typeof(int), typeof(ScriptCalibration), new PropertyMetadata(0));

        public Script Script { get; }

        public DelegateCommand LoadScriptCalibData { get; }
        public DelegateCommand NewScriptCalibData { get; }
        public DelegateCommand SaveScriptCalibData { get; }
        public DelegateCommand SaveScriptCalibDataAs { get; }

        public ScriptCalibData ScriptCalibData { get; private set; }
        public ScriptCalibration()
        {
            LoadScriptCalibData = new DelegateCommand(cmd_LoadScriptCalibData);
            NewScriptCalibData = new DelegateCommand(cmd_NewScriptCalibData);
            SaveScriptCalibData = new DelegateCommand(cmd_SaveScriptCalibData);
            SaveScriptCalibDataAs = new DelegateCommand(cmd_SaveScriptCalibDataAs);

            InitializeComponent();
            DataContextChanged += ScriptCalibration_DataContextChanged;
        }

        public ScriptCalibration(Script script) : this()
        {
            Script = script;

            string scdpath = $"{Script.GetCalibrationBase()}{ScriptCalibData.FileExt}";

            if(File.Exists(scdpath))
            {
                LoadScriptCalibData.Execute(scdpath);
            }
            else
            {
                NewScriptCalibData.Execute(null);
            }
        }

        private void cmd_SaveScriptCalibDataAs(object obj)
        {
            if (DataContext is ScriptCalibData scd) { }
            else
            {
                return;
            }

            if (obj is string path)
            {
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = $"PTE Script Calibration Data|*{ScriptCalibData.FileExt}";

                if (saveFileDialog.ShowDialog() != true) return;
                path = saveFileDialog.FileName;
            }

            scd.Export(path);
        }

        private void cmd_SaveScriptCalibData(object obj)
        {
            if (File.Exists(ScriptCalibData?.FilePath))
            {
                ScriptCalibData.Save(ScriptCalibData.FilePath);
                DialogResult = true;
            }
            else if (Script != null)
            {
                var targetpath = $"{Script.GetCalibrationBase()}{ScriptCalibData.FileExt}";

                ScriptCalibData.Save(targetpath);
                DialogResult = true;
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = $"PTE Script Calib Data|*{ScriptCalibData.FileExt}";
                sfd.Title = "Save Script Calib Data As...";
                if (sfd.ShowDialog() == true)
                {
                    ScriptCalibData.Save(sfd.FileName);
                    DialogResult = true;
                }
                else
                {
                    return;
                }
            }

            MessageBox.Show("Script Calib Data Saved");
        }

        private void cmd_NewScriptCalibData(object obj)
        {
            DataContext = ScriptCalibData = Script.AnalyzeCalibration();
        }

        private void cmd_LoadScriptCalibData(object obj)
        {
            string path = null;
            if(obj is string str)
            {
                path = str;
            }
            else
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = ScriptCalibData.FileExt;
                ofd.Filter = $"PTE Script Calibration Data|*{ScriptCalibData.FileExt}";
                ofd.Title = "Select the Apx Calibration File";
                if (ofd.ShowDialog() == true)
                {
                    path = ofd.FileName;
                }
            }
            
            if(File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    try
                    {
                        DataContext = ScriptCalibData = ScriptCalibData.Load(path);
                    }
                    catch
                    {
                        MessageBox.Show($"Load Script Data {path} Failed");
                    }
                }
            }
        }

        private void ScriptCalibration_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ScriptCalibData calib)
            {
                ValidDay = calib.ValidTime.Days;
                ValidHour = calib.ValidTime.Hours;
                WarnDay = calib.WarnTime.Days;
                WarnHour = calib.WarnTime.Hours;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tab)
            {
                if (tab.SelectedItem is KeyValuePair<string, SignalPathCalibData> spcd)
                {
                    if (ApxEngine.ApRef.ActiveSignalPathName != spcd.Key)
                    {
                        var sp = ApxEngine.ApRef.Sequence.GetSignalPath(spcd.Key);
                        sp.GetMeasurement(0).Show();
                    }
                }
            }
        }
    }
}
