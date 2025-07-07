using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
using ToucanCore.Abstraction.HAL;

namespace ToucanCore.Driver
{
    /// <summary>
    /// Interaction logic for FixtureDemoSimulator.xaml
    /// </summary>
    public partial class FixtureManipulator : Window
    {
        IFixture Fixture;

        public bool FrontDoorOpened
        {
            get { return (bool)GetValue(FrontDoorOpenedProperty); }
            set { SetValue(FrontDoorOpenedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FrontDoorOpened.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrontDoorOpenedProperty =
            DependencyProperty.Register("FrontDoorOpened", typeof(bool), typeof(FixtureManipulator), new PropertyMetadata(false));

        public bool FrontDoorClosed
        {
            get { return (bool)GetValue(FrontDoorClosedProperty); }
            set { SetValue(FrontDoorClosedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for FrontDoorClosed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FrontDoorClosedProperty =
            DependencyProperty.Register("FrontDoorClosed", typeof(bool), typeof(FixtureManipulator), new PropertyMetadata(true));

        public bool DutIned
        {
            get { return (bool)GetValue(DutInedProperty); }
            set { SetValue(DutInedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutIned.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutInedProperty =
            DependencyProperty.Register("DutIned", typeof(bool), typeof(FixtureManipulator), new PropertyMetadata(true));

        public bool DutOuted
        {
            get { return (bool)GetValue(DutOutedProperty); }
            set { SetValue(DutOutedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DutOuted.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DutOutedProperty =
            DependencyProperty.Register("DutOuted", typeof(bool), typeof(FixtureManipulator), new PropertyMetadata(false));

        public int ActiveIndex
        {
            get { return (int)GetValue(ActiveIndexProperty); }
            set { SetValue(ActiveIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ActiveIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActiveIndexProperty =
            DependencyProperty.Register("ActiveIndex", typeof(int), typeof(FixtureManipulator), new PropertyMetadata(0, ActiveIndexChanged));

        private static void ActiveIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //if(d is FixtureManipulator sim)
            //{
            //    if (sim.Fixture is IAutoActiveSlotFixture fixture)
            //    {
            //        MessageBox.Show($"You ARE trying to active the socket {e.NewValue}, which is supposed to be actived by it self", "CAUTION");
            //    }
            //}
        }

        public FixtureManipulator()
        {
            InitializeComponent();
            DataContextChanged += FixtureDemoSimulator_DataContextChanged;
            Closing += FixtureManipulator_Closing;
        }

        private void FixtureManipulator_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(sender is FixtureManipulator)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void FixtureDemoSimulator_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue is IAutoActiveSlotFixture fixture)
            {
                if (fixture == Fixture) return;
                Fixture = fixture;
                ActiveIndex = fixture.ActiveSocketIndex;

                Fixture.FrontDoorOpened += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = true;
                        FrontDoorClosed = false;
                        ActiveIndex = fixture.ActiveSocketIndex;
                    });
                };

                Fixture.FrontDoorOpening += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = false;
                        FrontDoorClosed = false;
                    });
                };

                Fixture.FrontDoorClosing += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = false;
                        FrontDoorClosed = false;
                    });
                };

                Fixture.FrontDoorClosed += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = false;
                        FrontDoorClosed = true;
                        ActiveIndex = fixture.ActiveSocketIndex;
                    });
                };

                Fixture.DutInDone += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DutOuted = false;
                        DutIned = true;
                        ActiveIndex = fixture.ActiveSocketIndex;
                    });
                };

                Fixture.DutOuted += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DutOuted = true;
                        DutIned = false;
                        ActiveIndex = fixture.ActiveSocketIndex;
                    });
                };
            }
            else if (e.NewValue is IFixture fixture1)
            {
                if (fixture1 == Fixture) return;
                Fixture = fixture1;

                Fixture.FrontDoorOpened += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = true;
                        FrontDoorClosed = false;
                    });
                };

                Fixture.FrontDoorOpening += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = false;
                        FrontDoorClosed = false;
                    });
                };

                Fixture.FrontDoorClosing += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = false;
                        FrontDoorClosed = false;
                    });
                };

                Fixture.FrontDoorClosed += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        FrontDoorOpened = false;
                        FrontDoorClosed = true;
                    });
                };

                Fixture.DutInDone += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DutOuted = false;
                        DutIned = true;
                    });
                };

                Fixture.DutOuted += (sd, msg) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        DutOuted = true;
                        DutIned = false;
                    });
                };
            }
        }

        private void btn_OpenFrontDoor_Click(object sender, RoutedEventArgs e)
        {
            Fixture.OpenFrontDoor(ActiveIndex);
        }

        private void btn_CloseFrontDoor_Click(object sender, RoutedEventArgs e)
        {
            Fixture.CloseFrontDoor(ActiveIndex);
        }

        private void btn_DutIn_Click(object sender, RoutedEventArgs e)
        {
            Fixture.DutIn(ActiveIndex);
        }

        private void btn_DutOut_Click(object sender, RoutedEventArgs e)
        {
            Fixture.DutOut(ActiveIndex);
        }
    }
}
