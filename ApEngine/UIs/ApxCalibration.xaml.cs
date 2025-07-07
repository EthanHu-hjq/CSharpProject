using ApEngine.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml;
using TestCore.Base;
using TestCore;
using Microsoft.Win32;

namespace ApEngine.UIs
{
    /// <summary>
    /// Interaction logic for ApxCalibration.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for Calibration.xaml
    /// </summary>
    public partial class ApxCalibration : Window
    {
        //public string[] LoudspeakerAvailableChannels { get => LoudspeakerProductionTestCalibData.AvailableChannels; }
        //public string[] LoudspeakerAvailableConfigs { get => LoudspeakerProductionTestCalibData.AvailableConfigs; }

        public static Array InputConnectorTypes = Enum.GetValues(typeof(AudioPrecision.API.InputConnectorType));
        public static Array OutputConnectorTypes = Enum.GetValues(typeof(AudioPrecision.API.OutputConnectorType));

        // Using a DependencyProperty as the backing store for EqSn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EqSnProperty =
            DependencyProperty.Register("EqSn", typeof(string), typeof(ApxCalibration), new PropertyMetadata(string.Empty));

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Version.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(ApxCalibration), new PropertyMetadata("0.1"));

        public ObservableCollection<Calib_AcousticInput> AcousticInputCalibDatas
        {
            get { return (ObservableCollection<Calib_AcousticInput>)GetValue(AcousticInputCalibDatasProperty); }
            set { SetValue(AcousticInputCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AcousticInputCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AcousticInputCalibDatasProperty =
            DependencyProperty.Register("AcousticInputCalibDatas", typeof(ObservableCollection<Calib_AcousticInput>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<Calib_AcousticInput>()));

        public ObservableCollection<Calib_AnalogInput> AnalogInputCalibDatas
        {
            get { return (ObservableCollection<Calib_AnalogInput>)GetValue(AnalogInputCalibDatasProperty); }
            set { SetValue(AnalogInputCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnalogInputCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnalogInputCalibDatasProperty =
            DependencyProperty.Register("AnalogInputCalibDatas", typeof(ObservableCollection<Calib_AnalogInput>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<Calib_AnalogInput>()));

        public ObservableCollection<Calib_AcousticOutput> AcousticOutputCalibDatas
        {
            get { return (ObservableCollection<Calib_AcousticOutput>)GetValue(AcousticOutputCalibDatasProperty); }
            set { SetValue(AcousticOutputCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AcousticOutputCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AcousticOutputCalibDatasProperty =
            DependencyProperty.Register("AcousticOutputCalibDatas", typeof(ObservableCollection<Calib_AcousticOutput>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<Calib_AcousticOutput>()));

        public ObservableCollection<Calib_AnalogOutput> AnalogOutputCalibDatas
        {
            get { return (ObservableCollection<Calib_AnalogOutput>)GetValue(AnalogOutputCalibDatasProperty); }
            set { SetValue(AnalogOutputCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AnalogOutputCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AnalogOutputCalibDatasProperty =
            DependencyProperty.Register("AnalogOutputCalibDatas", typeof(ObservableCollection<Calib_AnalogOutput>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<Calib_AnalogOutput>()));


        public ObservableCollection<EqCalibData> InputEqCalibDatas
        {
            get { return (ObservableCollection<EqCalibData>)GetValue(InputEqCalibDatasProperty); }
            set { SetValue(InputEqCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputEqCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputEqCalibDatasProperty =
            DependencyProperty.Register("InputEqCalibDatas", typeof(ObservableCollection<EqCalibData>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<EqCalibData>()));

        public ObservableCollection<EqCalibData> OutputEqCalibDatas
        {
            get { return (ObservableCollection<EqCalibData>)GetValue(OutputEqCalibDatasProperty); }
            set { SetValue(OutputEqCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OutputEqCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OutputEqCalibDatasProperty =
            DependencyProperty.Register("OutputEqCalibDatas", typeof(ObservableCollection<EqCalibData>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<EqCalibData>()));

        public ObservableCollection<Calib_ImpedanceThieleSmall> ImpedanceThieleSmalls
        {
            get { return (ObservableCollection<Calib_ImpedanceThieleSmall>)GetValue(ImpedanceThieleSmallsProperty); }
            set { SetValue(ImpedanceThieleSmallsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImpedanceThieleSmalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImpedanceThieleSmallsProperty =
            DependencyProperty.Register("ImpedanceThieleSmalls", typeof(ObservableCollection<Calib_ImpedanceThieleSmall>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<Calib_ImpedanceThieleSmall>()));

        public ObservableCollection<Calib_LoudspeakerProductionTest> LoudspeakerCalibDatas
        {
            get { return (ObservableCollection<Calib_LoudspeakerProductionTest>)GetValue(LoudspeakerCalibDatasProperty); }
            set { SetValue(LoudspeakerCalibDatasProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoudspeakerCalibDatas.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoudspeakerCalibDatasProperty =
            DependencyProperty.Register("LoudspeakerCalibDatas", typeof(ObservableCollection<Calib_LoudspeakerProductionTest>), typeof(ApxCalibration), new PropertyMetadata(new ObservableCollection<Calib_LoudspeakerProductionTest>()));



        public TestCore.MetaData.Info_EquipmentInstance EqInstance
        {
            get { return (TestCore.MetaData.Info_EquipmentInstance)GetValue(EqInstanceProperty); }
            set { SetValue(EqInstanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EqInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EqInstanceProperty =
            DependencyProperty.Register("EqInstance", typeof(TestCore.MetaData.Info_EquipmentInstance), typeof(ApxCalibration), new PropertyMetadata(null));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Messsage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(ApxCalibration), new PropertyMetadata(null));

        public int ValidDay
        {
            get { return (int)GetValue(ValidDayProperty); }
            set { SetValue(ValidDayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidDay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidDayProperty =
            DependencyProperty.Register("ValidDay", typeof(int), typeof(ApxCalibration), new PropertyMetadata(0));

        public int ValidHour
        {
            get { return (int)GetValue(ValidHourProperty); }
            set { SetValue(ValidHourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidHour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidHourProperty =
            DependencyProperty.Register("ValidHour", typeof(int), typeof(ApxCalibration), new PropertyMetadata(0));

        public int WarnDay
        {
            get { return (int)GetValue(WarnDayProperty); }
            set { SetValue(WarnDayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarnDay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarnDayProperty =
            DependencyProperty.Register("WarnDay", typeof(int), typeof(ApxCalibration), new PropertyMetadata(0));

        public int WarnHour
        {
            get { return (int)GetValue(WarnHourProperty); }
            set { SetValue(WarnHourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarnHour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarnHourProperty =
            DependencyProperty.Register("WarnHour", typeof(int), typeof(ApxCalibration), new PropertyMetadata(0));

        public DelegateCommand NewAcousticInput { get; }
        public DelegateCommand NewAnalogInput { get; }
        public DelegateCommand NewAcousticOutput { get; }
        public DelegateCommand NewAnalogOutput { get; }

        public DelegateCommand DeleteAcousticInput { get; }
        public DelegateCommand DeleteAnalogInput { get; }
        public DelegateCommand DeleteAcousticOutput { get; }
        public DelegateCommand DeleteAnalogOutput { get; }

        public string CalibFilePath { get; private set; }

        public ApxCalibration(TestCore.MetaData.Info_EquipmentInstance eqinstance) : this()
        {
            EqInstance = eqinstance;
            var calibdata = eqinstance?.Documents?.FirstOrDefault(x => System.IO.Path.GetExtension(x.Name) == ApCalibrationData.FileExt);

            if (calibdata is null)
            {
                var path = ApxEngine.HardwareCalibrationPath;

                if (File.Exists(path))
                {
                    using (StreamReader sr = new StreamReader(path))
                    {
                        ApCalibrationData data = XmlSerializerHelper.Deserialize(sr.ReadToEnd(), typeof(ApCalibrationData)) as ApCalibrationData;

                        LoadApCalibData(data);
                        CalibFilePath = path;
                    }
                }
            }
            else
            {
                List<string> paths = new List<string>();
                foreach (var doc in eqinstance.Documents)
                {
                    string dest = null;
                    doc.SaveAs(ref dest);

                    paths.Add(dest);
                }

                foreach (var p in paths)
                {
                    if (System.IO.Path.GetExtension(p).ToLower() == ApCalibrationData.FileExt)
                    {
                        using (StreamReader sr = new StreamReader(p))
                        {
                            ApCalibrationData data = XmlSerializerHelper.Deserialize(sr.ReadToEnd(), typeof(ApCalibrationData)) as ApCalibrationData;

                            LoadApCalibData(data);
                            CalibFilePath = p;
                        }
                        break;
                    }
                }
            }

            CalibTab.SelectionChanged += CalibTab_SelectionChanged;
        }

        private void cmd_NewAcousticInput(object obj)
        {
            if (obj is AudioPrecision.API.InputConnectorType type)
            {
                if (AcousticInputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is null)
                {
                    AcousticInputCalibDatas.Add(new Calib_AcousticInput() { ConnectorType = type });
                }
                else
                {
                    Message = $"Already Exist AcousticInput for {type}";
                }
            }
        }

        private void cmd_DeleteAcousticInput(object obj)
        {
            if (obj is AudioPrecision.API.InputConnectorType type)
            {
                if (AcousticInputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is Calib_AcousticInput ai)
                {
                    AcousticInputCalibDatas.Remove(ai);
                }
                else
                {
                    Message = $"No AcousticInput for {type} Exist";
                }
            }
        }

        private void cmd_NewAnalogInput(object obj)
        {
            if (obj is AudioPrecision.API.InputConnectorType type)
            {
                if (AnalogInputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is null)
                {
                    AnalogInputCalibDatas.Add(new Calib_AnalogInput() { ConnectorType = type });
                }
                else
                {
                    Message = $"Already Exist AnalogInput for {type}";
                }
            }
        }

        private void cmd_DeleteAnalogInput(object obj)
        {
            if (obj is AudioPrecision.API.InputConnectorType type)
            {
                if (AnalogInputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is Calib_AnalogInput ai)
                {
                    AnalogInputCalibDatas.Remove(ai);
                }
                else
                {
                    Message = $"No AnalogInput for {type} Exist";
                }
            }
        }

        private void cmd_NewAcousticOutput(object obj)
        {
            if (obj is AudioPrecision.API.OutputConnectorType type)
            {
                if (AcousticOutputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is null)
                {
                    AcousticOutputCalibDatas.Add(new Calib_AcousticOutput() { ConnectorType = type });
                }
                else
                {
                    Message = $"Already Exist AcousticOuput for {type}";
                }
            }
        }

        private void cmd_DeleteAcousticOutput(object obj)
        {
            if (obj is AudioPrecision.API.OutputConnectorType type)
            {
                if (AcousticOutputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is Calib_AcousticOutput ao)
                {
                    AcousticOutputCalibDatas.Remove(ao);
                }
                else
                {
                    Message = $"No AcousticOuput for {type} Exist";
                }
            }
        }

        private void cmd_NewAnalogOutput(object obj)
        {
            if (obj is AudioPrecision.API.OutputConnectorType type)
            {
                if (AnalogOutputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is null)
                {
                    AnalogOutputCalibDatas.Add(new Calib_AnalogOutput() { ConnectorType = type });
                }
                else
                {
                    Message = $"Already Exist AnalogOutput for {type}";
                }
            }
        }

        private void cmd_DeleteAnalogOutput(object obj)
        {
            if (obj is AudioPrecision.API.OutputConnectorType type)
            {
                if (AnalogOutputCalibDatas.FirstOrDefault(x => x.ConnectorType == type) is Calib_AnalogOutput ao)
                {
                    AnalogOutputCalibDatas.Remove(ao);
                }
                else
                {
                    Message = $"No AnalogOutput for {type} Exist";
                }
            }
        }

        private void CalibTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private string TemporaryCalibDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"CalibData");
        /// <summary>
        /// Temporary Path for store all calib files. will Auto Removed and generated when called
        /// </summary>

        private ApxCalibration()
        {
            NewAcousticInput = new DelegateCommand(cmd_NewAcousticInput);
            NewAnalogInput = new DelegateCommand(cmd_NewAnalogInput);
            NewAcousticOutput = new DelegateCommand(cmd_NewAcousticOutput);
            NewAnalogOutput = new DelegateCommand(cmd_NewAnalogOutput);

            DeleteAcousticInput = new DelegateCommand(cmd_DeleteAcousticInput);
            DeleteAnalogInput = new DelegateCommand(cmd_DeleteAnalogInput);
            DeleteAcousticOutput = new DelegateCommand(cmd_DeleteAcousticOutput);
            DeleteAnalogOutput = new DelegateCommand(cmd_DeleteAnalogOutput);

            InitializeComponent();

            try
            {
                if (Directory.Exists(TemporaryCalibDir))
                {
                    Directory.Delete(TemporaryCalibDir);
                }

                Directory.CreateDirectory(TemporaryCalibDir);
            }
            catch
            { }
        }

        //private void AcousticInput_AddItem(object sender, AddingNewItemEventArgs e)
        //{
        //    if (sender is DataGrid dg)
        //    {
        //        e.NewItem = new AcousticInputCalibData(dg.Items.Count);
        //    }
        //}

        //private void AanlogInput_AddItem(object sender, AddingNewItemEventArgs e)
        //{
        //    if (sender is DataGrid dg)
        //    {
        //        e.NewItem = new AnalogInputCalibData(dg.Items.Count);
        //    }
        //}

        private void Eq_AddItem(object sender, AddingNewItemEventArgs e)
        {
            if (sender is DataGrid dg)
            {
                e.NewItem = new EqCalibData(dg.Items.Count);
            }
        }

        private void dg_Impedance_AddItem(object sender, AddingNewItemEventArgs e)
        {
            if (sender is DataGrid dg)
            {
                var calib = Calib_ImpedanceThieleSmall.GetFromHardware();
                
                e.NewItem = calib;
            }
        }

        private void dg_Loadspeaker_AddItem(object sender, AddingNewItemEventArgs e)
        {
            if (sender is DataGrid dg)
            {
                var calib = Calib_LoudspeakerProductionTest.GetFromHardware();

                e.NewItem = calib;
            }
        }

        private void SelectPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                if (btn.Tag is DataGrid dg)
                {
                    if (btn.DataContext is EqCalibData eq)
                    {
                        OpenFileDialog ofd = new OpenFileDialog();
                        ofd.Title = "Select EQ File";
                        if (ofd.ShowDialog() == true)
                        {
                            eq.EqPath = ofd.FileName;
                            dg.Items.Refresh();
                        }
                    }
                    else if (btn.DataContext is Calib_LoudspeakerProductionTest loudspeaker)
                    {
                        if (loudspeaker.TestConfiguration == AudioPrecision.API.LoudspeakerTestConfiguration.External2Ch)
                        {
                            MessageBox.Show("4 wire does not require Correction Curve");
                            return;
                        }

                        OpenFileDialog ofd = new OpenFileDialog();
                        ofd.Title = "Select Correction Curve";
                        if (ofd.ShowDialog() == true)
                        {
                            loudspeaker.CorrectionCurve = ofd.FileName;
                            dg.Items.Refresh();
                        }
                    }
                    else if (btn.DataContext is Calib_ImpedanceThieleSmall imp)
                    {
                        if (imp.TestConfiguration == AudioPrecision.API.ImpedanceConfiguration.External2Ch)
                        {
                            MessageBox.Show("4 wire does not require Correction Curve");
                            return;
                        }

                        OpenFileDialog ofd = new OpenFileDialog();
                        ofd.Title = "Select Correction Curve";
                        if (ofd.ShowDialog() == true)
                        {
                            imp.CorrectionCurve = ofd.FileName;
                            dg.Items.Refresh();
                        }
                    }
                    else if (btn.DataContext != null) // For creating instance
                    {
                        if (dg.ItemsSource is ObservableCollection<EqCalibData> oc)
                        {
                            OpenFileDialog ofd = new OpenFileDialog();
                            ofd.Title = "Select EQ File";
                            if (ofd.ShowDialog() == true)
                            {
                                var tempeq = new EqCalibData(dg.Items.Count);
                                tempeq.EqPath = ofd.FileName;
                                oc.Add(tempeq);

                                dg.Items.Refresh();
                            }
                        }
                        else if (dg.ItemsSource is ObservableCollection<Calib_LoudspeakerProductionTest> ocls)
                        {
                            OpenFileDialog ofd = new OpenFileDialog();
                            ofd.Title = "Select Correction Curve";
                            if (ofd.ShowDialog() == true)
                            {
                                var tempeq = new Calib_LoudspeakerProductionTest();
                                tempeq.CorrectionCurve = ofd.FileName;
                                ocls.Add(tempeq);

                                dg.Items.Refresh();
                            }
                        }
                        else if (dg.ItemsSource is ObservableCollection<Calib_ImpedanceThieleSmall> ocis)
                        {
                            OpenFileDialog ofd = new OpenFileDialog();
                            ofd.Title = "Select Correction Curve";
                            if (ofd.ShowDialog() == true)
                            {
                                var tempeq = new Calib_ImpedanceThieleSmall();
                                tempeq.CorrectionCurve = ofd.FileName;
                                ocis.Add(tempeq);

                                dg.Items.Refresh();
                            }
                        }
                    }
                }
            }
        }

        // SaveAs
        private void SaveCalibData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                
                sfd.Filter = $"PTE Calibration Data|*{ApCalibrationData.FileExt}";
                sfd.Title = "Select the Calibration File";
                
                if(CalibFilePath != null)
                {
                    sfd.InitialDirectory = Directory.GetParent(CalibFilePath).FullName;
                    sfd.FileName = System.IO.Path.GetFileName(CalibFilePath);
                }

                if(sfd.ShowDialog() == true)
                {
                    SaveCalibData(System.IO.Path.GetDirectoryName(sfd.FileName), System.IO.Path.GetFileNameWithoutExtension(sfd.FileName));

                    MessageBox.Show($"Save Calib File as {sfd.FileName} OK", "Save Calib File OK");
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}", "Save Calib File Failed");
            }
        }

        private string SaveCalibData(string targetdir, string name = null)
        {
            if(CalibFilePath == null)
            {
                CalibFilePath = ApxEngine.HardwareCalibrationPath;
            }
            var sourcedir = System.IO.Path.GetDirectoryName(CalibFilePath);
            var sourcefiledir = System.IO.Path.Combine(sourcedir, System.IO.Path.GetFileNameWithoutExtension(CalibFilePath));
            bool issame = string.Equals(sourcedir, targetdir, StringComparison.OrdinalIgnoreCase);

            ApCalibrationData data = new ApCalibrationData();
            data.AnalogInputs = AnalogInputCalibDatas;
            data.AcousticInputs = AcousticInputCalibDatas;
            data.AnalogOutputs = AnalogOutputCalibDatas;
            data.AcousticOutputs = AcousticOutputCalibDatas;
            data.InputEqDatas = InputEqCalibDatas;
            data.OutputEqDatas = OutputEqCalibDatas;
            data.LoudspeakerProductionTests = LoudspeakerCalibDatas;
            data.ImpedanceThieleSmalls = ImpedanceThieleSmalls;

            data.EqType = EqInstance?.Model;
            data.EqSerialNumber = EqInstance?.SerialNumber;

            data.ValidTime = new TimeSpan(ValidDay, ValidHour, 0, 0);
            data.WarnTime = new TimeSpan(WarnDay, WarnHour, 0, 0);

            if (name is null)
            {
                name = EqInstance is null ? "default" : $"{EqInstance.Equipment?.Model}_{EqInstance.SerialNumber}";
            }

            var filedir = System.IO.Path.Combine(targetdir, name);

            if (!Directory.Exists(targetdir))
            {
                Directory.CreateDirectory(targetdir);
            }

            if (!Directory.Exists(filedir))
            {
                Directory.CreateDirectory(filedir);
            }

            foreach (var eq in InputEqCalibDatas)
            {
                if (eq.EqPath.Contains(":")) // Abs Path
                {
                    var fn = System.IO.Path.GetFileName(eq.EqPath);

                    File.Copy(eq.EqPath, System.IO.Path.Combine(filedir, fn), true);
                    eq.EqPath = fn;
                }
                else if (!issame)
                {
                    var eqfile = System.IO.Path.Combine(sourcefiledir, eq.EqPath);
                    if (File.Exists(eqfile))
                    {
                        File.Copy(eqfile, System.IO.Path.Combine(filedir, eq.EqPath), true);
                    }
                }
            }

            foreach (var eq in OutputEqCalibDatas)
            {
                if (eq.EqPath.Contains(":")) // Abs Path
                {
                    var fn = System.IO.Path.GetFileName(eq.EqPath);
                    File.Copy(eq.EqPath, System.IO.Path.Combine(filedir, fn), true);
                    eq.EqPath = fn;
                }

                if (!issame)
                {
                    var eqfile = System.IO.Path.Combine(sourcefiledir, eq.EqPath);
                    if (File.Exists(eqfile))
                    {
                        File.Copy(eqfile, System.IO.Path.Combine(filedir, eq.EqPath), true);
                    }
                }
            }

            foreach (var imp in ImpedanceThieleSmalls)
            {
                if (imp.CorrectionCurve?.Contains(":") == true)
                {
                    var fn = System.IO.Path.GetFileName(imp.CorrectionCurve);
                    File.Copy(imp.CorrectionCurve, System.IO.Path.Combine(filedir, fn), true);
                    imp.CorrectionCurve = fn;
                }

                if (!issame)
                {
                    var eqfile = System.IO.Path.Combine(sourcefiledir, imp.CorrectionCurve);
                    if (File.Exists(eqfile))
                    {
                        File.Copy(eqfile, System.IO.Path.Combine(filedir, imp.CorrectionCurve), true);
                    }
                }
            }

            foreach (var loudspeaker in LoudspeakerCalibDatas)
            {
                if (loudspeaker.CorrectionCurve?.Contains(":") == true)
                {
                    var fn = System.IO.Path.GetFileName(loudspeaker.CorrectionCurve);
                    File.Copy(loudspeaker.CorrectionCurve, System.IO.Path.Combine(filedir, fn), true);
                    loudspeaker.CorrectionCurve = fn;
                }

                if (!issame)
                {
                    var eqfile = System.IO.Path.Combine(sourcefiledir, loudspeaker.CorrectionCurve);
                    if (File.Exists(eqfile))
                    {
                        File.Copy(eqfile, System.IO.Path.Combine(filedir, loudspeaker.CorrectionCurve), true);
                    }
                }
            }

            var calibfile = System.IO.Path.Combine(targetdir, $"{name}{ApCalibrationData.FileExt}");
            data.Save(calibfile);

            return name;
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();

                ofd.Multiselect = false;
                ofd.Filter = $"Calibration Data|*{ApCalibrationData.FileExt}|Any File|*.*";

                if (ofd.ShowDialog() == true)
                {
                    using (StreamReader sr = new StreamReader(ofd.FileName))
                    {
                        ApCalibrationData data = XmlSerializerHelper.Deserialize(sr.ReadToEnd(), typeof(ApCalibrationData)) as ApCalibrationData;

                        //LoadApCalibData(data);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}", "Open Calib File Failed");
            }
        }

        private void LoadApCalibData(ApCalibrationData data)
        {
            if (EqInstance != null)
            {
                if (EqInstance?.Equipment?.Model != data.EqType)
                {
                    if (MessageBox.Show($"Current ApCalibData is not suit for {EqInstance.Equipment}, Are you sure Load it as {EqInstance.Equipment} any way?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        data.EqType = EqInstance.Equipment?.Model;
                        data.EqSerialNumber = EqInstance.SerialNumber;
                    }
                    else
                    {
                        return;
                    }
                }

                if (EqInstance?.SerialNumber != data.EqSerialNumber)
                {
                    if (MessageBox.Show($"Current ApCalibData SN is {data.EqSerialNumber}, Are you sure Apply it to {EqInstance.SerialNumber} any way?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        data.EqSerialNumber = EqInstance.SerialNumber;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            AcousticInputCalibDatas.Clear();
            foreach (var ac in data.AcousticInputs)
            {
                AcousticInputCalibDatas.Add(ac);
            }

            AnalogInputCalibDatas.Clear();
            foreach (var analog in data.AnalogInputs)
            {
                AnalogInputCalibDatas.Add(analog);
            }

            AcousticOutputCalibDatas.Clear();
            foreach (var acout in data.AcousticOutputs)
            {
                AcousticOutputCalibDatas.Add(acout);
            }

            AnalogOutputCalibDatas.Clear();
            foreach (var aout in data.AnalogOutputs)
            {
                AnalogOutputCalibDatas.Add(aout);
            }

            InputEqCalibDatas.Clear();
            foreach (var eq in data.InputEqDatas)
            {
                InputEqCalibDatas.Add(eq);
            }

            OutputEqCalibDatas.Clear();
            foreach (var eq in data.OutputEqDatas)
            {
                OutputEqCalibDatas.Add(eq);
            }

            ImpedanceThieleSmalls.Clear();
            foreach (var ts in data.ImpedanceThieleSmalls)
            {
                ImpedanceThieleSmalls.Add(ts);
            }

            LoudspeakerCalibDatas.Clear();
            foreach (var pts in data.LoudspeakerProductionTests)
            {
                LoudspeakerCalibDatas.Add(pts);
            }

            Version = data.Version;
            ValidHour = data.ValidTime.Hours;
            ValidDay = data.ValidTime.Days;

            WarnDay = data.WarnTime.Days;
            WarnHour = data.WarnTime.Hours;
        }

        private void btn_CommitAndQuit_Click(object sender, RoutedEventArgs e)
        {
            if (EqInstance is null)
            {
                MessageBox.Show("Null Equipment Instance, Only Apply Local Only. 没有找到设备,仅适用于本地", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            SaveCalibData(System.IO.Path.GetDirectoryName(ApxEngine.HardwareCalibrationPath), System.IO.Path.GetFileNameWithoutExtension(ApxEngine.HardwareCalibrationPath));

            //if (EqInstance != null)
            //{
            //    if (System.IO.Directory.Exists(ApxEngine.CalibrationBase))
            //    {
            //        if (System.IO.Directory.GetFiles(ApxEngine.CalibrationBase).Length > 0)
            //        {
            //            var files = Directory.GetFiles(ApxEngine.CalibrationBase);

            //            foreach (var file in files)
            //            {
            //                EqInstance.InsertNewDocument(file);
            //            }

            //            DialogResult = true;
            //            Close();
            //        }
            //    }
            //}
            //else
            //{
            //    MessageBox.Show("No Saved Calib Data found. If you open a Calib Data File, Please Save At First", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            //}
        }

        private void DisableCalibration_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("You are trying to DISABLE the APx Calibration. All Calibration Data will be deleted. Are you sure?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                if(File.Exists(ApxEngine.HardwareCalibrationPath))
                {
                    try
                    {
                        File.Delete(ApxEngine.HardwareCalibrationPath);

                        var fn = System.IO.Path.GetFileNameWithoutExtension(ApxEngine.HardwareCalibrationPath);

                        var dir = System.IO.Path.Combine(ApxEngine.CalibrationBase, fn);
                        if (Directory.Exists(dir))
                        {
                            Directory.Delete(dir, true);
                        }
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Error", $"Disable Calibration Failed. Error: {ex.Message}");
                    }
                }
            }
        }

        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {

        }

        //private void CalibByScript_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        ScriptCalibration calibration = new ScriptCalibration();
        //        calibration.ShowDialog();
        //    }
        //    catch(Exception ex)
        //    {
        //        MessageBox.Show(ex.ToString(), "Error");
        //    }
        //}


    }

    //public class ApCalibData : System.Xml.Serialization.IXmlSerializable
    //{
    //    public const string FileExt = ".pcd";
    //    public string Version { get; private set; } = "0.1";
    //    public const string Target = "Apx";

    //    public string EqType { get; set; }
    //    public string EqSerialNumber { get; set; }

    //    [XmlIgnore]
    //    public string CurrentDir { get; set; }

    //    public double AcousticInputSetting_Level { get; set; }
    //    public double AcousticInputSetting_Frequency { get; set; }
    //    public double AcousticInputSetting_Tolerance { get; set; }

    //    [XmlArray]
    //    public IEnumerable<AcousticInputCalibData> AcousticInputCalibDatas { get; set; }
    //    [XmlArray]
    //    public IEnumerable<AnalogInputCalibData> AnalogInputCalibDatas { get; set; }
    //    [XmlArray]
    //    public IEnumerable<EqCalibData> InputEqDatas { get; set; }
    //    [XmlArray]
    //    public IEnumerable<EqCalibData> OutputEqDatas { get; set; }
    //    [XmlArray]
    //    public IEnumerable<Calib_LoudspeakerProductionTest> LoudspeakerProductionTestCalibDatas { get; set; }

    //    public int Save(string path)
    //    {
    //        File.WriteAllText(path, XmlSerializerHelper.Serialize(this));

    //        return 1;
    //    }

    //    public static ApCalibData FromFile(string path)
    //    {
    //        var txt = File.ReadAllText(path);
    //        var data = (ApCalibData)XmlSerializerHelper.Deserialize(txt, typeof(ApCalibData));

    //        data.CurrentDir = System.IO.Path.GetDirectoryName(path);

    //        foreach (var eq in data.InputEqDatas)
    //        {
    //            if (!string.IsNullOrWhiteSpace(eq.EqPath))
    //            {
    //                eq.EqPath = System.IO.Path.Combine(data.CurrentDir, eq.EqPath);
    //            }
    //        }

    //        foreach (var eq in data.OutputEqDatas)
    //        {
    //            if (!string.IsNullOrWhiteSpace(eq.EqPath))
    //            {
    //                eq.EqPath = System.IO.Path.Combine(data.CurrentDir, eq.EqPath);
    //            }
    //        }

    //        foreach (var loudspeaker in data.LoudspeakerProductionTestCalibDatas)
    //        {
    //            if (!string.IsNullOrWhiteSpace(loudspeaker.CorrectionCurve))
    //            {
    //                loudspeaker.CorrectionCurve = System.IO.Path.Combine(data.CurrentDir, loudspeaker.CorrectionCurve);
    //            }
    //        }

    //        return data;
    //    }

    //    public XmlSchema GetSchema()
    //    {
    //        return null;
    //    }

    //    public void ReadXml(XmlReader reader)
    //    {
    //        bool isEmpty = reader.IsEmptyElement;

    //        if (isEmpty) return;

    //        Version = reader.GetAttribute("version");
    //        EqType = reader.GetAttribute("eqtype");
    //        EqSerialNumber = reader.GetAttribute("eqsn");

    //        reader.Read();

    //        while (reader.NodeType != XmlNodeType.EndElement)
    //        {
    //            var acoustic = new XmlSerializer(typeof(AcousticInputCalibData[]));

    //            reader.ReadStartElement("AcousticInputCalibDatas");
    //            if (double.TryParse(reader.GetAttribute("level"), out double acousticInput_level))
    //            {
    //                AcousticInputSetting_Level = acousticInput_level;
    //            }

    //            if (double.TryParse(reader.GetAttribute("frequency"), out double acousticInput_freq))
    //            {
    //                AcousticInputSetting_Frequency = acousticInput_freq;
    //            }

    //            if (double.TryParse(reader.GetAttribute("tolerance"), out double acousticInput_tol))
    //            {
    //                AcousticInputSetting_Tolerance = acousticInput_tol;
    //            }

    //            AcousticInputCalibDatas = acoustic.Deserialize(reader) as IEnumerable<AcousticInputCalibData>;
    //            reader.ReadEndElement();

    //            var analog = new XmlSerializer(typeof(AnalogInputCalibData[]));
    //            reader.ReadStartElement("AnalogInputCalibDatas");
    //            AnalogInputCalibDatas = analog.Deserialize(reader) as IEnumerable<AnalogInputCalibData>;
    //            reader.ReadEndElement();

    //            var eq = new XmlSerializer(typeof(EqCalibData[]));
    //            reader.ReadStartElement("InputEqDatas");
    //            InputEqDatas = eq.Deserialize(reader) as IEnumerable<EqCalibData>;
    //            reader.ReadEndElement();

    //            reader.ReadStartElement("OutputEqDatas");
    //            OutputEqDatas = eq.Deserialize(reader) as IEnumerable<EqCalibData>;
    //            reader.ReadEndElement();

    //            var loudspeaker = new XmlSerializer(typeof(Calib_LoudspeakerProductionTest[]));

    //            reader.ReadStartElement("LoudspeakerProductionTestCalibDatas");
    //            LoudspeakerProductionTestCalibDatas = loudspeaker.Deserialize(reader) as IEnumerable<Calib_LoudspeakerProductionTest>;
    //            reader.ReadEndElement();

    //            reader.MoveToContent();
    //        }

    //        reader.ReadEndElement();
    //    }

    //    public void WriteXml(XmlWriter writer)
    //    {
    //        writer.WriteAttributeString("version", Version);
    //        writer.WriteAttributeString("target", Target);
    //        writer.WriteAttributeString("eqtype", EqType);
    //        writer.WriteAttributeString("eqsn", EqSerialNumber);

    //        var acoustic = new XmlSerializer(AcousticInputCalibDatas.GetType());

    //        writer.WriteStartElement("AcousticInputCalibDatas");
    //        writer.WriteAttributeString("level", AcousticInputSetting_Level.ToString());
    //        writer.WriteAttributeString("frequency", AcousticInputSetting_Frequency.ToString());
    //        writer.WriteAttributeString("tolerance", AcousticInputSetting_Tolerance.ToString());
    //        acoustic.Serialize(writer, AcousticInputCalibDatas);
    //        writer.WriteEndElement();

    //        var analog = new XmlSerializer(AnalogInputCalibDatas.GetType());

    //        writer.WriteStartElement("AnalogInputCalibDatas");
    //        analog.Serialize(writer, AnalogInputCalibDatas);
    //        writer.WriteEndElement();

    //        var eq = new XmlSerializer(InputEqDatas.GetType());

    //        writer.WriteStartElement("InputEqDatas");
    //        eq.Serialize(writer, InputEqDatas);
    //        writer.WriteEndElement();
    //        writer.WriteStartElement("OutputEqDatas");
    //        eq.Serialize(writer, OutputEqDatas);
    //        writer.WriteEndElement();

    //        var loudspeaker = new XmlSerializer(LoudspeakerProductionTestCalibDatas.GetType());

    //        writer.WriteStartElement("LoudspeakerProductionTestCalibDatas");
    //        loudspeaker.Serialize(writer, LoudspeakerProductionTestCalibDatas);
    //        writer.WriteEndElement();
    //    }
    //}

    ///// <summary>
    ///// Unit dBSPL
    ///// </summary>
    //public class AcousticInputCalibData
    //{
    //    public int Index { get; set; }
    //    public string Channel { get => $"CH{Index}"; }
    //    //public string SN { get; set; }
    //    public double Sensitivity { get; set; } = 10;
    //    public double Frequency { get; set; } = 1000; // Hz
    //    public double Sensitivity_Expected { get; set; } = 10; //mv/Pa
    //    public double Sensitivity_Tolerance { get; set; } = 1; // dB

    //    public AcousticInputCalibData(int index)
    //    {
    //        Index = index;
    //    }

    //    public AcousticInputCalibData()
    //    {
    //    }
    //}

    //public class AnalogInputCalibData
    //{
    //    public int Index { get; set; }
    //    public string Channel { get => $"CH{Index}"; }  // In AP, it might be CH, Ch
    //    public double Frequency { get; set; } = 1000; // Hz

    //    public double dBSPL1 { get; set; } = 10;
    //    public double dBSPL1_CalibratorLevel { get; set; } = 94;
    //    public double dBSPL2 { get; set; } = 10;
    //    public double dBSPL2_CalibratorLevel { get; set; } = 94;

    //    public AnalogInputCalibData(int index)
    //    {
    //        Index = index;
    //    }

    //    public AnalogInputCalibData()
    //    {
    //    }
    //}
}
