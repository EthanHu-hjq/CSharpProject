using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScreenRecordingTool.Mvvm
{
    /// <summary>
    /// 所有 ViewModel 的基类，实现 INotifyPropertyChanged。
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 通知界面指定属性已变更。
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置字段值，若值发生变化则触发属性变更通知。
        /// </summary>
        /// <returns>值是否发生了变化。</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
