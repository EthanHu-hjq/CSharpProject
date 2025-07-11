using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DailyApp.Extensions
{
    public class PasswordHelper
    {


        /// <summary>
        /// 获取密码的静态方法
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string  GetPwd(DependencyObject obj)
        {
            return (string)obj.GetValue(PwdProperty);
        }

        /// <summary>
        /// 设置密码的静态方法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        public static void SetPwd(DependencyObject obj,string value)
        {
            obj.SetValue(PwdProperty, value);
        }

        // Using a DependencyProperty as the backing store for MyProperty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PwdProperty =
            DependencyProperty.RegisterAttached("Pwd", 
                typeof(string), 
                typeof(PasswordHelper), 
                new PropertyMetadata("", OnPwdChanged));//绑定属性变化事件

        /// <summary>
        /// 密码变化事件
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnPwdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = d as PasswordBox;
            if (passwordBox != null && passwordBox.Password != (string)e.NewValue)
            {
                passwordBox.Password = (string)e.NewValue;
            }
        }
    }

    public class PasswordBoxBehavior: Behavior<PasswordBox>
    {
        /// <summary>
        /// 初始化
        /// </summary>
        override protected void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PasswordChanged += PasswordBox_PasswordChanged;
        }

        /// <summary>
        /// 密码变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            string password = PasswordHelper.GetPwd(passwordBox);
            if (passwordBox != null && passwordBox.Password != password)
            {
                PasswordHelper.SetPwd(passwordBox, passwordBox.Password);
            }
        }

        /// <summary>
        /// 取消绑定
        /// </summary>
        override protected void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.PasswordChanged -= PasswordBox_PasswordChanged;
        }
    }
}
