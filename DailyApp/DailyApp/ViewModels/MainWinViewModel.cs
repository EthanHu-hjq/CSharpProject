using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DailyApp.ViewModels
{
    internal class MainWinViewModel
    {
        public DelegateCommand Click_btn { get; }
        public MainWinViewModel()
        {
            Click_btn = new DelegateCommand(OnClick_btn);
        }

        private void OnClick_btn()
        {
            MessageBox.Show("Hello, World!", "Greeting", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
