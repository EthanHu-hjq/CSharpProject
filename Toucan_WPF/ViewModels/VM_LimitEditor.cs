using ScottPlot;
using ScottPlot.Drawing.Colormaps;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using TestCore;
using TestCore.Data;

namespace Toucan_WPF.ViewModels
{
    public class VM_LimitEditor : DependencyObject
    {
        public TF_Limit Limit
        {
            get { return (TF_Limit)GetValue(LimitProperty); }
            set { SetValue(LimitProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Limit.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LimitProperty =
            DependencyProperty.Register("Limit", typeof(TF_Limit), typeof(VM_LimitEditor), new PropertyMetadata(null));

        public List<ViewCurveLimit> CurveLimits { get; }

        public WpfPlot wpfplot { get; set; }

        public DelegateCommand Save { get; }

        public VM_LimitEditor(TF_Limit limit, WpfPlot plot)
        {
            Save = new DelegateCommand(cmd_Save);

            wpfplot = plot;
            Limit = limit;
            CurveLimits = new List<ViewCurveLimit>();
            
            if (limit.LimitType == typeof(TF_Curve))
            {
                var vcl = new ViewCurveLimit(limit, string.Empty);
                CurveLimits.Add(vcl);

                int i = 0;
                while(limit.Secondary != null)
                {
                    limit = limit.Secondary;
                    vcl = new ViewCurveLimit(limit, $"sec{i}_");
                    i++;
                    CurveLimits.Add(vcl);
                }
            }
            RefreshChart();
        }

        private void cmd_Save(object obj)
        {
            var temp = Limit;
            foreach (var cvlimit in CurveLimits)
            {
                temp.USL = cvlimit.UslValue;
                temp.LSL = cvlimit.LslValue;

                temp = Limit.Secondary;
            }
        }

        static string logTickLabels(double y) => Math.Pow(10, y).ToString();
        static string logTickLabels_N0(double y) => Math.Pow(10, y).ToString("N0");
        static string logTickLabels_G(double y) => Math.Pow(10, y).ToString("G");
        private void RefreshChart()
        {
            wpfplot.Plot.Clear();
            wpfplot.Plot.ResetLayout();

            var limit = Limit;

            if (limit.LimitType == typeof(TF_Curve))
            {
                if (CurveLimits.All(x => x.USL.Count == 0 && x.LslPlot.PointCount == 0)) return;

                foreach (var vcl in CurveLimits) 
                {
                    if (vcl.UslPlot != null) wpfplot.Plot.Add(vcl.UslPlot);
                    if (vcl.LslPlot != null) wpfplot.Plot.Add(vcl.LslPlot);
                }

                TF_Curve cv = null;
                if (limit.USL is TF_Curve cvush)
                {
                    cv = cvush;
                }
                else if (limit.LSL is TF_Curve cvusl)
                {
                    cv = cvusl;
                }
                else
                {
                }

                if (cv.XLog)
                {
                    wpfplot.Plot.XAxis.TickLabelFormat(logTickLabels);
                }
                else
                {
                    wpfplot.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                }
                wpfplot.Plot.XAxis.MinorLogScale(cv.XLog);
                wpfplot.Plot.XAxis.MinorGrid(true);

                //wpfplot.Plot.YTicks();
                if (cv.YLog)
                {
                    wpfplot.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                }
                else
                {
                    wpfplot.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                }

                wpfplot.Plot.YAxis.MinorLogScale(cv.YLog);
                wpfplot.Plot.YAxis.MinorGrid(true);

                wpfplot.Plot.Legend(false);

                
            }

            try
            {
                wpfplot.Refresh();
            }
            catch
            {
            }
        }
    }

    public class ViewPoint : DependencyObject
    {
        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        // Using a DependencyProperty as the backing store for X.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(ViewPoint), new PropertyMetadata(double.NaN, XChanged));

        private static void XChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ViewPoint vp)
            {
                vp.DataChanged?.Invoke(vp, null);
            }
        }

        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Y.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(ViewPoint), new PropertyMetadata(double.NaN, YChanged));

