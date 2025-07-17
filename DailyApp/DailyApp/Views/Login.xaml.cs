using DailyApp.Models;
using Prism.Events;
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

namespace DailyApp.Views
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : UserControl
    {
        /// <summary>
        /// 事件聚合器
        /// </summary>
        private readonly IEventAggregator _eventAggregator;
        public Login(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            _eventAggregator = eventAggregator;// 注册事件
            _eventAggregator.GetEvent<MsgEvent>().Subscribe(Sub);// 订阅消息
        }

        /// <summary>
        /// 订阅消息
        /// </summary>
        /// <param name="obj"></param>
        private void Sub(string obj)
        {
            RegLoginBar.MessageQueue.Enqueue(obj);// 显示消息
        }
    }
}
