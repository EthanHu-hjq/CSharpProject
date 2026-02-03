using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenRecordTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ScreenRecordTool.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isRecording = false;
        [ObservableProperty] 
        private string? _timerCount;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(BtnClickCommand))]
        private bool _isEnable = false;

        public string BtnIcon
        {
            get => IsRecording ? "\uE91F" : "\uE714";
        } 
        public string BtnContent
        {
            get => IsRecording ? "Stop" : "Start";
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private void BtnClick()
        {
            if (IsRecording)
            {
                IsRecording = false;
            }
            else
            {
                IsRecording = true;
                Task.Run(() =>
                {
                    while (IsRecording)
                    {
                        Task.Delay(1000).Wait();
                        TimerCount = DateTime.Now.ToString("HH:mm:ss");
                    }
                });
            }
            OnPropertyChanged(nameof(BtnIcon));
            OnPropertyChanged(nameof(BtnContent));
        }
        private bool CanStart() => IsEnable;
    }
}
