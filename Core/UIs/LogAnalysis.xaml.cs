using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ToucanCore.UIs
{
    /// <summary>
    /// Interaction logic for LogAnalysis.xaml
    /// </summary>
    public partial class LogAnalysis : Window
    {
        public LogAnalysis()
        {
            InitializeComponent();
            cb_Filter.ItemsSource = new string[] { "TYMSFC", "TYMSFC -GetModel", "TYMSFC -CheckStation", "TYMSFC -InsertIntoTable", "SN: ", "ERROR", "WARN" };
        }

        public LogAnalysis(string logpath) : this()
        {
            tb_Path.Text = logpath;

            LoadLog(logpath);
        }

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Title = "请选择Log文件";
            ofd.Filter = "Log File|*.log";
            if (ofd.ShowDialog() == true)
            {
                tb_Path.Text = ofd.FileName;
                LoadLog(ofd.FileName);
            }
        }

        List<string> LogLines = new List<string>();
        private void LoadLog(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            try
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    LogLines.Clear();

                    while (!sr.EndOfStream)
                    {
                        LogLines.Add(sr.ReadLine());
                    }

                    FilterLog();
                }
            }
            catch (System.IO.IOException)
            {
                var temp = System.IO.Path.GetTempFileName();

                File.Copy(path, temp, true);

                using (StreamReader sr = new StreamReader(temp, Encoding.UTF8))
                {
                    LogLines.Clear();

                    while (!sr.EndOfStream)
                    {
                        LogLines.Add(sr.ReadLine());
                    }

                    FilterLog();
                }

                File.Delete(temp);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void FilterLog()
        {
            if (string.IsNullOrWhiteSpace(cb_Filter.Text))
            {
                for (int i = 0; i < LogLines.Count; i++)
                {
                    tb_Content.AppendText($"{i}: {LogLines[i]}\r\n");
                }

                return;
            }

            tb_Content.Clear();

            tb_Content.AppendText($"==== Search {cb_Filter.Text} ====");
            for (int i = 0; i < LogLines.Count; i++)
            {
                if (LogLines[i].Contains(cb_Filter.Text))
                {
                    tb_Content.AppendText($"{i}: {LogLines[i]}\r\n");
                }
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterLog();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadLog(tb_Path.Text);
        }
    }
}