        private static void YChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ViewPoint vp)
            {
                vp.DataChanged?.Invoke(vp, null);
            }
        }

        public event EventHandler DataChanged;

        public ViewPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public ViewPoint():this(0,0)
        {
        }
    }

    public class ViewCurveLimit : DependencyObject
    {
        public ObservableCollection<ViewPoint> USL
        {
            get { return (ObservableCollection<ViewPoint>)GetValue(USLProperty); }
            set { SetValue(USLProperty, value); }
        }

        // Using a DependencyProperty as the backing store for USL.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty USLProperty =
            DependencyProperty.Register("USL", typeof(ObservableCollection<ViewPoint>), typeof(ViewCurveLimit), new PropertyMetadata(null));

        public ObservableCollection<ViewPoint> LSL
        {
            get { return (ObservableCollection<ViewPoint>)GetValue(LSLProperty); }
            set { SetValue(LSLProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LSL.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LSLProperty =
            DependencyProperty.Register("LSL", typeof(ObservableCollection<ViewPoint>), typeof(ViewCurveLimit), new PropertyMetadata(null));

        public TF_Limit Limit { get; }

        public TF_Curve UslValue { get; private set; }
        public TF_Curve LslValue { get; private set; }

        public ScatterPlotDraggable UslPlot { get; }
        public ScatterPlotDraggable LslPlot { get; }

        public string Name { get; }

        public ViewCurveLimit(TF_Limit limit, string name)
        {
            Limit = limit;

            if (limit.USL is TF_Curve cv)
            {
                USL = new ObservableCollection<ViewPoint>();
                UslValue = cv;
                var xs = cv.XLog ? cv.X.Select(x => Math.Log10(x)).ToArray() : cv.X;
                var ys = cv.YLog ? cv.Y.Select(x => Math.Log10(x)).ToArray() : cv.Y;

                UslPlot = new ScatterPlotDraggable(xs, ys)
                {
                    DragCursor = Cursor.Crosshair,
                    DragEnabled = true,
                };

                UslPlot.Label = $"{name}USL";
                for (int i = 0; i < cv.Length; i++)
                {
                    var p = new ViewPoint(cv.X[i], cv.Y[i]);
                    USL.Add(p);
                    p.DataChanged += EventUpdateUsl;
                }

                USL.CollectionChanged += USL_CollectionChanged;
            }
            if (limit.LSL is TF_Curve cvl)
            {
                LslValue = cvl;
                LSL = new ObservableCollection<ViewPoint>();
                var xs = cvl.XLog ? cvl.X.Select(x => Math.Log10(x)).ToArray() : cvl.X;
                var ys = cvl.YLog ? cvl.Y.Select(x => Math.Log10(x)).ToArray() : cvl.Y;

                LslPlot = new ScatterPlotDraggable(xs, ys)
                {
                    DragCursor = Cursor.Crosshair,
                    DragEnabled = true,
                };
                LslPlot.Label = $"{name}LSL";
                for (int i = 0; i < cvl.Length; i++)
                {
                    var p = new ViewPoint(cvl.X[i], cvl.Y[i]);
                    LSL.Add(p);
                    p.DataChanged += EventUpdateLsl;
                }
                LSL.CollectionChanged += LSL_CollectionChanged;
            }
            Name = name;
        }

        private void LSL_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ViewPoint vp)
                    {
                        vp.DataChanged += EventUpdateLsl;
                    }
                }
            }

            EventUpdateLsl(this, null);
        }

        private void EventUpdateLsl(object sender, EventArgs args)
        {
            LslValue = new TF_Curve(LSL.Select(x => x.X).ToArray(), LSL.Select(x => x.Y).ToArray(), LslValue.X_Unit, LslValue.Y_Unit, LslValue.XLog, LslValue.YLog);
            var xs = LslValue.XLog ? LSL.Select(x => Math.Log10(x.X)).ToArray() : LslValue.X;
            var ys = LslValue.YLog ? LSL.Select(x => Math.Log10(x.Y)).ToArray() : LslValue.Y;

            Limit.LSL = LslValue;
            LslPlot.Update(xs, ys);
        }

        private void USL_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is ViewPoint vp)
                    {
                        vp.DataChanged += EventUpdateUsl;
                    }
                }
            }
        }

        private void EventUpdateUsl(object sender, EventArgs args)
        {
            UslValue = new TF_Curve(USL.Select(x => x.X).ToArray(), USL.Select(x => x.Y).ToArray(), UslValue.X_Unit, UslValue.Y_Unit, UslValue.XLog, UslValue.YLog);
            var xs = UslValue.XLog ? USL.Select(x => Math.Log10(x.X)).ToArray() : UslValue.X;
            var ys = UslValue.YLog ? USL.Select(x => Math.Log10(x.Y)).ToArray() : UslValue.Y;

            Limit.USL = UslValue;
            UslPlot.Update(xs, ys);
        }

        public void UpdateUslValue(double[] xs, double[] ys)
        {
            if (Limit.USL is TF_Curve cv)
            {
                Limit.USL = new TF_Curve(xs, ys, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);
                USL.Clear();

                for (int i = 0; i < xs.Length; i++)
                {
                    USL.Add(new ViewPoint( xs[i], ys[i]));
                }
            }
            else if(Limit.LSL is TF_Curve cvl)
            {
                Limit.USL = new TF_Curve(xs, ys, cvl.X_Unit, cvl.Y_Unit, cvl.XLog, cvl.YLog);
                USL.Clear();

                for (int i = 0; i < xs.Length; i++)
                {
                    USL.Add(new ViewPoint(xs[i], ys[i]));
                }
            }
        }

        public void UpdateLslValue(double[] xs, double[] ys)
        {
            if (Limit.LSL is TF_Curve cv)
            {
                Limit.LSL = new TF_Curve(xs, ys, cv.X_Unit, cv.Y_Unit, cv.XLog, cv.YLog);
                LSL.Clear();

                for (int i = 0; i < xs.Length; i++)
                {
                    LSL.Add(new ViewPoint(xs[i], ys[i]));
                }
            }
            else if (Limit.USL is TF_Curve cvl)
            {
                Limit.LSL = new TF_Curve(xs, ys, cvl.X_Unit, cvl.Y_Unit, cvl.XLog, cvl.YLog);
                LSL.Clear();

                for (int i = 0; i < xs.Length; i++)
                {
                    LSL.Add(new ViewPoint(xs[i], ys[i]));
                }
            }
        }
    }
}
