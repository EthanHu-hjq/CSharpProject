using ApEngine.Base;
using Microsoft.Win32;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ApEngine.UIs
{
    /// <summary>
    /// Interaction logic for Reference.xaml
    /// </summary>
    public partial class Reference : Window
    {
        public ApReferenceData ApReferenceData { get; private set; }
        public Script Script { get; }

        public ObservableCollection<string> ExportFiles
        {
            get { return (ObservableCollection<string>)GetValue(ExportFilesProperty); }
            set { SetValue(ExportFilesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ExportFiles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ExportFilesProperty =
            DependencyProperty.Register("ExportFiles", typeof(ObservableCollection<string>), typeof(Reference), new PropertyMetadata(new ObservableCollection<string>()));

        public ObservableCollection<string> ImportFiles
        {
            get { return (ObservableCollection<string>)GetValue(ImportFilesProperty); }
            set { SetValue(ImportFilesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImportFiles.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImportFilesProperty =
            DependencyProperty.Register("ImportFiles", typeof(ObservableCollection<string>), typeof(Reference), new PropertyMetadata(new ObservableCollection<string>()));

        public ObservableCollection<string> Samples
        {
            get { return (ObservableCollection<string>)GetValue(SamplesProperty); }
            set { SetValue(SamplesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Samples.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SamplesProperty =
            DependencyProperty.Register("Samples", typeof(ObservableCollection<string>), typeof(Reference), new PropertyMetadata(new ObservableCollection<string>()));



        public int ValidDay
        {
            get { return (int)GetValue(ValidDayProperty); }
            set { SetValue(ValidDayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidDay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidDayProperty =
            DependencyProperty.Register("ValidDay", typeof(int), typeof(Reference), new PropertyMetadata(0));

        public int ValidHour
        {
            get { return (int)GetValue(ValidHourProperty); }
            set { SetValue(ValidHourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ValidHour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValidHourProperty =
            DependencyProperty.Register("ValidHour", typeof(int), typeof(Reference), new PropertyMetadata(0));

        public int WarnDay
        {
            get { return (int)GetValue(WarnDayProperty); }
            set { SetValue(WarnDayProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarnDay.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarnDayProperty =
            DependencyProperty.Register("WarnDay", typeof(int), typeof(Reference), new PropertyMetadata(0));

        public int WarnHour
        {
            get { return (int)GetValue(WarnHourProperty); }
            set { SetValue(WarnHourProperty, value); }
        }

        // Using a DependencyProperty as the backing store for WarnHour.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WarnHourProperty =
            DependencyProperty.Register("WarnHour", typeof(int), typeof(Reference), new PropertyMetadata(0));

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Version.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(Reference), new PropertyMetadata("0.1"));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Message.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(Reference), new PropertyMetadata(null));

        public Reference()
        {
            InitializeComponent();
        }
        public Reference(Script script) : this()
        {
            Script = script;

            var refpath = System.IO.Path.Combine(ApxEngine.ReferenceBase, $"{script.StationConfig.CustomerName}_{script.StationConfig.ProductName}_{script.StationConfig.StationName}{ApReferenceData.FileExt}");

            if (ApReferenceData.FromFile(refpath) is ApReferenceData data)
            {
                LoadReferenceData(data);
            }
            else
            {
                LoadReferenceData(new ApReferenceData());
            }
        }

        private void btn_Open_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = $"PTE Reference Data|*{ApReferenceData.FileExt}";

                if (ofd.ShowDialog() == true)
                {
                    if (ApReferenceData.FromFile(ofd.FileName) is ApReferenceData data)
                    {
                        LoadReferenceData(data);
                    }
                }
            }
            catch
            {
                Message = "Open Reference Data Failed";
            }
        }

        private void SaveReference_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ApReferenceData.CurrentDir is null)
                {
                    SaveFileDialog sfd = new SaveFileDialog();

                    sfd.Filter = $"PTE Reference Data|*{ApReferenceData.FileExt}";
                    if (Script != null)
                    {
                        sfd.FileName = $"{Script.StationConfig.CustomerName}_{Script.StationConfig.ProductName}_{Script.StationConfig.StationName}{ApReferenceData.FileExt}";
                    }

                    if (sfd.ShowDialog() == true)
                    {
                        ApReferenceData.Save(sfd.FileName);
                    }
                }
            }
            catch
            {
                Message = "Save Reference Data Failed";
            }
        }

        private void LoadReferenceData(ApReferenceData data)
        {
            ApReferenceData = data;

            ValidHour = data.ValidTime.Hours;
            ValidDay = data.ValidTime.Days;

            WarnDay = data.WarnTime.Days;
            WarnHour = data.WarnTime.Hours;
            Version = ApReferenceData.Version;

            Samples.Clear();
            foreach (var sn in data.Samples) Samples.Add(sn);
        }
    }
}
