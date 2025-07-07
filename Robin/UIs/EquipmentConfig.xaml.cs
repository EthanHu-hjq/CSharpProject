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
using System.Windows.Shapes;
using TestCore.Ctrls;

namespace Robin.UIs
{
    /// <summary>
    /// Interaction logic for EquipmentConfig.xaml
    /// </summary>
    public partial class EquipmentConfig : Window
    {
        HardwareControl hardwarecontrol { get; }

        public HexValue ControlMask
        {
            get { return (HexValue)GetValue(ControlMaskProperty); }
            set { SetValue(ControlMaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ControlMask.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ControlMaskProperty =
            DependencyProperty.Register("ControlMask", typeof(HexValue), typeof(EquipmentConfig), new PropertyMetadata(null));

        public int DoorCtrl
        {
            get { return (int)GetValue(DoorCtrlProperty); }
            set { SetValue(DoorCtrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DoorCtrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoorCtrlProperty =
            DependencyProperty.Register("DoorCtrl", typeof(int), typeof(EquipmentConfig), new PropertyMetadata(-1));

        public int JigCtrl
        {
            get { return (int)GetValue(JigCtrlProperty); }
            set { SetValue(JigCtrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for JigCtrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty JigCtrlProperty =
            DependencyProperty.Register("JigCtrl", typeof(int), typeof(EquipmentConfig), new PropertyMetadata(-1));

        public int DutCtrl
        {
            get { return (int)GetValue(DutCtrlProperty); }
            set { SetValue(DutCtrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutCtrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutCtrlProperty =
            DependencyProperty.Register("DutCtrl", typeof(int), typeof(EquipmentConfig), new PropertyMetadata(-1));

        public ObservableCollection<KeyValueObject<string, byte>> ControlStates
        {
            get { return (ObservableCollection<KeyValueObject<string, byte>>)GetValue(ControlStatesProperty); }
            set { SetValue(ControlStatesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for States.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ControlStatesProperty =
            DependencyProperty.Register("ControlStates", typeof(ObservableCollection<KeyValueObject<string, byte>>), typeof(EquipmentConfig), new PropertyMetadata(null));

        public HexValue ReadyMask
        {
            get { return (HexValue)GetValue(ReadyMaskProperty); }
            set { SetValue(ReadyMaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReadyMask.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadyMaskProperty =
            DependencyProperty.Register("ReadyMask", typeof(HexValue), typeof(EquipmentConfig), new PropertyMetadata(null));

        public HexValue ReadyValue
        {
            get { return (HexValue)GetValue(ReadyValueProperty); }
            set { SetValue(ReadyValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReadyValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadyValueProperty =
            DependencyProperty.Register("ReadyValue", typeof(HexValue), typeof(EquipmentConfig), new PropertyMetadata(null));


        public ObservableCollection<KeyValueObject<string,byte>> InputNames
        {
            get { return (ObservableCollection<KeyValueObject<string,byte>>)GetValue(InputNamesProperty); }
            set { SetValue(InputNamesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputNames.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputNamesProperty =
            DependencyProperty.Register("InputNames", typeof(ObservableCollection<KeyValueObject<string,byte>>), typeof(EquipmentConfig), new PropertyMetadata(null));


        string FilePath = App.HardwareDefinitionPath;

        public EquipmentConfig()
        {
            ControlStates = new ObservableCollection<KeyValueObject<string, byte>>();
            InputNames = new ObservableCollection<KeyValueObject<string, byte>>();
            if(hardwarecontrol is null)
            {
                try
                {
                    hardwarecontrol = HardwareControl.FromFile(FilePath);
                }
                catch
                {
                    hardwarecontrol = new HardwareControl();
                }
            }

            foreach (var d in hardwarecontrol.ControlStates)
            {
                ControlStates.Add(KeyValueObject<string, byte>.FromKeyValuePair(d));
            }

            foreach (var d in hardwarecontrol.InputNames)
            {
                InputNames.Add(KeyValueObject<string, byte>.FromKeyValuePair(d));
            }

            ControlMask = new HexValue(hardwarecontrol.ControlMask);

            DoorCtrl = hardwarecontrol.DoorCtrl;
            JigCtrl = hardwarecontrol.JigCtrl;
            DutCtrl = hardwarecontrol.DutCtrl;

            ReadyMask = new HexValue(hardwarecontrol.ReadyMask);
            ReadyValue = new HexValue(hardwarecontrol.ReadyValue);

            InitializeComponent();
        }

        public EquipmentConfig(HardwareControl hc) : this()
        {
            hardwarecontrol = hc;
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            if (ControlMask.Value < 0)
            {
                hardwarecontrol.ControlMask = 0;
            }
            else
            {
                hardwarecontrol.ControlMask = (byte)ControlMask.Value;
            }    
            
            hardwarecontrol.DoorCtrl = DoorCtrl;
            hardwarecontrol.JigCtrl = JigCtrl;
            hardwarecontrol.DutCtrl = DutCtrl;
            hardwarecontrol.ControlStates.Clear();
            foreach(var state in ControlStates)
            {
                if (string.IsNullOrWhiteSpace(state.Key)) continue;
                if (state.Value < 0) continue;
                if(hardwarecontrol.ControlStates.ContainsKey(state.Key))
                {
                    hardwarecontrol.ControlStates[state.Key] = state.Value;
                }
                else
                {
                    hardwarecontrol.ControlStates.Add(state.Key, state.Value);
                }
            }

            if(ReadyMask.Value < 0)
            {
                hardwarecontrol.ReadyMask = 0;
            }
            else
            {
                hardwarecontrol.ReadyMask = (byte)ReadyMask.Value;
            }

            if(ReadyValue.Value < 0)
            {
                hardwarecontrol.ReadyValue = 0;
            }
            else
            {
                hardwarecontrol.ReadyValue = (byte)ReadyValue.Value;
            }

            hardwarecontrol.InputNames.Clear();
            foreach (var state in InputNames)
            {
                if (string.IsNullOrWhiteSpace(state.Key)) continue;
                if (state.Value < 0) continue;
                if (hardwarecontrol.InputNames.ContainsKey(state.Key))
                {
                    hardwarecontrol.InputNames[state.Key] = state.Value;
                }
                else
                {
                    hardwarecontrol.InputNames.Add(state.Key, state.Value);
                }
            }

            hardwarecontrol.Save(FilePath);
        }

        private void dg_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var idx = e.Row.GetIndex();
            e.Row.Header = idx;
            if (e.Row.DataContext is KeyValueObject<string, byte> kvo)
            {
                kvo.Value = (byte)idx;
            }
        }

        private void btn_Reset_Click(object sender, RoutedEventArgs e)
        {
            ControlStates.Clear();
            InputNames.Clear();
        }
    }

    public class KeyValueObject<T, V>
    { 
        public T Key { get; set; }
        public V Value { get; set; }

        public static KeyValueObject<T, V> FromKeyValuePair(KeyValuePair<T, V> kvp)
        {
            return new KeyValueObject<T, V>() { Key = kvp.Key, Value = kvp.Value };
        }

        public KeyValuePair<T, V> ToKeyValuePair()
        {
            return new KeyValuePair<T, V>(Key, Value);
        }
    }
}
