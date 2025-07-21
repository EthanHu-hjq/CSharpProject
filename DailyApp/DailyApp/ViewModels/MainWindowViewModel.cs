using DailyApp.Models;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace DailyApp.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        /// <summary>
        /// 左侧菜单栏列表属性
        /// </summary>
        private List<LeftMenuInfo> _lefMenuList;

        public List<LeftMenuInfo> LeftMenuList
        {
            get { return _lefMenuList; }
            set { 
                _lefMenuList = value;
                RaisePropertyChanged();
            }
        }


        public MainWindowViewModel()
        {
            LeftMenuList = new List<LeftMenuInfo>();

            // 左侧菜单栏列表创建数据方法
            CreateMenu();

        }

        private void CreateMenu()
        {
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "Home", MenuName = "首页", ViewName = "IndexView" });
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "NotebookOutline", MenuName = "待办事项", ViewName = "WaitView" });
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "NotebookPlus", MenuName = "备忘录", ViewName = "MemoView" });
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "Cog", MenuName = "设置", ViewName = "SettingView" });
        }
    }
}
