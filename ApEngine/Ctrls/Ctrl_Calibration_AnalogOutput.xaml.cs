using ApEngine.Base;
using AudioPrecision.API;
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

namespace ApEngine.Ctrls
{
    /// <summary>
    /// Interaction logic for Ctrl_Calibration_AnalogOutput.xaml
    /// </summary>
    public partial class Ctrl_Calibration_AnalogOutput : StackPanel
    {
        public Ctrl_Calibration_AnalogOutput()
        {
            InitializeComponent();
            DataContextChanged += Ctrl_Calibration_AnalogOutput_DataContextChanged;
        }

        private void Ctrl_Calibration_AnalogOutput_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is Calib_AnalogOutput calib)
            {
                if (calib.dBrG is null)
                {
                    calib.dBrG = ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.dBrG.Text;
                    calib.dBm = ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.dBm.Text;
                    calib.Watts = ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.Watts.Text;
                }
                else
                {
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.dBrG.Text = calib.dBrG;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.dBm.Text = calib.dBm;
                    ApxEngine.ApRef.SignalPathSetup.References.AnalogOutputReferences.Watts.Text = calib.Watts;
                }
            }
        }
    }
}
