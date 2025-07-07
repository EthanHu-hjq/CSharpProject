using Robin.UIs;
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

namespace Robin.Ctrls
{
    /// <summary>
    /// Interaction logic for ApSequenceExtended.xaml
    /// </summary>
    public partial class ApSequenceExtended : UserControl
    {
        public static string[] dBrG_Names = new string[] { "dbrg1", "dbrg2" };
        public static string[] MicCal_Names = new string[] { "dbrg1", "dbrg2" };
        public static string[] OutputEq_Names = new string[] { "dbrg1", "dbrg2" };
        public static string[] SenseR_Names = new string[] { "dbrg1", "dbrg2" };

        public static string[] DataExportSpec_Names = new string[] { "dbrg1", "dbrg2" };

        public static string[] AuxOut_Names = new string[] { "dbrg1", "dbrg2" };

        public static string[] ChannelNames = new string[] { "Ch1", "Ch2", "Ch3", "Ch4", "Ch5", "Ch6", "Ch7", "Ch8", "Ch9", "Ch10", "Ch11", "Ch12", "Ch13", "Ch14", "Ch15", "Ch16", };

        public string dBrG
        {
            get { return (string)GetValue(dBrGProperty); }
            set { SetValue(dBrGProperty, value); }
        }

        // Using a DependencyProperty as the backing store for dBrG.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty dBrGProperty =
            DependencyProperty.Register("dBrG", typeof(string), typeof(ApSequenceExtended), new PropertyMetadata(null));

        public string MicCal
        {
            get { return (string)GetValue(MicCalProperty); }
            set { SetValue(MicCalProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MicCal.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MicCalProperty =
            DependencyProperty.Register("MicCal", typeof(string), typeof(ApSequenceExtended), new PropertyMetadata(null));

        public string SenseR
        {
            get { return (string)GetValue(SenseRProperty); }
            set { SetValue(SenseRProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SenseR.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SenseRProperty =
            DependencyProperty.Register("SenseR", typeof(string), typeof(ApSequenceExtended), new PropertyMetadata(null));

        public string AuxOut
        {
            get { return (string)GetValue(AuxOutProperty); }
            set { SetValue(AuxOutProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AuxOut.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AuxOutProperty =
            DependencyProperty.Register("AuxOut", typeof(string), typeof(ApSequenceExtended), new PropertyMetadata(null));

        public string OutputEq
        {
            get { return (string)GetValue(OutputEqProperty); }
            set { SetValue(OutputEqProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OutputEq.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OutputEqProperty =
            DependencyProperty.Register("OutputEq", typeof(string), typeof(ApSequenceExtended), new PropertyMetadata(null));


        public ApSequenceExtended()
        {
            InitializeComponent();
        }
        private void btn_SelectChannels_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
