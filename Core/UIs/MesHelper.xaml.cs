using Mes;
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
using TestCore.Configuration;
using TestCore.Abstraction.Process;
using TestCore.Abstraction.Data;
using TestCore.Data;

namespace ToucanCore.UIs
{
    /// <summary>
    /// MesHelper.xaml 的交互逻辑
    /// </summary>
    public partial class MesHelper : Window
    {
        public static string[] Locations { get; } = new string[] { "TYHZ", "TYDG", "TYTH", "PRIMAX", "PRIMAX TH" };
        public string Location
        {
            get { return (string)GetValue(LocationProperty); }
            set { SetValue(LocationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register("Location", typeof(string), typeof(MesHelper), new PropertyMetadata(null, LocChanged));

        private static void LocChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MesHelper mh)
            {
                if (Enum.TryParse(mh.Location, out TestCore.Location loc))
                {
                }
                else
                {
                    loc = TestCore.Location.Vendor;
                }

                mh.MesInstance = Mes.MesManager.GetMesInstance(loc, mh.Location);
                if(mh.MesInstance != null)
                {
                    mh.tb_Log?.AppendText($"{DateTime.Now.ToShortTimeString()}: Swich MES to {loc} OK\r\n");
                }
            }
        }

        public string Product
        {
            get { return (string)GetValue(ProductProperty); }
            set { SetValue(ProductProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Product.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProductProperty =
            DependencyProperty.Register("Product", typeof(string), typeof(MesHelper), new PropertyMetadata(null, ConfigChanged));

        

        public string Station
        {
            get { return (string)GetValue(StationProperty); }
            set { SetValue(StationProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Station.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StationProperty =
            DependencyProperty.Register("Station", typeof(string), typeof(MesHelper), new PropertyMetadata(null, ConfigChanged));

        public string SerialNumber
        {
            get { return (string)GetValue(SerialNumberProperty); }
            set { SetValue(SerialNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNumberProperty =
            DependencyProperty.Register("SerialNumber", typeof(string), typeof(MesHelper), new PropertyMetadata(null, ConfigChanged));

        public string PartNo
        {
            get { return (string)GetValue(PartNoProperty); }
            set { SetValue(PartNoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PartNo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PartNoProperty =
            DependencyProperty.Register("PartNo", typeof(string), typeof(MesHelper), new PropertyMetadata(null, ConfigChanged));

        public string LineNo
        {
            get { return (string)GetValue(LineNoProperty); }
            set { SetValue(LineNoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LineNo.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LineNoProperty =
            DependencyProperty.Register("LineNo", typeof(string), typeof(MesHelper), new PropertyMetadata(null, ConfigChanged));


        public string TableName
        {
            get { return (string)GetValue(TableNameProperty); }
            set { SetValue(TableNameProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TableName.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TableNameProperty =
            DependencyProperty.Register("TableName", typeof(string), typeof(MesHelper), new PropertyMetadata(null, ConfigChanged));

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Version.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(MesHelper), new PropertyMetadata("1.0", ConfigChanged));

        public string MesApi
        {
            get { return (string)GetValue(MesApiProperty); }
            set { SetValue(MesApiProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MesApi.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MesApiProperty =
            DependencyProperty.Register("MesApi", typeof(string), typeof(MesHelper), new PropertyMetadata("Get_Model"));

        public string Parameter
        {
            get { return (string)GetValue(ParameterProperty); }
            set { SetValue(ParameterProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Parameter.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParameterProperty =
            DependencyProperty.Register("Parameter", typeof(string), typeof(MesHelper), new PropertyMetadata(null));


        private static void ConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MesHelper mh)
            {
                mh.Config = new SFCsConfig(mh.TableName, mh.Product, mh.Station, mh.MesApi, mh.Version);
            }
        }


        SFCsConfig Config;

        public MesHelper()
        {
            if (GlobalConfiguration.Default.Station.Location == TestCore.Location.Vendor)
            {
                Location = GlobalConfiguration.Default.Station.Vendor;
            }
            else
            {
                Location = GlobalConfiguration.Default.Station.Location.ToString();
            }
            InitializeComponent();
        }

        public MesHelper(SFCsConfig config) : this()
        {
            Product = config?.Product;
            Station = config?.Station;
            TableName = config?.SfcsTable;
            Version = config?.Version;
            MesApi = config?.GetPartNoApi;
            LineNo = config?.PersitLineNo;
            PartNo = config?.PersitPartNo;
        }

        IMes MesInstance;
        private void cb_MesChanged(object sender, SelectionChangedEventArgs e)
        {
            if(sender is ComboBox cb)
            {
                if(cb.SelectedItem is string location)
                {
                    if(Enum.TryParse(location, out TestCore.Location loc))
                    {
                        MesInstance = Mes.MesManager.GetMesInstance(loc, location);
                    }
                    else
                    {
                        loc = TestCore.Location.Vendor;
                    }

                    MesInstance = Mes.MesManager.GetMesInstance(loc, location);
                }
            }
        }

        private void btn_Execute_Click(object sender, RoutedEventArgs e)
        {
            
            if (tb_Func.SelectedIndex == 0)
            {
                try
                {
                    MesInstance.CheckStation(Config, SerialNumber, LineNo);
                    tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: CheckStation {SerialNumber} on {LineNo} Passed\r\n");
                }
                catch (Exception cse)
                {
                    tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: CheckStation {SerialNumber} on {LineNo} Failed. {cse.Message}\r\n");
                }

            }
            else if (tb_Func.SelectedIndex == 1)
            {
                string rtn = null;
                try
                {
                    rtn = MesInstance.ExecMesApi(Config, MesApi, Parameter);
                    tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: Exec {MesApi} {Parameter} Passed. rtn {rtn}\r\n");
                }
                catch(Exception apie)
                {
                    tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: Exec {MesApi} {Parameter} Failed. {apie.Message}\r\n");
                }
                
            }
            else if (tb_Func.SelectedIndex == 2)
            {
                MessageBox.Show("Not Support Yet");

                //try
                //{
                //    ITestResult rs = new TF_Result() { SFCsConfig = Config };

                //    MesInstance.CommitMesResult(rs, "", tb_ExtCols.Text, tb_ExtVals.Text);
                //    tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: Exec Commit Result Passed.\r\n");
                //}
                //catch (Exception apie)
                //{
                //    tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: Exec {MesApi} {Parameter} Failed. {apie.Message}\r\n");
                //}
            }

            tb_Log.ScrollToEnd();
        }

        private void btn_Initialize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MesInstance?.Initialize(Config, null);
                tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: Initialize OK.\r\n");
            }
            catch (Exception ex)
            {
                tb_Log.AppendText($"{DateTime.Now.ToShortTimeString()}: Initialize Failed. {ex.Message}\r\n");
            }
        }
    }
}
