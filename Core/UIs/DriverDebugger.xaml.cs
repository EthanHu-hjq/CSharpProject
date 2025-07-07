using System;
using System.Collections.Generic;
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
using ToucanCore.Configuration;
using ToucanCore.Abstraction.HAL;
using ToucanCore.Abstraction.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

namespace ToucanCore.UIs
{
    /// <summary>
    /// Interaction logic for DriverDebugger.xaml
    /// </summary>
    public partial class DriverDebugger : Window
    {
//#if DEBUG
//        public static string[] HardwareResources { get; } = new string[]
//                { "COM1", "COM2"};
//#else
        public static string[] HardwareResources { get; } = System.IO.Ports.SerialPort.GetPortNames();

        public static List<IFixture> FixtureList = HardwareConfig.Fixtures;
        public static List<IRelayArray> RelayArrayList = HardwareConfig.RelayArrays;
        public static List<ISerialNumberReader> SerialNumberReaderList = HardwareConfig.SerialNumberReaders;
//#endif


        public IFixture FixtureType
        {
            get { return (IFixture)GetValue(FixtureTypeProperty); }
            set { SetValue(FixtureTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FixtureType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FixtureTypeProperty =
            DependencyProperty.Register("FixtureType", typeof(IFixture), typeof(DriverDebugger), new PropertyMetadata(null));

        public string ActiveResource
        {
            get { return (string)GetValue(ActiveResourceProperty); }
            set { SetValue(ActiveResourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveResource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveResourceProperty =
            DependencyProperty.Register("ActiveResource", typeof(string), typeof(DriverDebugger), new PropertyMetadata(null));

        public int SocketIndex
        {
            get { return (int)GetValue(SocketIndexProperty); }
            set { SetValue(SocketIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SocketIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SocketIndexProperty =
            DependencyProperty.Register("SocketIndex", typeof(int), typeof(DriverDebugger), new PropertyMetadata(0));


        public IFixture ActiveFixture
        {
            get { return (IFixture)GetValue(ActiveFixtureProperty); }
            set { SetValue(ActiveFixtureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveFixture.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveFixtureProperty =
            DependencyProperty.Register("ActiveFixture", typeof(IFixture), typeof(DriverDebugger), new PropertyMetadata(null));

        public bool? FrontDoorOpenned
        {
            get { return (bool?)GetValue(FrontDoorOpennedProperty); }
            set { SetValue(FrontDoorOpennedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FrontDoorOpenned.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrontDoorOpennedProperty =
            DependencyProperty.Register("FrontDoorOpenned", typeof(bool?), typeof(DriverDebugger), new PropertyMetadata(null));

        public bool? FrontDoorClosed
        {
            get { return (bool?)GetValue(FrontDoorClosedProperty); }
            set { SetValue(FrontDoorClosedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FrontDoorClosed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrontDoorClosedProperty =
            DependencyProperty.Register("FrontDoorClosed", typeof(bool?), typeof(DriverDebugger), new PropertyMetadata(null));

        public bool? DutInDone
        {
            get { return (bool?)GetValue(DutInDoneProperty); }
            set { SetValue(DutInDoneProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutInDone.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutInDoneProperty =
            DependencyProperty.Register("DutInDone", typeof(bool?), typeof(DriverDebugger), new PropertyMetadata(null));

        public bool? DutOutDone
        {
            get { return (bool?)GetValue(DutOutDoneProperty); }
            set { SetValue(DutOutDoneProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutOutDone.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutOutDoneProperty =
            DependencyProperty.Register("DutOutDone", typeof(bool?), typeof(DriverDebugger), new PropertyMetadata(null));

        public bool? DutPresent
        {
            get { return (bool?)GetValue(DutPresentProperty); }
            set { SetValue(DutPresentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutPresent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutPresentProperty =
            DependencyProperty.Register("DutPresent", typeof(bool?), typeof(DriverDebugger), new PropertyMetadata(null));

        public bool? Safety
        {
            get { return (bool?)GetValue(SafetyProperty); }
            set { SetValue(SafetyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Safety.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SafetyProperty =
            DependencyProperty.Register("Safety", typeof(bool?), typeof(DriverDebugger), new PropertyMetadata(null));




        public IRelayArray ActiveRelayArray
        {
            get { return (IRelayArray)GetValue(ActiveRelayArrayProperty); }
            set { SetValue(ActiveRelayArrayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveRelayArray.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveRelayArrayProperty =
            DependencyProperty.Register("ActiveRelayArray", typeof(IRelayArray), typeof(DriverDebugger), new PropertyMetadata(null, ActiveRelayArrayChanged));

        private static void ActiveRelayArrayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DriverDebugger self)
            {
                if (e.OldValue is IRelayArray oldra)
                {
                    oldra.Close();
                    oldra.Clear();
                }

                if (e.NewValue is IRelayArray ra)
                {
                    self.ug_RelayArray.Children.Clear();
                    for (int i = 0; i < ra.ChannelCount; i++)
                    {
                        var btn = new Button() { Content = $"{i}", CommandParameter = i, DataContext = self };
                        btn.Click += RelayArrayItemClick;
                        self.ug_RelayArray.Children.Add(btn);
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
            DependencyProperty.Register("ActiveSnReader", typeof(ISerialNumberReader), typeof(DriverDebugger), new PropertyMetadata(null));

        public string ContentSnRead
        {
            get { return (string)GetValue(ContentSnReadProperty); }
            set { SetValue(ContentSnReadProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ContentSnRead.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ContentSnReadProperty =
            DependencyProperty.Register("ContentSnRead", typeof(string), typeof(DriverDebugger), new PropertyMetadata(null));





        private static void RelayArrayItemClick(object sender, RoutedEventArgs e)
        {
            if(sender is Button btn)
            {
                if (btn.CommandParameter is int index && btn.DataContext is DriverDebugger dd)
                {
                    if (btn.Background == Brushes.Red)
                    {
                        var val = 1 << index;
                        dd.ActiveRelayArray?.SetRelay(0, val);
                        btn.Background = Brushes.White;
                    }
                    else
                    {
                        var val = 1 << index;
                        dd.ActiveRelayArray?.SetRelay(val, val);
                        btn.Background = Brushes.Red;
                    }
                    

                    
                }
            }
        }

        public DelegateCommand OpenFrontDoor { get; }
        public DelegateCommand CloseFrontDoor { get; }
        public DelegateCommand OpenRearDoor { get; }
        public DelegateCommand CloseRearDoor { get; }
        public DelegateCommand DutIn { get; }
        public DelegateCommand DutOut { get; }
        public DelegateCommand LoadDut { get; }
        public DelegateCommand UnloadDut { get; }

        public DelegateCommand ReadSn { get; }

        public DelegateCommand OpenFixture { get; }
        public DelegateCommand OpenRelayArray { get; }
        public DelegateCommand OpenSnReader { get; }
        public DelegateCommand RefreshState { get; }

        Dictionary<string, IFixture> Fixtures { get; }

        public DriverDebugger()
        {
            RefreshState = new DelegateCommand((_) => { UpdateFixtureState(); });

            OpenFrontDoor = new DelegateCommand(cmd_OpenFrontDoor);
            CloseFrontDoor = new DelegateCommand((_) => { ActiveFixture?.CloseFrontDoor(); UpdateFixtureState(); });
            OpenRearDoor = new DelegateCommand((_) => { ActiveFixture?.OpenRearDoor(); UpdateFixtureState(); });
            CloseRearDoor = new DelegateCommand((_) => { ActiveFixture?.CloseRearDoor(); UpdateFixtureState(); });
            DutIn = new DelegateCommand((_) => { ActiveFixture?.DutIn(); UpdateFixtureState(); });
            DutOut = new DelegateCommand((_) => { ActiveFixture?.DutOut(); UpdateFixtureState(); });

            

            OpenFixture = new DelegateCommand(cmd_OpenFixture);
            OpenRelayArray = new DelegateCommand(cmd_OpenRelayArray);
            OpenSnReader = new DelegateCommand(cmd_OpenSnReader);

            ReadSn = new DelegateCommand(cmd_ReadSn);

            Fixtures = new Dictionary<string, IFixture>();
            foreach (var rcs in HardwareResources)
            {
                Fixtures.Add(rcs, null);
            }

            InitializeComponent();
        }

        private void cmd_OpenFrontDoor(object obj)
        {
            ActiveFixture?.OpenFrontDoor(); UpdateFixtureState();
        }

        private void cmd_OpenFixture(object obj)
        {
            if (string.IsNullOrEmpty(ActiveResource)) return;

            try
            {
                if (Fixtures.ContainsKey(ActiveResource))
                {
                    if (Fixtures[ActiveResource] is null)
                    {
                        ActiveFixture = Fixtures[ActiveResource] = Activator.CreateInstance(FixtureType.GetType()) as IFixture;
                        ActiveFixture.Resource = ActiveResource;
                    }
                    else
                    {
                        if (Fixtures[ActiveResource].GetType() == FixtureType.GetType())
                        {
                            ActiveFixture = Fixtures[ActiveResource];
                        }
                        else
                        {
                            Fixtures[ActiveResource]?.Close();
                            Fixtures[ActiveResource]?.Clear();

                            ActiveFixture = Fixtures[ActiveResource] = Activator.CreateInstance(FixtureType.GetType()) as IFixture;
                            ActiveFixture.Resource = ActiveResource;
                        }
                    }

                    if (ActiveFixture.IsInitialized)
                    {
                        ActiveFixture.Close();
                        ActiveFixture.Clear();
                    }
                    else
                    {
                        ActiveFixture.Initialize();
                        ActiveFixture.Open();
                        UpdateFixtureState();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show($"Open Fixture for {FixtureType.GetType().Name} in {ActiveResource} failed. Err: {ex}", "Warning");
            }
        }

        private void cmd_OpenRelayArray(object obj)
        {
            if(ActiveRelayArray is IRelayArray ra && ActiveResource is string rcs)
            {
                if(!ra.IsInitialized) { ra.Resource = rcs; ra.Initialize(); }
                if(!ra.IsOpen) { ra.Open(); }
            }
        }

        private void cmd_ReadSn(object obj)
        {
            if (ActiveSnReader is ISerialNumberReader sr)
            {
                ContentSnRead = null;
                
                if (!sr.IsInitialized) { sr.Initialize(); }
                if (!sr.IsOpen) { sr.Open(); }

                ContentSnRead = sr.ReadSerialNumber();
            }
        }

        private void cmd_OpenSnReader(object obj)
        {
            if (ActiveSnReader is ISerialNumberReader sr && ActiveResource is string rcs)
            {
                if (!sr.IsInitialized) { sr.Resource = rcs; sr.Initialize(); }
                if (!sr.IsOpen) { sr.Open(); }
            }
        }

        private void UpdateFixtureState()
        {
            if (ActiveFixture?.IsInitialized == true)
            {
                bool state = false;
                if (ActiveFixture.GetStateFrontDoorOpen(out state, SocketIndex) > 0)
                {
                    FrontDoorOpenned = state;
                }
                if (ActiveFixture.GetStateFrontDoorClose(out state, SocketIndex) > 0)
                {
                    FrontDoorClosed = state;
                }
                if (ActiveFixture.GetStateDutPresent(out state, SocketIndex) > 0)
                {
                    DutPresent = state;
                }
                if (ActiveFixture.GetStateDutIn(out state, SocketIndex) > 0)
                {
                    DutInDone = state;
                }
                if (ActiveFixture.GetStateDutOut(out state, SocketIndex) > 0)
                {
                    DutOutDone = state;
                }
                if (ActiveFixture.GetStateSafety(out state, SocketIndex) > 0)
                {
                    Safety = state;
                }
            }
        }
    }
}
