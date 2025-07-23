using DailyApp.Models;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Windows.Documents;

namespace DailyApp.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region Properties
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

        #endregion


        #region 构造函数
        public MainWindowViewModel(IRegionManager regionManager,IRegionNavigationJournal navigationJournal)
        {
            LeftMenuList = new List<LeftMenuInfo>();

            // 左侧菜单栏列表创建数据方法
            CreateMenu();

            // 区域管理对象赋值
            _regionManager = regionManager;

            // 切换导航页命令
            NavigateCmd = new DelegateCommand<LeftMenuInfo>(Navigate);

            // 历史记录对象赋值
            _navigationJournal = navigationJournal;
            // 前进 后退命令
            ForwardCmd = new DelegateCommand(GoForward);
            BackwardCmd = new DelegateCommand(GoBackward);
        }
        #endregion

        #region 私有方法

        /// <summary>
        /// 左侧菜单栏列表创建数据方法
        /// </summary>
        private void CreateMenu()
        {
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "Home", MenuName = "首页", ViewName = "HomeUC" });
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "NotebookOutline", MenuName = "待办事项", ViewName = "WaitUC" });
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "NotebookPlus", MenuName = "备忘录", ViewName = "MemoUC" });
            LeftMenuList.Add(new LeftMenuInfo() { Icon = "Cog", MenuName = "设置", ViewName = "SettingUC" });
        }

        #region 区域管理 导航页切换
        //区域管理对象字段
        private IRegionManager _regionManager;
        //切换导航页命令
        public DelegateCommand<LeftMenuInfo> NavigateCmd { get; set; }
        private void Navigate(LeftMenuInfo menuInfo)
        {
            if (menuInfo == null || string.IsNullOrEmpty(menuInfo.ViewName)) return;
            // 切换导航页
            _regionManager.Regions["MainViewRegion"].RequestNavigate(menuInfo.ViewName, callback =>
            {
                // 导航页切换成功后，更新历史记录
                _navigationJournal = callback.Context.NavigationService.Journal;
            });
        }
        #endregion

        #region 前进 后退
        //历史记录字段
        private IRegionNavigationJournal _navigationJournal;//在构造函数中赋值 在区域管理对导航页切换成功后，更新历史记录
        public DelegateCommand ForwardCmd { get;private set; }
        public DelegateCommand BackwardCmd { get;private set; }
        /// <summary>
        /// 前进 
        /// </summary>
        private void GoForward()
        {
            if (_navigationJournal.CanGoForward && _navigationJournal != null)
            {
                _navigationJournal.GoForward();
            }
        }
        private void GoBackward()
        {
            if (_navigationJournal.CanGoBack && _navigationJournal != null)
            {
                _navigationJournal.GoBack();
            }
        }
        #endregion



        #endregion

    }
}
