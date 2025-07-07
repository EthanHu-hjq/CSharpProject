using Microsoft.Win32;
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
using TestCore.Ctrls;
using TestCore.Data;
using ToucanCore.Abstraction.Configuration;
using ToucanCore.Abstraction.HAL;
using ToucanCore.HAL;

namespace ToucanCore.UIs
{
    /// <summary>
    /// Interaction logic for Setting.xaml
    /// </summary>
    public partial class HardwareSetting : Window
    {
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(HardwareSetting), new PropertyMetadata(false));

//#if DEBUG
//        public static string[] HardwareResources { get; } = new string[]
//                { "COM1", "COM2"};
//#else
        public static string[] HardwareResources { get; } = System.IO.Ports.SerialPort.GetPortNames();
//#endif
        public List<IStartTrigger> StartTriggers { get; } = HardwareConfig.StartTriggers;
        public List<IFixture> Fixtures { get; set; } = HardwareConfig.Fixtures;
        public List<IRelayArray> RelayArrays { get; set; } = HardwareConfig.RelayArrays;
        public List<ISerialNumberReader> SerialNumberReaders { get; set; } = HardwareConfig.SerialNumberReaders;

        public IStartTrigger StartTrigger
        {
            get { return (IStartTrigger)GetValue(StartTriggerProperty); }
            set { SetValue(StartTriggerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartTrigger.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartTriggerProperty =
            DependencyProperty.Register("StartTrigger", typeof(IStartTrigger), typeof(HardwareSetting), new PropertyMetadata(null));


        public ObservableCollection<HexValue> SlotMasks
        {
            get { return (ObservableCollection<HexValue>)GetValue(SlotMasksProperty); }
            set { SetValue(SlotMasksProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlotMasks.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotMasksProperty =
            DependencyProperty.Register("SlotMasks", typeof(ObservableCollection<HexValue>), typeof(HardwareSetting), new PropertyMetadata(new ObservableCollection<HexValue>()));

        /// <summary>
        /// If All Item is 0, bypass it
        /// </summary>
        public ObservableCollection<HexValue> PreUutRelayValues
        {
            get { return (ObservableCollection<HexValue>)GetValue(PreUutRelayValuesProperty); }
            set { SetValue(PreUutRelayValuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RelayValues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreUutRelayValuesProperty =
            DependencyProperty.Register("PreUutRelayValues", typeof(ObservableCollection<HexValue>), typeof(HardwareSetting), new PropertyMetadata(new ObservableCollection<HexValue>()));


        /// <summary>
        /// If All Item is 0, bypass it
        /// </summary>
        public ObservableCollection<HexValue> UutIdentifiedRelayValues
        {
            get { return (ObservableCollection<HexValue>)GetValue(UutIdentifiedRelayValuesProperty); }
            set { SetValue(UutIdentifiedRelayValuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UutIdentifiedRelayValues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UutIdentifiedRelayValuesProperty =
            DependencyProperty.Register("UutIdentifiedRelayValues", typeof(ObservableCollection<HexValue>), typeof(HardwareSetting), new PropertyMetadata(new ObservableCollection<HexValue>()));

        public ObservableCollection<HexValue> PostUutRelayValues
        {
            get { return (ObservableCollection<HexValue>)GetValue(PostUutRelayValuesProperty); }
            set { SetValue(PostUutRelayValuesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PostUutRelayValues.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PostUutRelayValuesProperty =
            DependencyProperty.Register("PostUutRelayValues", typeof(ObservableCollection<HexValue>), typeof(HardwareSetting), new PropertyMetadata(new ObservableCollection<HexValue>()));


        public IFixture ActiveFixture
        {
            get { return (IFixture)GetValue(ActiveFixtureProperty); }
            set { SetValue(ActiveFixtureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveFixture.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveFixtureProperty =
            DependencyProperty.Register("ActiveFixture", typeof(IFixture), typeof(HardwareSetting), new PropertyMetadata(null));

        //private static void ActiveFixtureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is HardwareSetting SettingHardware)
        //    {
        //        if (e.NewValue is IFixture fixture)
        //        {
        //            if (SettingHardware.ActiveFixture is Fixture_None)
        //            {
        //                SettingHardware.PreUutRelayValues.Clear();
        //                SettingHardware.UutIdentifiedRelayValues.Clear();
        //                SettingHardware.PostUutRelayValues.Clear();
        //            }
        //            else if (SettingHardware.ActiveRelayArray is IFixture relayarray)
        //            {
        //                if (fixture is IRelayArray fra)
        //                {
        //                    SettingHardware.ActiveRelayArray = fra;
        //                }

        //                SettingHardware.PreUutRelayValues.Clear();
        //                SettingHardware.UutIdentifiedRelayValues.Clear();
        //                SettingHardware.PostUutRelayValues.Clear();

        //                for (int i = 0; i < SettingHardware.SlotCount; i++)
        //                {
        //                    SettingHardware.PreUutRelayValues.Add(new HexValue());
        //                    SettingHardware.UutIdentifiedRelayValues.Add(new HexValue());
        //                    SettingHardware.PostUutRelayValues.Add(new HexValue());
        //                }
        //            }
        //        }
        //    }
        //}

        public IRelayArray ActiveRelayArray
        {
            get { return (IRelayArray)GetValue(ActiveRelayArrayProperty); }
            set { SetValue(ActiveRelayArrayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveRelayArray.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveRelayArrayProperty =
            DependencyProperty.Register("ActiveRelayArray", typeof(IRelayArray), typeof(HardwareSetting), new PropertyMetadata(null, ActiveRelayArrayChanged));

        private static void ActiveRelayArrayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HardwareSetting SettingHardware)
            {
                var hc = SettingHardware.DataContext as HardwareConfig;

                SettingHardware.SlotMasks.Clear();
                SettingHardware.PreUutRelayValues.Clear();
                SettingHardware.UutIdentifiedRelayValues.Clear();
                SettingHardware.PostUutRelayValues.Clear();
                
                if (e.NewValue is RelayArray_None)
                {
                }
                else if (e.NewValue is IRelayArray relayarray)
                {
                    var cnt = Math.Min(SettingHardware.SlotCount, hc.SlotMasks.Count);

                    for (int i = 0; i < cnt; i++)
                    {
                        SettingHardware.SlotMasks.Add(new HexValue() { Value = hc.SlotMasks[i] });
                        SettingHardware.PreUutRelayValues.Add(new HexValue() { Value = hc.PreUutRoute[i] });
                        SettingHardware.UutIdentifiedRelayValues.Add(new HexValue() { Value = hc.UutIdentifiedRoute[i] });
                        SettingHardware.PostUutRelayValues.Add(new HexValue() { Value = hc.PostUutRoute[i] });
                    }

                    for (int i = cnt; i < SettingHardware.SlotCount; i++)
                    {
                        SettingHardware.SlotMasks.Add(new HexValue());
                        SettingHardware.PreUutRelayValues.Add(new HexValue());
                        SettingHardware.UutIdentifiedRelayValues.Add(new HexValue());
                        SettingHardware.PostUutRelayValues.Add(new HexValue());
                    }
                }
            }
        }

        public ISerialNumberReader ActiveSnReader
        {
            get { return (ISerialNumberReader)GetValue(ActiveSnReaderProperty); }
            set { SetValue(ActiveSnReaderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveSnReader.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveSnReaderProperty =
            DependencyProperty.Register("ActiveSnReader", typeof(ISerialNumberReader), typeof(HardwareSetting), new PropertyMetadata(null));


        public bool TrigOnDutPresent
        {
            get { return (bool)GetValue(TrigOnDutPresentProperty); }
            set { SetValue(TrigOnDutPresentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TrigOnDutPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TrigOnDutPresentProperty =
            DependencyProperty.Register("TrigOnDutPresent", typeof(bool), typeof(HardwareSetting), new PropertyMetadata(false));

        public int SlotCount
        {
            get { return (int)GetValue(SlotCountProperty); }
            set { SetValue(SlotCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SlotCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotCountProperty =
            DependencyProperty.Register("SlotCount", typeof(int), typeof(HardwareSetting), new PropertyMetadata(0));

        public bool AutoDutIn
        {
            get { return (bool)GetValue(AutoDutInProperty); }
            set { SetValue(AutoDutInProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoDutIn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoDutInProperty =
            DependencyProperty.Register("AutoDutIn", typeof(bool), typeof(HardwareSetting), new PropertyMetadata(false));

        public bool AutoDutOut
        {
            get { return (bool)GetValue(AutoDutOutProperty); }
            set { SetValue(AutoDutOutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoDutOut.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoDutOutProperty =
            DependencyProperty.Register("AutoDutOut", typeof(bool), typeof(HardwareSetting), new PropertyMetadata(false));
        //public HardwareConfig HardwareConfig
        //{
        //    get { return (HardwareConfig)GetValue(HardwareConfigProperty); }
        //    set { SetValue(HardwareConfigProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for HardwareSetting.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty HardwareConfigProperty =
        //    DependencyProperty.Register("HardwareConfig", typeof(HardwareConfig), typeof(HardwareSetting), new PropertyMetadata(null));


        public ObservableCollection<KeyValue<string, string>> ComponentRegisters
        {
            get { return (ObservableCollection<KeyValue<string, string>>)GetValue(ComponentRegistersProperty); }
            set { SetValue(ComponentRegistersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComponentRegisters.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComponentRegistersProperty =
            DependencyProperty.Register("ComponentRegisters", typeof(ObservableCollection<KeyValue<string, string>>), typeof(HardwareSetting), new PropertyMetadata(null));



        public string DefaultFilePath { get; set; }
        
        public HardwareSetting()
        {
            InitializeComponent();
            ComponentRegisters = new ObservableCollection<KeyValue<string, string>>();
            DataContextChanged += HardwareSetting_DataContextChanged;
        }

        private void HardwareSetting_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is HardwareConfig hc)
            {
                RefreshHardwareConfig(hc);

                DefaultFilePath = hc.FilePath;

                if(hc.Registers is null)
                {
                    ComponentRegisters = new ObservableCollection<KeyValue<string, string>>();
                }   
                else
                {
                    ComponentRegisters = new ObservableCollection<KeyValue<string, string>>();
                    foreach(var kvp in hc.Registers)
                    {
                        ComponentRegisters.Add(new KeyValue<string, string>() { Key = kvp.Key, Value = kvp.Value });
                    }
                }
            }
        }
        private void RefreshHardwareConfig(HardwareConfig hc)
        {
            ActiveFixture = hc.Fixture ?? HardwareConfig.Fixtures.FirstOrDefault();
            ActiveRelayArray = hc.RelayArray ?? HardwareConfig.RelayArrays.FirstOrDefault();

            if(ActiveFixture != null)
            {
                StartTrigger = hc.StartTrigger;

                AutoDutIn = ActiveFixture.AutoDutIn;
                AutoDutOut = ActiveFixture.AutoDutOut;
            }

            //if (hc.RelayArray?.ChannelCount > 0)
            //{
            //    for (int i = hc.PreUutRoute.Count; i < ActiveFixture.SocketCount; i++)
            //    {
            //        hc.PreUutRoute.Add(-1);
            //        hc.UutIdentifiedRoute.Add(-1);
            //        hc.PostUutRoute.Add(-1);
            //    }

            //    for (int i = 0; i < ActiveFixture.SocketCount; i++)
            //    {
            //        PreUutRelayValues[i].Value = hc.PreUutRoute[i];
            //        UutIdentifiedRelayValues[i].Value = hc.UutIdentifiedRoute[i];
            //        PostUutRelayValues[i].Value = hc.PostUutRoute[i];
            //    }
            //}
        }

        /// <summary>
        /// Save UI data into HardwareConfig object
        /// </summary>
        /// <returns></returns>
        public HardwareConfig Save()
        {
            if (DataContext is HardwareConfig hc)
            {
            }
            else
            {
                hc = new HardwareConfig();
            }

            hc.StartTrigger = StartTrigger;
            hc.Fixture = ActiveFixture;

            hc.RelayArray = ActiveRelayArray;
            //if (hc.SlotMasks.Count > 0)
            //{
                hc.SlotMasks = SlotMasks.Select(x => x.Value).ToList();
                hc.PreUutRoute = PreUutRelayValues.Select(x => x.Value).ToList();
                hc.UutIdentifiedRoute = UutIdentifiedRelayValues.Select(x => x.Value).ToList();
                hc.PostUutRoute = PostUutRelayValues.Select(x => x.Value).ToList();
            //}

            foreach (var kv in ComponentRegisters)
            {
                if (hc.Registers.ContainsKey(kv.Key))
                {
                    hc.Registers[kv.Key] = kv.Value;
                }
                else
                {
                    hc.Registers.Add(kv.Key, kv.Value);
                }
            }

            hc.SerialNumberReader = ActiveSnReader;
            hc.TrigOnDutPresent = TrigOnDutPresent;
            return hc;
        }

        private void btn_SaveAsClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            //sfd.Filter = "XML File|*.xml";
            sfd.Title = "Save setting As...";

            if (Directory.Exists(DefaultFilePath))
            {
                sfd.InitialDirectory = Directory.GetParent(DefaultFilePath).FullName;
            }

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    HardwareConfig hc = Save();
                    hc.Save(sfd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Load {sfd.FileName} failed. ex: {ex}", "Warning");
                }
            }

        }

        private void btn_LoadClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "XML File|*.xml";
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    DataContext = HardwareConfig.Load(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Load {ofd.FileName} failed. ex: {ex}", "Warning");
                }
            }
        }

        private void btn_OkClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var hc = Save();
                if (hc.FilePath is null)
                {
                    hc.Save(DefaultFilePath);
                }
                else
                {
                    hc.Save(null);
                }

                DialogResult = true;
                Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Save Hardware Failed. Err {ex.Message}");
            }
        }

        private void btn_CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btn_Customize_Click(object sender, RoutedEventArgs e)
        {
            //FixtureSetting fs = new FixtureSetting();
            //fs.ShowDialog();

            try
            {
                ActiveFixture?.SettingUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warnning");
            }
        }
    }

    public class KeyValue<T, V>
    {
        public T Key { get; set; }
        public V Value { get; set; }
    }
}
