using ManualMVVM.Commands;
using ManualMVVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ManualMVVM.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ICommand screenRecordCmd { get; }
        public ICommand stopScreenCmd { get; }

        private string? _screenStatus;

        public string? ScreenStatus
        {
            get { return _screenStatus; }
            set
            {
                SetProperty(ref _screenStatus, value);
            }
        }

        private UserModel _user = new UserModel();

        public UserModel User
        {
            get { return _user ; }
            set => SetProperty(ref _user, value);
        }


        public MainViewModel()
        {
            screenRecordCmd = new RelayCommand(_ => ScreenRecord());
            stopScreenCmd = new RelayCommand(_ => StopScreen());
        }

        private void StopScreen()
        {
            ScreenStatus = "Stop Screen Recording";
            User = new UserModel(id:1, name: "John");
        }

        private void ScreenRecord()
        {
            ScreenStatus = "Start Screen Recording";
            User = new UserModel(id: 2,name:"Amy");
        }
    }
}
