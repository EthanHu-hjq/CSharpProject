using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestCore.Data;
using TestCore;
using ScottPlot;

namespace Toucan_WPF.ViewModels
{
    internal static class UIUtilities
    {
        public static List<TF_ItemData> ItemContainsInfinity = new List<TF_ItemData>();
        static string logTickLabels(double y) => Math.Pow(10, y).ToString();
        static string logTickLabels_N0(double y) => Math.Pow(10, y).ToString("N0");
        static string logTickLabels_G(double y) => Math.Pow(10, y).ToString("G");

        public static void RefreshData(WpfPlot self, Nest<TF_StepData> data)
        {
            if (data is null) return;

            if (data.Element is TF_ItemData itemdata)
            {
                bool hasinfinity = ItemContainsInfinity.Contains(itemdata);

                try
                {
                    if (itemdata.Limit.Comp == Comparison.NULL)
                    {
                        if (data.Children?.FirstOrDefault()?.Element is TF_ItemData pitemdata)
                        {
                            if (pitemdata.Value is double)
                            {
                                self.Plot.Clear();

                                double[] value_d = new double[data.Count];
                                double[] hl = new double[data.Count];
                                double[] ll = new double[data.Count];
                                string[] labels = new string[data.Count];
                                double[] ys = new double[data.Count];

                                bool havelimit = false;
                                for (int i = 0; i < data.Count; i++)
                                {
                                    if (data[i].Element is TF_ItemData item)
                                    {
                                        value_d[i] = (double)item.Value;

                                        if (item.Limit.USL is double hlv)
                                        {
                                            hl[i] = hlv - value_d[i];
                                            if (!double.IsNaN(hlv))
                                            {
                                                havelimit = true;
                                            }
                                        }
                                        else
                                        {
                                            hl[i] = double.NaN;
                                        }

                                        if (item.Limit.LSL is double llv)
                                        {
                                            ll[i] = value_d[i] - llv;
                                            if (!double.IsNaN(llv))
                                            {
                                                havelimit = true;
                                            }
                                        }
                                        else
                                        {
                                            ll[i] = double.NaN;
                                        }
                                    }

                                    labels[i] = data[i].Element.Name;
                                    ys[i] = i;
                                }

                                self.Plot.YTicks(labels);
                                self.Plot.YAxis.MinorLogScale(false);

                                var barplot = self.Plot.AddBar(value_d);

                                barplot.Orientation = ScottPlot.Orientation.Horizontal;
                                barplot.ShowValuesAboveBars = true;
                                barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });

                                if (havelimit)
                                {
                                    var ebplot = self.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                    ebplot.LineWidth = 1;
                                    ebplot.MarkerSize = 10;
                                }

                                self.Plot.XAxis.Label(pitemdata.Limit.Unit);
                                self.Plot.XAxis.TickLabelFormat((double a) => { return a.ToString(); });
                                self.Plot.XAxis.MinorLogScale(false);
                                self.Plot.YLabel(null);

                                self.Plot.Title($"{data.Element.Name}");
                                self.Plot.Legend();

                                self.Refresh();
                            }
                            else if (pitemdata.Value is TF_Curve cv_template)
                            {
                                self.Plot.Clear();
                                self.Plot.ResetLayout();
                                for (int i = 0; i < data.Count; i++)
                                {
                                    if (((TF_ItemData)data[i].Element).Value is TF_Curve cv)
                                    {
                                        if (cv.Length == 0) continue;

                                        var xs = cv.XLog ? cv.X.Select(x => Math.Log10(x)).ToArray() : cv.X;
                                        var ys = cv.YLog ? cv.Y.Select(x => Math.Log10(x)).ToArray() : cv.Y;

                                        if (hasinfinity)
                                        {
                                            ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                            xs = xs.Take(ys.Length).ToArray();
                                        }

                                        var plot = self.Plot.AddScatter(xs, ys, markerSize: 0);

                                        if (((TF_ItemData)data[i].Element)?.Limit?.Tag is TF_Limit template)
                                        {
                                            if (template.LSL is TF_Curve llcv)
                                            {
                                                if (llcv.Length > 0)
                                                {
                                                    var llplot = self.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                                    llplot.Label = $"{data[i].Element.Name}_LL";
                                                }
                                            }

                                            if (template.USL is TF_Curve hlcv)
                                            {
                                                if (hlcv.Length > 0)
                                                {
                                                    var hlplot = self.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                                    hlplot.Label = $"{data[i].Element.Name}_HL";
                                                }
                                            }
                                        }

                                        plot.Label = data[i].Element.Name;
                                    }
                                }

                                //self.Plot.XTicks();
                                if (cv_template.XLog)
                                {
                                    self.Plot.XAxis.TickLabelFormat(logTickLabels);
                                }
                                else
                                {
                                    self.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                self.Plot.XAxis.MinorLogScale(cv_template.XLog);
                                self.Plot.XAxis.MinorGrid(true);

                                //self.Plot.YTicks();
                                if (cv_template.YLog)
                                {
                                    self.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                                }
                                else
                                {
                                    self.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                                }
                                self.Plot.YAxis.MinorLogScale(cv_template.YLog);
                                self.Plot.YAxis.MinorGrid(true);

                                self.Plot.YAxis.AutomaticTickPositions();

                                self.Plot.XAxis.Label(cv_template.X_Unit);
                                self.Plot.YAxis.Label(cv_template.Y_Unit);

                                self.Plot.Title(data.Element.Name);

                                self.Plot.Legend();

                                self.Refresh();
                            }
                        }
                    }
                    else
                    {
                        if (itemdata.Value is double vd)
                        {
                            self.Plot.Clear();

                            double[] value_d = new double[1] { vd };
                            double[] hl = new double[1];
                            double[] ll = new double[1];
                            double[] ys = new double[1] { 0 };
                            if (itemdata.Limit.USL is double hlv)
                            {
                                hl[0] = hlv - vd;
                            }
                            else
                            {
                                hl[0] = double.NaN;
                            }

                            if (itemdata.Limit.LSL is double llv)
                            {
                                ll[0] = vd - llv;
                            }
                            else
                            {
                                ll[0] = double.NaN;
                            }

                            var barplot = self.Plot.AddBar(new double[1] { vd });
                            barplot.Orientation = ScottPlot.Orientation.Horizontal;
                            barplot.Label = itemdata.Name;
                            barplot.ShowValuesAboveBars = true;
                            barplot.ValueFormatter = new Func<double, string>((val) => { return $"{val:N3}"; });

                            if (double.IsNaN(ll[0]) && double.IsNaN(hl[0]))
                            {
                            }
                            else
                            {
                                var ebplot = self.Plot.AddErrorBars(value_d, ys, hl, ll, null, null, null, 5);
                                ebplot.LineWidth = 3;
                                ebplot.MarkerSize = 10;
                            }

                            self.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            self.Plot.YTicks(new string[1] { itemdata.Name });
                            self.Plot.XLabel(itemdata.Limit?.Unit);
                            self.Plot.YLabel(null);
                            self.Plot.Title(null);
                            self.Plot.XAxis.MinorLogScale(false);

                            self.Refresh();
                        }
                        else if (itemdata.Value is TF_Curve cv)
                        {
                            self.Plot.Clear();
                            //self.Plot.YTicks(new string[0]{ });  // this will make Ytick to be null

                            if (cv.Length == 0) return;
                            double[] xs = null;
                            double[] ys = null;

                            //double[] hls = null;
                            //double[] lls = null;

                            if (cv.XLog)
                            {
                                xs = cv.X.Select(x => Math.Log10(x)).ToArray();
                            }
                            else
                            {
                                xs = cv.X;
                            }

                            if (cv.YLog)
                            {
                                ys = cv.Y.Select(x => Math.Log10(x)).ToArray();
                            }
                            else
                            {
                                ys = cv.Y;
                            }

                            if (hasinfinity)
                            {
                                ys = ys.TakeWhile(x => !double.IsInfinity(x)).ToArray();
                                xs = xs.Take(ys.Length).ToArray();
                            }

                            var plot = self.Plot.AddScatter(xs, ys, markerSize: 0);  // Ys might contains Infinity or NaN
                            //plot.OnNaN = ScottPlot.Plottable.ScatterPlot.NanBehavior.Ignore;  // need update library. .netfx 4.6.2 required
                            plot.Label = itemdata.Name;

                            if (itemdata.Limit.Tag is TF_Limit template)
                            {
                                if (template.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        var llplot = self.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                    }
                                }

                                if (template.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = self.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                    }
                                }
                            }
                            else
                            {
                                if (itemdata.Limit.LSL is TF_Curve llcv)
                                {
                                    if (llcv.Length > 0)
                                    {
                                        var llplot = self.Plot.AddScatter(cv.XLog ? llcv.X.Select(x => Math.Log10(x)).ToArray() : llcv.X, cv.YLog ? llcv.Y.Select(x => Math.Log10(x)).ToArray() : llcv.Y, System.Drawing.Color.OrangeRed, markerSize: 0);
                                        llplot.Label = $"{itemdata.Name}_LL";
                                    }
                                }

                                if (itemdata.Limit.USL is TF_Curve hlcv)
                                {
                                    if (hlcv.Length > 0)
                                    {
                                        var hlplot = self.Plot.AddScatter(cv.XLog ? hlcv.X.Select(x => Math.Log10(x)).ToArray() : hlcv.X, cv.YLog ? hlcv.Y.Select(x => Math.Log10(x)).ToArray() : hlcv.Y, System.Drawing.Color.IndianRed, markerSize: 0);
                                        hlplot.Label = $"{itemdata.Name}_HL";
                                    }
                                }
                            }

                            if (cv.XLog)
                            {
                                self.Plot.XAxis.TickLabelFormat(logTickLabels);
                            }
                            else
                            {
                                self.Plot.XAxis.TickLabelFormat((double x) => { return $"{x}"; });

                            }
                            self.Plot.XAxis.MinorLogScale(cv.XLog);
                            self.Plot.XAxis.MinorGrid(true);

                            //self.Plot.YTicks();
                            if (cv.YLog)
                            {
                                self.Plot.YAxis.TickLabelFormat(logTickLabels_G);

                            }
                            else
                            {
                                self.Plot.YAxis.TickLabelFormat((double x) => { return $"{x}"; });
                            }
                            self.Plot.YAxis.MinorLogScale(cv.YLog);
                            self.Plot.YAxis.MinorGrid(true);

                            self.Plot.YAxis.AutomaticTickPositions();

                            self.Plot.XAxis.Label(cv.X_Unit);
                            self.Plot.YAxis.Label(cv.Y_Unit);

                            self.Plot.Title(itemdata.Name);
                            self.Plot.Legend();

                            self.Refresh();
                        }
                    }
                }
                catch (InvalidOperationException ioex)
                {
                    self.UILog($"Draw Graph Error on {itemdata.Name}. Err: {ioex.Message}");
                    if (!hasinfinity)
                    {
                        ItemContainsInfinity.Add(itemdata);
                        RefreshData(self, data);
                    }
                }
                catch (Exception e)
                {
                    self.UILog(e.Message);
                }
            }
        }
    }
}
