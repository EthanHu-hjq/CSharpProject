using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ModbusTcpClient.Helpers;
using ModbusTcpClient.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusTcpClient.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private TcpHelper? _tcpObject = null!;

        private ushort[]? _rss;
        [RelayCommand]
        private void GetFrontDoorClosed()
        {
            GetFrontDoorClosedEnable = false;
            do
            {
                _rss = null;
                using(_tcpObject = new TcpHelper(IpAddress, Port))
                {
                    try
                    {
                        _rss = _tcpObject.ReadHoldingRegisters(1, TestReady, 1);
                        if (_rss != null && _rss.Length > 0)
                        {
                            AddLog("info", $"Front door status {TestReady}: {_rss[0]}");
                        }
                        else
                        {
                            AddLog("warn", $"Failed to read register {TestReady}");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog("error", $"Error for {TestReady}: {ex.Message}");
                        _rss = null;
                        continue;
                    }
                }
            }while (_rss == null || _rss.Length == 0 || _rss[0] != 1);
            AddLog("info", "Front door closed");
            SetTestCompletedEnable = true;
        }
        [ObservableProperty]
        private ushort _testReady = 561;

        [RelayCommand]
        private void SetTestCompleted()
        {
            _rss = null;
            using (_tcpObject = new TcpHelper(IpAddress, Port))
            {
                try
                {
                    _tcpObject.WriteHoldingRegister(1,TestCompleted, 1);
                    AddLog("info", $"Set test completed {TestCompleted}");
                    Thread.Sleep(200);
                    _rss = _tcpObject.ReadHoldingRegisters(1, TestCompleted, 1);
                    AddLog("info", $"Test completed {TestCompleted} set done : {_rss[0]}");
                    if (_rss[0] == 1)
                    {
                        _tcpObject.WriteHoldingRegister(1, TestCompleted, 0);
                        Thread.Sleep(200);
                        _rss = _tcpObject.ReadHoldingRegisters(1, TestCompleted, 1);
                        AddLog("info", $"Test completed {TestCompleted} reset done : {_rss[0]}");
                    }

                    if (_rss != null && _rss.Length > 0 && _rss[0] == 1)
                    {
                        AddLog("warn", "Test completed Continue......");
                    }
                    else if (_rss != null && _rss.Length > 0 && _rss[0] == 0)
                    {
                        AddLog("warn", "Test completed reset......");
                        SetTestCompletedEnable = false;
                    }
                }
                catch (Exception ex)
                {
                    AddLog("error", $"Error for {TestCompleted}: {ex.Message}");
                }
            }
        }
        [ObservableProperty]
        private ushort _testCompleted = 571;

        [ObservableProperty]
        private ObservableCollection<LogEntry> _logEntries = null!;

        public MainViewModel()
        {
            LogEntries = new ObservableCollection<LogEntry>();
        }

        [ObservableProperty]
        private string _timeStamp = DateTime.Now.ToString("yyyy - MM - dd HH:mm:ss.uuu");

        public void AddLog(string level, string message)
        {
            switch (level.ToLower())
            {
                case "info":
                    Log.Information(message);
                    break;
                case "warn":
                    Log.Warning(message);
                    break;
                case "error":
                    Log.Error(message);
                    break;
                default:
                    Log.Debug(message);
                    break;
            }

            string timestamp = DateTime.Now.ToString("yyyy - MM - dd HH:mm:ss");
            LogEntries.Add(new LogEntry { TimeStamp = timestamp, Level = level, Message = message });
        }


        [ObservableProperty]
        private string _ipAddress = "192.168.1.10";

        [ObservableProperty]
        private int _port = 502;

        [ObservableProperty]
        private bool _getFrontDoorClosedEnable = true;

        [ObservableProperty]
        private bool _setTestCompletedEnable = false;
    }
}
