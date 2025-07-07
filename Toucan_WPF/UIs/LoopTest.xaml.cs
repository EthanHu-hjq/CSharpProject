using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TestCore;
using Toucan_WPF.ViewModels;
using ToucanCore.Engine;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// Interaction logic for LoopTest.xaml
    /// </summary>
    public partial class LoopTest : Window
    {
        public event EventHandler LoopTestStart;
        public event EventHandler LoopTestStop;

        public static int DelayMs = 100;

        public int Delay_ms
        {
            get { return (int)GetValue(Delay_msProperty); }
            set { SetValue(Delay_msProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Delay_ms.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Delay_msProperty =
            DependencyProperty.Register("Delay_ms", typeof(int), typeof(LoopTest), new PropertyMetadata(DelayMs));

        public int LoopTimes
        {
            get { return (int)GetValue(LoopTimesProperty); }
            set { SetValue(LoopTimesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoopTimes.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoopTimesProperty =
            DependencyProperty.Register("LoopTimes", typeof(int), typeof(LoopTest), new PropertyMetadata(10));

        public bool IsTesting
        {
            get { return (bool)GetValue(IsTestingProperty); }
            set { SetValue(IsTestingProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsTesting.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsTestingProperty =
            DependencyProperty.Register("IsTesting", typeof(bool), typeof(LoopTest), new PropertyMetadata(false));

        public string SerialNumber
        {
            get { return (string)GetValue(SerialNumberProperty); }
            set { SetValue(SerialNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNumberProperty =
            DependencyProperty.Register("SerialNumber", typeof(string), typeof(LoopTest), new PropertyMetadata(null));



        public bool StopWhenFailed
        {
            get { return (bool)GetValue(StopWhenFailedProperty); }
            set { SetValue(StopWhenFailedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StopWhenFailed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StopWhenFailedProperty =
            DependencyProperty.Register("StopWhenFailed", typeof(bool), typeof(LoopTest), new PropertyMetadata(false));


        //LoopSlot[] Slots { get; }
        public LoopSlot[] Slots
        {
            get { return (LoopSlot[])GetValue(SlotsProperty); }
            set { SetValue(SlotsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Slots.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SlotsProperty =
            DependencyProperty.Register("Slots", typeof(LoopSlot[]), typeof(LoopTest), new PropertyMetadata(null));

        public DelegateCommand StartLoopTest { get; }

        VM_Execution Execution { get; }
        private int[] TestTimes;

        public LoopTest(VM_Execution execution)
        {
            StartLoopTest = new DelegateCommand(cmd_StartLoopTest);
            
            Execution = execution;
            TestTimes = new int[execution.Execution.SocketCount];

            Slots = new LoopSlot[execution.Execution.SocketCount];
            for (int i = 0; i < execution.Execution.SocketCount; i++)
            {
                var rs = execution.Execution.Results[i];
                if (rs.AttachProperties.ContainsKey("Tag"))
                {
                    rs.AttachProperties["Tag"] = "Loop";
                }
                else
                {
                    rs.AttachProperties.Add("Tag", "Loop");
                }

                Slots[i] = new LoopSlot() { SocketIndex = i, SerialNumber = execution.Slots[i].SerialNumber, IsSFC = rs.IsSFC };
                Slots[i].TestStartTimer.Elapsed += TestStartTimer_Elapsed;
                Slots[i].TestStartTimer.AutoReset = false;
                rs.IsSFC = false;
            }

            InitializeComponent();

            Closed += LoopTest_Closed;
        }

        private void TestStartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(sender is System.Timers.Timer timer)
            {
                Dispatcher.Invoke(() =>
                {
                    var slot = Slots.First(x => x.TestStartTimer == timer);
                    slot.TestStartTimer.Stop();
                    Execution.UpdateDutSn.Execute(new Tuple<int, string>(slot.SocketIndex, slot.SerialNumber));
                    
                });
            }            
        }

        private void LoopTest_Closed(object sender, EventArgs e)
        {
            if (IsTesting)
            {
                Execution.Execution.OnPostUUTed -= Execution_OnPostUUTed;
            }

            for (int i = 0; i < Execution.Execution.SocketCount; i++)
            {
                var rs = Execution.Execution.Results[i];
                rs.IsSFC = Slots[i].IsSFC;
                if (rs.AttachProperties.ContainsKey("Tag"))
                {
                    rs.AttachProperties["Tag"] = null;
                }
                else
                {
                    rs.AttachProperties.Add("Tag", null);
                }
            }

            DelayMs = Delay_ms;
        }

        private void cmd_StartLoopTest(object obj)
        {
            if (!IsTesting)
            {
                if(LoopTimes < 0)
                {
                    MessageBox.Show("Loop Time Should be more than 0", "Error");
                    return;
                }
                else if(LoopTimes > 100)
                {
                    if(MessageBox.Show($"Loop Times {LoopTimes}, Are you sure", "Warning", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }
            }

            IsTesting = !IsTesting;
            if (IsTesting)
            {
                //Execution.UpdateDutSn.Execute(SerialNumber);
                for (int i = 0; i < Execution.Execution.SocketCount; i++)
                {
                    TestTimes[i] = 1;
                    Slots[i].TotalCount = Slots[i].PassCount = 0;
                    Execution.UpdateDutSn.Execute(new Tuple<int, string>(i, Slots[i].SerialNumber));
                    Slots[i].TestStartTimer.Interval = Delay_ms;
                    Slots[i].TestStartTimer.Stop();
                }

                // If do so, the async report process in TestStand might get impredictable data update. for the new test will before the last report process
                Execution.Execution.OnPostUUTed += Execution_OnPostUUTed;

            }
        }

        private void Execution_OnPostUUTed(object sender, TestCore.Data.TF_Result e)
        {
            Dispatcher.Invoke(()=>
            {
                Slots[e.SocketIndex].TotalCount += 1;

                if(e.Status == TF_TestStatus.PASSED) Slots[e.SocketIndex].PassCount += 1;

                TestTimes[e.SocketIndex] += 1;  // Start from 1;

                if (LoopTimes > 0 && TestTimes[e.SocketIndex] > LoopTimes) //
                {
                    //IsTesting = false;
                    Slots[e.SocketIndex].TestStartTimer.Stop();

                    if (TestTimes.All(x => x >= LoopTimes))
                    {
                        IsTesting = false;
                    }
                }
                else if(e.Result == TF_ItemStatus.Failed && StopWhenFailed)
                {
                    IsTesting = false;

                    foreach(var slot in Slots)
                    {
                        slot.TestStartTimer.Stop();
                    }
                }
                else
                {
                    Slots[e.SocketIndex].TestStartTimer.Start();
                }

                if (!IsTesting)
                {
                    Execution.Execution.OnPostUUTed -= Execution_OnPostUUTed;
                    
                    return;  // stop loop
                } 
            
                

                //Task.Run(() =>
                //{
                //    System.Threading.Thread.Sleep(Delay_ms);
                //    Dispatcher.Invoke(() =>
                //    {
                //        Execution.UpdateDutSn.Execute(Slots[e.SocketIndex].SerialNumber);
                //    }
                //    );

                //}
                //);

                //System.Threading.Thread.Sleep(Delay_ms);
                //Execution.UpdateDutSn.Execute(new Tuple<int, string>(e.SocketIndex, Slots[e.SocketIndex].SerialNumber));
                
            });
        }
    }

    public class LoopSlot : DependencyObject
    {
        public int SocketIndex { get; set; }
        public string SerialNumber { get; set; }

        public bool IsSFC { get; set; }

        internal System.Timers.Timer TestStartTimer { get; } = new System.Timers.Timer();

        public int TotalCount
        {
            get { return (int)GetValue(TotalCountProperty); }
            set { SetValue(TotalCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TotalCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TotalCountProperty =
            DependencyProperty.Register("TotalCount", typeof(int), typeof(LoopSlot), new PropertyMetadata(0));

        public int PassCount
        {
            get { return (int)GetValue(PassCountProperty); }
            set { SetValue(PassCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PassCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PassCountProperty =
            DependencyProperty.Register("PassCount", typeof(int), typeof(LoopSlot), new PropertyMetadata(0));
    }
}
