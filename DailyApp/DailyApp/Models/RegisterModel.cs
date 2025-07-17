using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyApp.Models
{
    public class RegisterModel:BindableBase
    {
        #region Name
        /// <summary>
        /// 姓名
        /// </summary>
        private string _name;
		public string Name
		{
			get { return _name; }
			set { 
				_name = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region Account
		private string _account;

		public string Account
		{
			get { return _account; }
			set { 
				_account = value;
				RaisePropertyChanged();
            }
        }
        #endregion

        #region Password
        private string _password;

		public string Password
		{
			get { return _password; }
			set { 
				_password = value;
				RaisePropertyChanged();
			}
		}
        #endregion

        #region ConfirmPassword
        private string _confirmPassword;

		public string ConfirmPassword
		{
			get { return _confirmPassword; }
			set { 
				_confirmPassword = value;
				RaisePropertyChanged();
			}
		}
        #endregion
    }
}
