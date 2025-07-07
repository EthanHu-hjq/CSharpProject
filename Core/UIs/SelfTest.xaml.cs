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
using TestCore.Configuration;
using TestCore.Services;
using TestCore;

namespace ToucanCore.UIs
{
    /// <summary>
    /// Interaction logic for SelfTest1.xaml
    /// </summary>
    public partial class SelfTest : Window
    {
        static TestCore.Services.IToolboxService Toolbox = ServiceStatic.ToolboxService();
        public SelfTest()
        {
            InitializeComponent();
            DoSelfTest();
        }

        public void DoSelfTest()
        {
            AppendLog("==== Self Test ====");
            AppendLog("---- Environment ----");
            AppendLog($"MachineName: {Environment.MachineName}");
            AppendLog($"OSVersion: {Environment.OSVersion}");
            AppendLog($"UserDomainName: {Environment.UserDomainName}");
            AppendLog($"CurrentDirectory: {Environment.CurrentDirectory}");

            AppendLog("---- App ----");

            AppendLog($"AppName: {Application.ResourceAssembly?.GetName()?.Name ?? System.Windows.Forms.Application.ProductName}");
            AppendLog($"AppName: {Application.ResourceAssembly?.GetName()?.Version?.ToString() ?? System.Windows.Forms.Application.ProductVersion}");
            AppendLog($"AppIs64Bit: {(IntPtr.Size == 8 ? "Yes" : "No")}");

            AppendLog("---- Hardware ----");

            System.Diagnostics.PerformanceCounter pd_cpu = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            System.Diagnostics.PerformanceCounter pd_ram = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            AppendLog($"  Current CPU: {pd_cpu.NextValue()} %");
            AppendLog($"  Available Memory: {pd_ram.NextValue()} MB");

            AppendLog($"  C: {(Directory.Exists("C:") ? "OK" : "Not Exist")}");
            AppendLog($"  D: {(Directory.Exists("D:") ? "OK" : "Not Exist")}");

            var ports = System.IO.Ports.SerialPort.GetPortNames();
            AppendLog($"  Ports: {string.Join(" ", ports)}");

            pd_cpu.Dispose();
            pd_ram.Dispose();

            AppendLog($"  CPU ID: {StationConfig.CPU_ID}");
            AppendLog($"  MB  ID: {StationConfig.MB_ID}");
            AppendLog($"  HDD ID: {StationConfig.HDD_ID}");

            AppendLog($"  Current IP: {StationConfig.IpAddress}");
            var nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            foreach (var nic in nics)
            {
                if (nic.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up)
                {
                    continue;
                }

                // Only suit for FE/GE/WLAN
                switch (nic.NetworkInterfaceType)
                {
                    case System.Net.NetworkInformation.NetworkInterfaceType.Ethernet:
                    case System.Net.NetworkInformation.NetworkInterfaceType.FastEthernetT:
                    case System.Net.NetworkInformation.NetworkInterfaceType.FastEthernetFx:
                    case System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet:
                    case System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211:
                        foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                        {
                            if (nic.Name.Contains("VirtualBox")) continue;

                            // if no dns, which means it is probablly in internat
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && addr.IsDnsEligible)
                            {
                                var baddr = addr.Address.GetAddressBytes();

                                uint laddr = (uint)(baddr[0] << 24) + (uint)(baddr[1] << 16) + (uint)(baddr[2] << 8) + (uint)baddr[3];

                                var mac = string.Join(":", nic.GetPhysicalAddress().GetAddressBytes().Select(x => x.ToString("X02")));
                                var ipaddr = addr.Address.ToString();
                                var mask = addr.IPv4Mask.ToString();

                                AppendLog($"  NIC: {nic.Name}. {ipaddr}, {mask}, {mac}");
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            AppendLog("---- Network ----");
            AppendLog("1. Nas");

            if (string.IsNullOrEmpty(StationConfig.IpAddress))
            {
                AppendLog("  No Network detected. Ignored");
            }
            else
            {
                DateTime t0 = DateTime.Now;
                AppendLog($"  {t0}: Ping {SFCsConfig.NetworkTestTarget}.");
                var rtn = TF_Utility.Ping(SFCsConfig.NetworkTestTarget, out string remoteip);
                if (rtn > 0)
                {
                    AppendLog($"  {DateTime.Now}: Ping Nas OK. Remote IP {remoteip}. delay {DateTime.Now.Subtract(t0).TotalMilliseconds} ms");
                }
                else
                {
                    AppendLog($"  {DateTime.Now}: Ping Failed.");
                }
            }

            AppendLog("2. SFCs");

            if (string.IsNullOrEmpty(StationConfig.IpAddress))
            {
                AppendLog("  No Network detected. Ignored");
            }
            else
            {
                string sitesfc = null;
                if (GlobalConfiguration.Default.Station.Location == TestCore.Location.TYHZ)
                {
                    sitesfc = "10.12.20.98";
                }
                else if (GlobalConfiguration.Default.Station.Location == TestCore.Location.TYDG)
                {
                    sitesfc = "10.6.202.165";
                }
                else if (GlobalConfiguration.Default.Station.Location == TestCore.Location.TYDC)
                {
                    sitesfc = "10.7.20.5";
                }
                else if (GlobalConfiguration.Default.Station.Location == TestCore.Location.TYDC)
                {
                    sitesfc = "10.85.1.24";
                }
                else if (GlobalConfiguration.Default.Station.Location == Location.PRIMAX)
                {
                    sitesfc = "10.40.1.72";
                }
                else if (GlobalConfiguration.Default.Station.Vendor == "PRIMAX TH")
                {
                    sitesfc = "10.80.1.27"; 
                }

                if (string.IsNullOrEmpty(sitesfc))
                {
                    AppendLog($"  Not Support Site {GlobalConfiguration.Default.Station.Location}");
                }
                else
                {
                    DateTime t0 = DateTime.Now;
                    AppendLog($"  {t0}: Ping {sitesfc}. Site {GlobalConfiguration.Default.Station.Location}");
                    var rtn = TF_Utility.Ping(sitesfc, out string remoteip);
                    if (rtn > 0)
                    {
                        AppendLog($"  {DateTime.Now}: Ping Sfcs OK. Remote IP {remoteip}. delay {DateTime.Now.Subtract(t0).TotalMilliseconds} ms");
                    }
                    else
                    {
                        AppendLog($"  {DateTime.Now}: Ping Failed.");
                    }
                }

            }

            AppendLog("3. Raven");

            if (string.IsNullOrEmpty(StationConfig.IpAddress))
            {
                AppendLog("  No Network detected. Ignored");
            }
            else
            {
                DateTime t0 = DateTime.Now;
                AppendLog($"  {t0}: Ping {RemoteServiceConfig.Instance.ServerAddress}.");
                var rtn = TF_Utility.Ping(RemoteServiceConfig.Instance.ServerAddress, out string remoteip);
                if (rtn > 0)
                {
                    AppendLog($"  {DateTime.Now}: Ping Raven OK. Remote IP {remoteip}. delay {DateTime.Now.Subtract(t0).TotalMilliseconds} ms");
                }
                else
                {
                    AppendLog($"  {DateTime.Now}: Ping Failed.");
                }
            }

            AppendLog("4. NTP");
            if (string.IsNullOrEmpty(StationConfig.IpAddress))
            {
                AppendLog("  No Network detected. Ignored");
            }
            else
            {
                DateTime t0 = DateTime.Now;
                if (Toolbox.GetTimeService() is ITimeService timesrv)
                {
                    try
                    {
                        var time = timesrv.CurrentTime;
                        AppendLog($"  Get Service Time from {SFCsConfig.NetworkTestTarget} OK.");
                        AppendLog($"  NTP Time {time}.");
                    }
                    catch
                    {
                        AppendLog($"  Get Service Time from {SFCsConfig.NetworkTestTarget} Failed.");
                    }
                }
            }

            AppendLog("==== Self Test Completed ====");
        }

        public void AppendLog(string log)
        {
            TF_Base.StaticLog(log);

            textBox_Log.AppendText($"{log}\r\n");
        }
    }
}
