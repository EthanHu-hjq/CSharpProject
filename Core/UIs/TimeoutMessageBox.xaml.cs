using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ToucanCore.UIs
{
    /// <summary>
    /// Interaction logic for TimeoutMessageBox.xaml
    /// </summary>
    public partial class TimeoutMessageBox : Window
    {
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(TimeoutMessageBox), new PropertyMetadata(null));



        System.Windows.Threading.DispatcherTimer timer1;
        public int Timeout_ms { get; set; } = 30000;
        DateTime T0;
        public delegate bool ElapsedHandler(out string msg);
        public ElapsedHandler ElapsedAction { get;  set; }
        public TimeoutMessageBox()
        {
            InitializeComponent();

            timer1 = new System.Windows.Threading.DispatcherTimer();
            timer1.Interval = new TimeSpan(1000);
            timer1.Tick += Timer1_Tick; ;
            Closed += TimeoutMessageBox_Closed;
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            if (Timeout_ms > 0 && DateTime.Now.Subtract(T0).TotalMilliseconds > Timeout_ms)
            {
                Close();
            }
            else
            {
                Message = string.Format("Elapsed {0} ms\r\n", DateTime.Now.Subtract(T0).TotalMilliseconds);

                if (ElapsedAction is null) { }
                else
                {
                    var rs = ElapsedAction.Invoke(out string dd);

                    if (rs == true)
                    {
                        Close();
                    }
                    else
                    {
                        Message += $"{dd ?? ""}\r\nPlease Wait";
                    }
                }
            }
        }

        private void TimeoutMessageBox_Closed(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        public new bool? ShowDialog()
        {
            T0 = DateTime.Now;
            timer1.Start();
            return base.ShowDialog();
        }
    }
}
