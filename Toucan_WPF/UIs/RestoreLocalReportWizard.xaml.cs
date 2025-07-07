using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TestCore;
using TestCore.Data;
using TestCore.Services;
using ToucanCore.Engine;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// RestoreLocalReportIntoRemote.xaml 的交互逻辑
    /// </summary>
    public partial class RestoreLocalReportWizard : Window
    {
        public IReportService ReportService
        {
            get { return (IReportService)GetValue(ReportServiceProperty); }
            set { SetValue(ReportServiceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ReportService.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ReportServiceProperty =
            DependencyProperty.Register("ReportService", typeof(IReportService), typeof(RestoreLocalReportWizard), new PropertyMetadata(null));

        public IEnumerable<CheckData<string>> SubFolders
        {
            get { return (IEnumerable<CheckData<string>>)GetValue(SubFoldersProperty); }
            set { SetValue(SubFoldersProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SubFolders.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SubFoldersProperty =
            DependencyProperty.Register("SubFolders", typeof(IEnumerable<CheckData<string>>), typeof(RestoreLocalReportWizard), new PropertyMetadata(null));



        public string RootFolder
        {
            get { return (string)GetValue(RootFolderProperty); }
            set { SetValue(RootFolderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RootFolder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RootFolderProperty =
            DependencyProperty.Register("RootFolder", typeof(string), typeof(RestoreLocalReportWizard), new PropertyMetadata(null));

        public RestoreLocalReportWizard()
        {
            InitializeComponent();
        }

        public RestoreLocalReportWizard(IReportService service, string rootfolder) : this()
        {
            ReportService = service;
            RootFolder = rootfolder;
            AnalyzeFolder();
        }

        static Regex RE_TimeFolder = new Regex(@"^(\d+[-\s]\d+[-\s]\d+)");

        private void AnalyzeFolder()
        {
            if (Directory.Exists(RootFolder))
            {
                var subfolders = Directory.GetDirectories(RootFolder);

                List<CheckData<string>> temp = new List<CheckData<string>>();
                foreach (var sub in subfolders)
                {
                    if(RE_TimeFolder.IsMatch(System.IO.Path.GetFileName(sub)))
                    {
                        temp.Add(new CheckData<string>() { Data = sub, IsChecked = true });
                    }
                }

                if(temp.Count == 0)   // not the format folder
                {
                    var files = Directory.GetFiles(RootFolder);
                    temp.Add(new CheckData<string>() { Data = RootFolder, IsChecked = true });
                }

                SubFolders = temp;
            }
        }

        private void btn_SelectDir_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
            fbd.SelectedPath = RootFolder;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                RootFolder = fbd.SelectedPath;

                AnalyzeFolder();
            }
        }

        private void btn_Upload_Click(object sender, RoutedEventArgs e)
        {
            if (SubFolders is null) return;

            List<TF_Result> rss = new List<TF_Result>();
            foreach(var sub in SubFolders)
            {
                if(sub.IsChecked)
                {
                    var files = Directory.GetFiles(sub.Data, "*.*", SearchOption.AllDirectories);

                    TF_Result rs = null;

                    List<string> unmatcheds = new List<string>();
                    foreach (var file in files)
                    {
                        var fname = System.IO.Path.GetFileName(file);
                        if (GetMetaFromRecordName(fname, out string sn, out string customer, out string product, out string station, out string stationid, out string slotid, out DateTime datetime, out bool issfc, out TF_TestStatus status, out string[] tags))
                        {
                            if (sn.Length <= 2) continue;

                            TF_Result result = new TF_Result()
                            {
                                SerialNumber = sn,
                                StationConfig = new TestCore.Configuration.StationConfig("", customer, product, product, station, stationid),
                                SFCsConfig = new TestCore.Configuration.SFCsConfig(true, "", false),
                                EndTime = datetime,
                                IsSFC = issfc,
                                SocketId = slotid,
                                Status = status,
                                RawFile = file,
                            };
                            rss.Add(result);

                            rs = result;
                        }
                        else
                        {
                            if (rs != null)
                            {
                                var appendix = rs.Clone() as TF_Result;
                                appendix.RawFile = file;
                                rss.Add(appendix);
                            }
                        }
                    }
                }
            }

            if (System.Windows.MessageBox.Show($"{rss.Count} in {RootFolder} will be upload, Are you sure", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                foreach(var rs in rss)
                {
                    ReportService.Push(rs, rs.RawFile);
                }
            }
        }

        static Lazy<Regex> RE_RecordName = new Lazy<Regex>(() => { return new Regex(@"^(\[.+\])*(.+)_(\w+)_([\w|-]+)_(.+)_(\d{14})_(\w{1,2})(\w+)(\w{1})(\..+)"); });
        /// <summary>
        /// this is from TestCore, for compatibility, implement one here
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="sn">Any charactor</param>
        /// <param name="customer">only word</param>
        /// <param name="product">only word and -</param>
        /// <param name="station">any word</param>
        /// <param name="stationid">2 charactor, 00-99,A0-ZZ</param>
        /// <param name="slotid">1 or more charactor, 0-9, 10-99, A0-ZZ</param>
        /// <param name="datetime"></param>
        /// <param name="issfc"></param>
        /// <param name="status">Pass, Fail, Error</param>
        public static bool GetMetaFromRecordName(string filename, out string sn, out string customer, out string product, out string station, out string stationid, out string slotid, out DateTime datetime, out bool issfc, out TF_TestStatus status, out string[] tags)
        {
            sn = null;
            customer = null;
            product = null;
            station = null;
            stationid = null;
            slotid = null;
            datetime = default(DateTime);
            issfc = false;
            status = TF_TestStatus.NULL;
            tags = new string[0];

            var match = RE_RecordName.Value.Match(filename);
            if (match.Success)
            {
                var attachstr = match.Groups[1].Value;

                if (!string.IsNullOrEmpty(attachstr))
                {
                    tags = attachstr.Substring(1, attachstr.Length - 2).Split(new string[] { "][" }, StringSplitOptions.None);
                }


                sn = match.Groups[2].Value;
                customer = match.Groups[3].Value;
                product = match.Groups[4].Value;
                station = match.Groups[5].Value;

                datetime = DateTime.ParseExact(match.Groups[6].Value, "yyyyMMddHHmmss", null);
                stationid = match.Groups[7].Value;
                slotid = match.Groups[8].Value;

                switch (match.Groups[9].Value)
                {
                    case "P":
                        issfc = true;
                        status = TF_TestStatus.PASSED;
                        break;
                    case "G":
                        status = TF_TestStatus.PASSED;
                        break;
                    case "W":
                        issfc = true;
                        status = TF_TestStatus.WAIVE;
                        break;
                    case "M":
                        status = TF_TestStatus.WAIVE;
                        break;
                    case "F":
                        issfc = true;
                        status = TF_TestStatus.FAILED;
                        break;
                    case "N":
                        status = TF_TestStatus.FAILED;
                        break;
                    case "E":
                        status = TF_TestStatus.ERROR;
                        break;
                    case "T":
                        status = TF_TestStatus.TERMINATED;
                        break;
                    case "A":
                        status = TF_TestStatus.ABORT;
                        break;
                }
                return true;
            }

            return false;
        }

    }

    public class CheckData<T>
    {
        public bool IsChecked { get; set; }
        public T Data { get; set; }
    }
}
