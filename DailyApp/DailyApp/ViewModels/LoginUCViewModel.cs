using Prism.Dialogs;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyApp.ViewModels
{
    internal class LoginUCViewModel : IDialogAware
    {
        public DialogCloseListener RequestClose { get; set; }

        public DelegateCommand Login { get; set; }
        public LoginUCViewModel()
        {
            Login = new DelegateCommand(OnLogin);
        }

        /// <summary>
        /// Login event handler
        /// </summary>
        private void OnLogin()
        {
            RequestClose.Invoke(ButtonResult.OK);
        }


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
    }
}
