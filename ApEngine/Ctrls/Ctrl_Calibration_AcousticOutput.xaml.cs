using ApEngine.Base;
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
    /// Interaction logic for Ctrl_Calibration_AcousticOutput.xaml
    /// </summary>
    public partial class Ctrl_Calibration_AcousticOutput : StackPanel
    {
        public Ctrl_Calibration_AcousticOutput()
        {
            InitializeComponent();
            DataContextChanged += Ctrl_Calibration_AcousticOutput_DataContextChanged;
        }

        private void Ctrl_Calibration_AcousticOutput_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Calib_AcousticOutput calib)
            {
                //if (calib.VoltageRatio is null)
                //{
                //    calib.VoltageRatio = ApxEngine.ApRef.SignalPathSetup.References.AcousticOutputReferences.VoltageRatio;
                //    calib.RefFreq = ApxEngine.ApRef.SignalPathSetup.References.AcousticOutputReferences.ReferenceFrequency;
                //}
                //else
                //{ 
                //    //TODO
                //}
            }
        }
    }
}
