using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using ToucanCore.Abstraction.Engine;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// GoldenSampleManager.xaml 的交互逻辑
    /// </summary>
    public partial class GoldenSampleManager : Window
    {
        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsEditable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(GoldenSampleManager), new PropertyMetadata(false));



        public ObservableCollection<Sample> Samples
        {
            get { return (ObservableCollection<Sample>)GetValue(SamplesProperty); }
            set { SetValue(SamplesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Samples.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SamplesProperty =
            DependencyProperty.Register("Samples", typeof(ObservableCollection<Sample>), typeof(GoldenSampleManager), new PropertyMetadata(null));

        private IScriptPro script;
        public GoldenSampleManager()
        {
            InitializeComponent();
            Samples = new ObservableCollection<Sample>();
        }

        public GoldenSampleManager(IScriptPro script) : this()
        {
            foreach(var i in script.GoldenSamples)
            {
                Samples.Add(new Sample(i));
            }
            this.script = script;
            IsEditable = !script.LockStatus;
        }

        private void btn_Save_Click(object sender, RoutedEventArgs e)
        {
            script?.UpdateGoldenSamples(Samples.Select(x=> x.SerialNumber));
            MessageBox.Show("Golden Sample File Saved");
        }
    }

    public class Sample
    {
        public string SerialNumber { get; set; }

        public Sample() { }
        public Sample(string serialNumber) :this() { SerialNumber = serialNumber; }
    }
}
