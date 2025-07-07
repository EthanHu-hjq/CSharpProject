using ScottPlot.Plottable;
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
using TestCore.Data;
using Toucan_WPF.ViewModels;

namespace Toucan_WPF.UIs
{
    /// <summary>
    /// LimitEditor.xaml 的交互逻辑
    /// </summary>
    public partial class LimitEditor : Window
    {
        public VM_LimitEditor vm { get; }
        public LimitEditor(Nest<TF_Limit> nest)
        {
            InitializeComponent();
            DataContext = vm = new VM_LimitEditor(nest.Element, wpfplot);
        }

        private void wpfplot_PlottableDragged(object sender, RoutedEventArgs e)
        {

        }

        private void wpfplot_PlottableDropped(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ScatterPlotDraggable spd)
            {
                if (vm.CurveLimits.FirstOrDefault(x => x.UslPlot == spd) is ViewCurveLimit clusl)
                {
                    if (clusl.UslValue?.XLog == true)
                    {
                        if (clusl.UslValue?.YLog == true)
                        {
                            clusl.UpdateUslValue(spd.Xs.Select(x => Math.Pow(10, x)).ToArray(), spd.Ys.Select(x => Math.Pow(10, x)).ToArray());
                        }
                        else
                        {
                            clusl.UpdateUslValue(spd.Xs.Select(x => Math.Pow(10, x)).ToArray(), spd.Ys);
                        }
                    }
                    else
                    {
                        if (clusl.UslValue?.YLog == true)
                        {
                            clusl.UpdateUslValue(spd.Xs, spd.Ys.Select(x=> Math.Pow(10, x)).ToArray());
                        }
                        else
                        {
                            clusl.UpdateUslValue(spd.Xs, spd.Ys);
                        }
                    }
                }
                else if (vm.CurveLimits.FirstOrDefault(x => x.LslPlot == spd) is ViewCurveLimit cllsl)
                {
                    if (cllsl.LslValue?.XLog == true)
                    {
                        if (cllsl.LslValue?.YLog == true)
                        {
                            cllsl.UpdateLslValue(spd.Xs.Select(x => Math.Pow(10, x)).ToArray(), spd.Ys.Select(x => Math.Pow(10, x)).ToArray());
                        }
                        else
                        {
                            cllsl.UpdateLslValue(spd.Xs.Select(x => Math.Pow(10, x)).ToArray(), spd.Ys);
                        }
                    }
                    else
                    {
                        if (cllsl.LslValue?.YLog == true)
                        {
                            cllsl.UpdateLslValue(spd.Xs, spd.Ys.Select(x => Math.Pow(10, x)).ToArray());
                        }
                        else
                        {
                            cllsl.UpdateLslValue(spd.Xs, spd.Ys);
                        }
                    }
                }
            }
        }
    }
}
