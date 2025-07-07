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
using System.Windows.Shapes;
using TestCore;

namespace Robin.UIs
{
    /// <summary>
    /// Interaction logic for InputSn.xaml
    /// </summary>
    public partial class InputSn : Window
    {
        public DelegateCommand Confirm { get; }

        public string SerialNumber
        {
            get { return (string)GetValue(SerialNumberProperty); }
            set { SetValue(SerialNumberProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SerialNumber.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SerialNumberProperty =
            DependencyProperty.Register("SerialNumber", typeof(string), typeof(InputSn), new PropertyMetadata(null));

        public string Model
        {
            get { return (string)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(string), typeof(InputSn), new PropertyMetadata(null));

        public string ModelDescription
        {
            get { return (string)GetValue(ModelDescriptionProperty); }
            set { SetValue(ModelDescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ModelDescription.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelDescriptionProperty =
            DependencyProperty.Register("ModelDescription", typeof(string), typeof(InputSn), new PropertyMetadata(null));


        public string VacsData
        {
            get { return (string)GetValue(VacsDataProperty); }
            set { SetValue(VacsDataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VacsData.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VacsDataProperty =
            DependencyProperty.Register("VacsData", typeof(string), typeof(InputSn), new PropertyMetadata(null));

        public bool AttachResult
        {
            get { return (bool)GetValue(AttachResultProperty); }
            set { SetValue(AttachResultProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AttachResult.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AttachResultProperty =
            DependencyProperty.Register("AttachResult", typeof(bool), typeof(InputSn), new PropertyMetadata(false));

        public string Info
        {
            get { return (string)GetValue(InfoProperty); }
            set { SetValue(InfoProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Info.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InfoProperty =
            DependencyProperty.Register("Info", typeof(string), typeof(InputSn), new PropertyMetadata(null));



        public InputSn()
        {
            Confirm = new DelegateCommand(cmd_Confirm);
            InitializeComponent();

            tb_SN.Focus();
        }

        private void cmd_Confirm(object obj)
        {
            if (string.IsNullOrWhiteSpace(tb_SN.Text))
            {
                Info = "Serial Number Could not be empty";
            }
            else
            {
                SerialNumber = tb_SN.Text;
                DialogResult = true;
                Close();
            }
        }
    }
}
