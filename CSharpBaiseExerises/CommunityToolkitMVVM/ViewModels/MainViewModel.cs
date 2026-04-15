using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkitMVVM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommunityToolkitMVVM.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
		private UserModel _userInfo;

		public UserModel UserInfo
		{
			get => _userInfo;
			set
			{
				SetProperty(ref _userInfo, value);
            }
		}

        public RelayCommand ChangeCommand => new RelayCommand(Change);

        public MainViewModel()
        {
            _userInfo = new UserModel
            {
                Id = 1,
                Name = "John Doe"
            };
        }

        private void Change()
        {
            UserInfo.Id = 2;
            UserInfo.Name = "Jane Smith";
        }
    }
}
