using Microsoft.Win32;
using Robin.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
using TestCore;
using TestCore.Ctrls;
using MahApps.Metro.Controls;

namespace Robin.UIs
{
    /// <summary>
    /// Interaction logic for GlobalConfig.xaml
    /// </summary>
    public partial class GlobalConfig : MetroWindow
    {
        public VM_Robin Robin { get; set; }

        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(GlobalConfig), new PropertyMetadata(false));


        public ObservableCollection<KeyValueObject<GlobalDefinitionGroupName, object>> Items
        {
            get { return (ObservableCollection<KeyValueObject<GlobalDefinitionGroupName, object>>)GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof(ObservableCollection<KeyValueObject<GlobalDefinitionGroupName, object>>), typeof(GlobalConfig), new PropertyMetadata(null));

        public ObservableCollection<KeyValueObject<string, string>> Variables
        {
            get { return (ObservableCollection<KeyValueObject<string, string>>)GetValue(VariablesProperty); }
            set { SetValue(VariablesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Variables.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VariablesProperty =
            DependencyProperty.Register("Variables", typeof(ObservableCollection<KeyValueObject<string, string>>), typeof(GlobalConfig), new PropertyMetadata(null));

        public HexValue ControlMask
        {
            get { return (HexValue)GetValue(ControlMaskProperty); }
            set { SetValue(ControlMaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ControlMask.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ControlMaskProperty =
            DependencyProperty.Register("ControlMask", typeof(HexValue), typeof(GlobalConfig), new PropertyMetadata(null));

        public int DoorCtrl
        {
            get { return (int)GetValue(DoorCtrlProperty); }
            set { SetValue(DoorCtrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DoorCtrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DoorCtrlProperty =
            DependencyProperty.Register("DoorCtrl", typeof(int), typeof(GlobalConfig), new PropertyMetadata(-1));

        public int JigCtrl
        {
            get { return (int)GetValue(JigCtrlProperty); }
            set { SetValue(JigCtrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for JigCtrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty JigCtrlProperty =
            DependencyProperty.Register("JigCtrl", typeof(int), typeof(GlobalConfig), new PropertyMetadata(-1));

        public int DutCtrl
        {
            get { return (int)GetValue(DutCtrlProperty); }
            set { SetValue(DutCtrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutCtrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutCtrlProperty =
            DependencyProperty.Register("DutCtrl", typeof(int), typeof(GlobalConfig), new PropertyMetadata(-1));

        public ObservableCollection<KeyValueObject<string, byte>> ControlStates
        {
            get { return (ObservableCollection<KeyValueObject<string, byte>>)GetValue(ControlStatesProperty); }
            set { SetValue(ControlStatesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for States.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ControlStatesProperty =
            DependencyProperty.Register("ControlStates", typeof(ObservableCollection<KeyValueObject<string, byte>>), typeof(GlobalConfig), new PropertyMetadata(null));

        public HexValue ReadyMask
        {
            get { return (HexValue)GetValue(ReadyMaskProperty); }
            set { SetValue(ReadyMaskProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReadyMask.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadyMaskProperty =
            DependencyProperty.Register("ReadyMask", typeof(HexValue), typeof(GlobalConfig), new PropertyMetadata(null));

        public HexValue ReadyValue
        {
            get { return (HexValue)GetValue(ReadyValueProperty); }
            set { SetValue(ReadyValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReadyValue.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReadyValueProperty =
            DependencyProperty.Register("ReadyValue", typeof(HexValue), typeof(GlobalConfig), new PropertyMetadata(null));


        public ObservableCollection<KeyValueObject<string, byte>> InputNames
        {
            get { return (ObservableCollection<KeyValueObject<string, byte>>)GetValue(InputNamesProperty); }
            set { SetValue(InputNamesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for InputNames.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InputNamesProperty =
            DependencyProperty.Register("InputNames", typeof(ObservableCollection<KeyValueObject<string, byte>>), typeof(GlobalConfig), new PropertyMetadata(null));

        public DelegateCommand Save { get; }
        public DelegateCommand Reset { get; }
        GlobalGroupSetting GroupSetting { get; }
        HardwareControl hardwarecontrol { get; }

        string EquipmentFilePath = App.HardwareDefinitionPath;
        string DefinitionFilePath = App.GroupDefinitionPath;
        public GlobalConfig()
        {
            Save = new DelegateCommand(cmd_Save);
            Reset = new DelegateCommand(cmd_Reset);

            GroupItem<IGroupItem>.FilePath = "GlobalItem.xml";
            GroupItem<IGroupItem>.Workbase = App.Workbase;

            if(GroupSetting is null)
            {
                GroupSetting = GlobalGroupSetting.FromFile(DefinitionFilePath);
            }

            if(GroupSetting.GlobalDefinitionGroups.Count == 0)
            {
                GroupSetting = GlobalGroupSetting.GetDefault();
            }

            Variables = new ObservableCollection<KeyValueObject<string, string>>();
            
            foreach(var varitem in GroupSetting.Variables)
            {
                Variables.Add(KeyValueObject<string, string>.FromKeyValuePair(varitem));
            }

            Items = new ObservableCollection<KeyValueObject<GlobalDefinitionGroupName, object>>();

            foreach (var item in GroupSetting.GlobalDefinitionGroups)
            {

                if (item.Value is GroupItem<int> gii)
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = new ObservableGroupItem<int>(gii) });
                }
                else if (item.Value is GroupItem<double> gid)
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = new ObservableGroupItem<double>(gid) });
                }
                else if (item.Value is GroupItem<uint> giui)
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = new ObservableGroupItem<uint>(giui) });
                }
                else if (item.Value is GroupItem<float> gif)
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = new ObservableGroupItem<float>(gif) });
                }
                else if (item.Value is GroupItem<string> gis)
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = new ObservableGroupItem<string>(gis) });
                }
                else if (item.Value is GroupItem<byte> gibyte)
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = new ObservableGroupItem<byte>(gibyte) });
                }
                else
                {
                    Items.Add(new KeyValueObject<GlobalDefinitionGroupName, object>() { Key = item.Key, Value = item.Value});
                }
            }

            ControlStates = new ObservableCollection<KeyValueObject<string, byte>>();
            InputNames = new ObservableCollection<KeyValueObject<string, byte>>();
            if (hardwarecontrol is null)
            {
                try
                {
                    hardwarecontrol = HardwareControl.FromFile(EquipmentFilePath);
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

            //var apx500 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Audio Precision\\APx500", false);

            //if(apx500 != null)
            //{
            //    var aps = apx500.GetSubKeyNames();
            //    ApxPaths = new ApInformation[aps.Length];
            //    for (int i = 0; i < aps.Length; i++)
            //    {
            //        ApInformation api = new ApInformation();
            //        api.Version = aps[i];
            //        api.Location = apx500.OpenSubKey(aps[i]).GetValue("Location") as string;
            //        ApxPaths[i] = api; 
            //    }

            //    if (ApxPaths.FirstOrDefault(x => x.Location == GroupSetting.APxLocation) is ApInformation effectiveap)
            //    {
            //        effectiveap.Enable = true;
            //    }
            //    else
            //    {
            //        effectiveap = ApxPaths.LastOrDefault();
            //        effectiveap.Enable = true;
            //        GroupSetting.APxLocation = effectiveap.Location;
            //    }
            //}

            InitializeComponent();
        }

        public ApInformation[] ApxPaths { get; }

        private void cmd_Reset(object obj)
        {
            if(MessageBox.Show("You are trying to RESET the Global Setting, Are you SURE?", "Warn", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK)
            {
                GlobalGroupSetting.GetDefault().Save(DefinitionFilePath);
            }
        }

        private void cmd_Save(object obj)
        {
            GroupSetting.Variables.Clear();
            foreach(var varitem in Variables)
            {
                GroupSetting.Variables.Add(varitem.Key, varitem.Value);
            }

            var dict = GroupSetting.GlobalDefinitionGroups;
            foreach (var item in Items)
            {
                if (item.Value is ObservableGroupItem<double> ogdouble)
                {
                    ogdouble.Save.Execute(null);
                    dict[item.Key] = ogdouble.GroupItem;
                }
                else if (item.Value is ObservableGroupItem<int> ogiint)
                {
                    ogiint.Save.Execute(null);
                    dict[item.Key] = ogiint.GroupItem;
                }
                else if(item.Value is ObservableGroupItem<uint> ogiuint)
                {
                    ogiuint.Save.Execute(null);
                    dict[item.Key] = ogiuint.GroupItem;
                }
                else if (item.Value is ObservableGroupItem<string> ogstring)
                {
                    ogstring.Save.Execute(null);
                    List<KeyValuePair<string, string>> abss = new List<KeyValuePair<string, string>>();
                    foreach(var gitem in ogstring.GroupItem.Items)
                    {
                        if(gitem.Value.Contains(":\\"))
                        {
                            abss.Add(gitem);
                        }
                    }

                    var sdict = ogstring.GroupItem.Items as Dictionary<string, string>;

                    foreach (var absitem in abss)
                    {
                        if(System.IO.File.Exists(absitem.Value))
                        {
                            var name = System.IO.Path.GetFileName(absitem.Value);
                            System.IO.File.Copy(absitem.Value, System.IO.Path.Combine(App.CommonFileDir, name), true);

                            sdict[absitem.Key] = name;
                        }
                        else
                        {
                            MessageBox.Show($"{absitem.Value}, Adding Group Item {absitem.Key} denied", "Warning");
                        }
                    }

                    //ogstring.Save.Execute(null); // Had already update the groupitem in sdict;
                    dict[item.Key] = ogstring.GroupItem;
                }
                else if (item.Value is ObservableGroupItem<byte> ogbyte)
                {
                    ogbyte.Save.Execute(null);
                    dict[item.Key] = ogbyte.GroupItem;
                }
                else if (item.Value is ObservableGroupItem<float> ogfloat)
                {
                    ogfloat.Save.Execute(null);
                    dict[item.Key] = ogfloat.GroupItem;
                }
                else
                {
                    //dict[item.Key] = item.Value;
                }    
            }

            //if(ApxPaths.FirstOrDefault(x=> x.Enable) is ApInformation api)
            //{
            //    GroupSetting.APxLocation = api.Location;
            //}

            if(GroupSetting.Save(DefinitionFilePath) <= 0)
            {
                return;
            }

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
            foreach (var state in ControlStates)
            {
                if (string.IsNullOrWhiteSpace(state.Key)) continue;
                if (state.Value < 0) continue;
                if (hardwarecontrol.ControlStates.ContainsKey(state.Key))
                {
                    hardwarecontrol.ControlStates[state.Key] = state.Value;
                }
                else
                {
                    hardwarecontrol.ControlStates.Add(state.Key, state.Value);
                }
            }

            if (ReadyMask.Value < 0)
            {
                hardwarecontrol.ReadyMask = 0;
            }
            else
            {
                hardwarecontrol.ReadyMask = (byte)ReadyMask.Value;
            }

            if (ReadyValue.Value < 0)
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

            hardwarecontrol.Save(EquipmentFilePath);

            MessageBox.Show("Global Config Saved", "Save");
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

        private void btn_GotoDataDir_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!System.IO.Directory.Exists(App.CommonFileDir))
                {
                    System.IO.Directory.CreateDirectory(App.CommonFileDir);
                }

                ToucanCore.UIs.FileBrowser fb = new ToucanCore.UIs.FileBrowser(new ToucanCore.UIs.ProjectDirectory(App.CommonFileDir));
                fb.Topmost = true;
                fb.ShowDialog();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Open {App.CommonFileDir} Failed. Err: {ex}", "Error");
            }
        }

        private void MI_LockClick(object sender, RoutedEventArgs e)
        {
            Robin.IsEditMode = !Robin.IsEditMode;
            IsEditable = !IsEditable;
        }
    }

    public class ApInformation
    {
        public string Version { get; set; }
        public string Location { get; set; }
        public bool Enable { get; set; }
    }
}
