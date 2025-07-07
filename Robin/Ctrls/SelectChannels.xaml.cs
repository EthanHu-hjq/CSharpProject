using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using TestCore;

namespace Robin.Ctrls
{
    /// <summary>
    /// Interaction logic for SelectChannels.xaml
    /// </summary>
    public partial class SelectChannels : UserControl
    {       
        public DelegateCommand SelectAll { get; }
        public DelegateCommand ClearSelection { get; }

        public int ChannelCount
        {
            get { return (int)GetValue(ChannelCountProperty); }
            set { SetValue(ChannelCountProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ChannelCount.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChannelCountProperty =
            DependencyProperty.Register("ChannelCount", typeof(int), typeof(SelectChannels), new PropertyMetadata(16, ChannelCountChanged));

        private static void ChannelCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is SelectChannels vm)
            {
                vm.ChannelStatus.Clear();

                for (int i = 0; i < vm.ChannelCount; i++)
                {
                    vm.ChannelStatus.Add(new ChannelState() { Enable = true, Index = i, Name = $"Ch {i + 1}" });
                }
            }
        }

        public string SelectedString
        {
            get { return (string)GetValue(SelectedStringProperty); }
            set { SetValue(SelectedStringProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedString.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedStringProperty =
            DependencyProperty.Register("SelectedString", typeof(string), typeof(SelectChannels), new PropertyMetadata(string.Empty));

        public ObservableCollection<ChannelState> ChannelStatus
        {
            get { return (ObservableCollection<ChannelState>)GetValue(ChannelStatusProperty); }
            set { SetValue(ChannelStatusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ChannelStatus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ChannelStatusProperty =
            DependencyProperty.Register("ChannelStatus", typeof(ObservableCollection<ChannelState>), typeof(SelectChannels), new PropertyMetadata(null));



        public SelectChannels()
        {
            InitializeComponent();

            SelectAll = new DelegateCommand(cmd_SelectAll);
            ClearSelection = new DelegateCommand(cmd_Clear);
        }

        private void cmd_Clear(object obj)
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                ChannelStatus[i].Enable = false;
            }
        }

        private void cmd_SelectAll(object obj)
        {
            for (int i = 0; i < ChannelCount; i++)
            {
                ChannelStatus[i].Enable = true;
            }
        }
    }

    public class ChannelState
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public bool Enable { get; set; }
    }
}
