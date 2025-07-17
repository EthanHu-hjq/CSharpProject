using DailyApp.DTOs;
using DailyApp.HttpClients;
using DailyApp.Models;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DailyApp.ViewModels
{
    public class LoginViewModel :BindableBase, IDialogAware
    {
        #region Commands
        public DelegateCommand LoginCommand { get; set; }
        public DelegateCommand<string> ShowRegisterViewCommand { get; set; }
        public DelegateCommand RegCmm { get; set; }
        #endregion

        #region Properties

        #region SelectedIndex
        private string _selectedIndex;

        public string SelectedIndex
        {
            get { return _selectedIndex; }
            set { 
                _selectedIndex = value; 
                RaisePropertyChanged();
            }
        }
        #endregion

        #region 注册信息
        private AccountInfoDTO _accountInfoDTO;

        public AccountInfoDTO AccountInfoDTO
        {
            get { return _accountInfoDTO; }
            set { 
                _accountInfoDTO = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #endregion

        #region 字段
        //HttpRestClient字段
        private HttpRestClient _httpRestClient;
        
        public RegisterModel RegisterModel { get; set; }//注册模型

        private PasswordEncryptor encryptor { get; set; }//密码加密器
        private IEventAggregator _eventAggregator { get; set; }//事件聚合器
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public LoginViewModel(HttpRestClient httpRestClient,IEventAggregator eventAggregator)
        {
            LoginCommand = new DelegateCommand(OnLogin);
            ShowRegisterViewCommand = new DelegateCommand<string>(OnShowRegisterView);
            RegCmm = new DelegateCommand(OnReg);
            AccountInfoDTO = new AccountInfoDTO();
            _httpRestClient = httpRestClient;
            RegisterModel = new RegisterModel();
            encryptor = new PasswordEncryptor();
            _eventAggregator = eventAggregator;
        }

        #region 命令实现方法
        /// <summary>
        /// 注册
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void OnReg()
        {
            AccountInfoDTO.Name = RegisterModel.Name;
            AccountInfoDTO.Account = RegisterModel.Account;
            AccountInfoDTO.Password = RegisterModel.Password;
            AccountInfoDTO.ConfirmPassword = RegisterModel.ConfirmPassword;

            if (string.IsNullOrEmpty(AccountInfoDTO.Account) || string.IsNullOrEmpty(AccountInfoDTO.Password) 
                || string.IsNullOrEmpty(AccountInfoDTO.Name) || string.IsNullOrEmpty(AccountInfoDTO.ConfirmPassword))
            {
                return;
            }
            if(AccountInfoDTO.Password != AccountInfoDTO.ConfirmPassword)
            {
                MessageBox.Show("两次密码输入不一致，请重新输入！");
                return;
            }

            AccountInfoDTO.Password = encryptor.EncryptPassword(AccountInfoDTO.Password);
            
            //TODO: 注册逻辑实现
            ApiRequest apiRequest = new ApiRequest();
            apiRequest.Route = "Account/Register";
            apiRequest.Method = RestSharp.Method.POST;
            apiRequest.Parameters = AccountInfoDTO;
            ApiResponse apiResponse = _httpRestClient.Execute(apiRequest);
            
            if(apiResponse.ResultCode == 0)
            {
                _eventAggregator.GetEvent<MsgEvent>().Publish(apiResponse.ResultMessage);
                SelectedIndex = "0";
            }
            else
            {
                MessageBox.Show(apiResponse.ResultMessage);
            }
        }

        private void OnShowRegisterView(string obj)
        {
            int index = int.Parse(obj);
            SelectedIndex = index.ToString();
            if(index == 1)
            {
                RegisterModel.Name = "";
                RegisterModel.Account = "";
                RegisterModel.Password = "";
                RegisterModel.ConfirmPassword = "";
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        private void OnLogin()
        {
            string password = RegisterModel.Password;
            if (password == "123456")
            {
                //登录成功
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
            }

        }

        public string Title => "DailyApp";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
        }
        #endregion
    }
}
