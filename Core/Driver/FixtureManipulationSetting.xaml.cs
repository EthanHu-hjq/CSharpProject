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
using TestCore.Services;
using ToucanCore.HAL;
using ToucanCore.UIs;

namespace ToucanCore.Driver
{
    /// <summary>
    /// PLC_Mitsubishi_FX_SettingUI.xaml 的交互逻辑
    /// </summary>
    public partial class FixtureManipulationSetting : Window
    {
        public string ConfigFilePath { get; }
        private FixtureManipulationConfig Config { get; }

        //public static Array InCmds { get; } = Enum.GetValues(typeof(FixtureManipulation_In));
        //public static Array OutCmds { get; } = Enum.GetValues(typeof(FixtureManipulation_Out));

        public ObservableCollection<KeyValue<string, string>> InCommands
        {
            get { return (ObservableCollection<KeyValue<string, string>>)GetValue(InCommandsProperty); }
            set { SetValue(InCommandsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InCommands.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InCommandsProperty =
            DependencyProperty.Register("InCommands", typeof(ObservableCollection<KeyValue<string, string>>), typeof(FixtureManipulationSetting), new PropertyMetadata(null));


        public ObservableCollection<KeyValue<string, string>> OutCommands
        {
            get { return (ObservableCollection<KeyValue<string, string>>)GetValue(OutCommandsProperty); }
            set { SetValue(OutCommandsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OutCommands.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OutCommandsProperty =
            DependencyProperty.Register("OutCommands", typeof(ObservableCollection<KeyValue<string, string>>), typeof(FixtureManipulationSetting), new PropertyMetadata(null));


        public FixtureManipulationSetting(string configFilePath, FixtureManipulationConfig initconfig = null)
        {
            ConfigFilePath = configFilePath;

            if (initconfig is FixtureManipulationConfig config)
            {
                Config = config;
            }
            else if (System.IO.File.Exists(configFilePath))
            {
                Config = FixtureManipulationConfig.Load(configFilePath);
            }

            InCommands = new ObservableCollection<KeyValue<string, string>>();
            foreach (var item in Config.InCommands) InCommands.Add(new KeyValue<string, string>() { Key=item.Key, Value=item.Value});

            OutCommands = new ObservableCollection<KeyValue<string, string>>();
            foreach (var item in Config.OutCommands) OutCommands.Add(new KeyValue<string, string>() { Key = item.Key, Value = item.Value });

            InitializeComponent();
        }

        private void btn_Clear_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(ConfigFilePath))
            {
                if (MessageBox.Show("Are you sure to clear the Local Setting", "Warning", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    FileInfo fi = new FileInfo(ConfigFilePath);

                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    fi.Delete();
                }
            }
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in InCommands)
            {
                if (Config.InCommands.ContainsKey(item.Key))
                {
                    Config.InCommands[item.Key] = item.Value;
                }
                else
                {
                    Config.InCommands.Add(item.Key, item.Value);
                }
                
            }

            foreach (var item in OutCommands)
            {
                if (Config.OutCommands.ContainsKey(item.Key))
                {
                    Config.OutCommands[item.Key] = item.Value;
                }
                else
                {
                    Config.OutCommands.Add(item.Key, item.Value);
                }
            }

            Config.SaveAs(ConfigFilePath);
        }
    }
}
