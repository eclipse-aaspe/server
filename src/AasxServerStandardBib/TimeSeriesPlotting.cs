using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using AasxServer;
using AdminShellNS;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using ScottPlot;
using static AdminShellNS.AdminShellV20;

namespace AasxServerStandardBib
{
    public class TimeSeriesPlotting
    {
        public enum TimeSeriesTimeAxis { None, Utc, Tai, Plain, Duration }

        public class ListOfTimeSeriesData : List<TimeSeriesData>
        {
            public TimeSeriesData FindDataSetBySource(AdminShell.SubmodelElementCollection smcts)
            {
                if (smcts == null)
                    return null;

                foreach (var tsd in this)
                    if (tsd?.SourceTimeSeries == smcts)
                        return tsd;
                return null;
            }

            public static CumulativeDataItems GenerateCumulativeDataItems(
                List<Tuple<TimeSeriesDataSet, double>> cum, string defaultLang)
            {
                var res = new CumulativeDataItems();
                if (cum == null)
                    return res;
                double pos = 0.0;
                foreach (var cm in cum)
                {
                    var ds = cm.Item1;
                    if (ds == null)
                        continue;

                    //var lab = PlotHelpers.EvalDisplayText("" + ds.DataSetId, ds.DataPoint, ds.DataPointCD,
                    //    addMinimalTxt: false, defaultLang: defaultLang, useIdShort: true);
                    var lab = "auskommentiert wegen WPF";

                    res.Value.Add(cm.Item2);
                    res.Label.Add(lab);
                    res.Position.Add(pos);
                    pos += 1.0;
                }
                return res;
            }

            //public ScottPlot.Plottable.IPlottable GenerateCumulativePlottable(
            //    ScottPlot.WpfPlot wpfPlot,
            //    CumulativeDataItems cumdi,
            //    PlotArguments args)
            //{
            //    // access
            //    if (wpfPlot == null || cumdi == null || args == null)
            //        return null;

            //    if (args.type == PlotArguments.Type.Pie)
            //    {
            //        var pie = wpfPlot.Plot.AddPie(cumdi.Value.ToArray());
            //        pie.SliceLabels = cumdi.Label.ToArray();
            //        pie.ShowLabels = args.labels;
            //        pie.ShowValues = args.values;
            //        pie.ShowPercentages = args.percent;
            //        pie.SliceFont.Size = 9.0f;
            //        return pie;
            //    }

            //    if (args.type == PlotArguments.Type.Bars)
            //    {
            //        var bar = wpfPlot.Plot.AddBar(cumdi.Value.ToArray());
            //        wpfPlot.Plot.XTicks(cumdi.Position.ToArray(), cumdi.Label.ToArray());
            //        bar.ShowValuesAboveBars = args.values;
            //        return bar;
            //    }

            //    return null;
            //}

            public List<IWpfPlotViewControl> RenderTimeSeries(double defPlotHeight, string defaultLang, int sessionNumber, DateTime filterFromDate, DateTime filterToDate)
            {
                // first access
                var res = new List<IWpfPlotViewControl>();
                //if (panel == null)
                //    return null;
                //panel.Children.Clear();

                // ScottPlot.Plot plt = new ScottPlot.Plot(2400, 1200);
                ScottPlot.Plot plt = new ScottPlot.Plot(1800, 900);

                // go over all groups                
                foreach (var tsd in this)
                {
                    // skip?
                    if (tsd.Args?.skip == true)
                        continue;

                    // which kind of chart?
                    if (tsd.Args != null &&
                        (tsd.Args.type == PlotArguments.Type.Bars
                            || tsd.Args.type == PlotArguments.Type.Pie))
                    {
                        //
                        // Cumulative plots (e.g. the last sample, bars, pie, ..)
                        //

                        tsd.RederedCumulative = true;

                        // start new group
                        //var pvc = new WpfPlotViewControlCumulative();
                        //tsd.UsedPlot = pvc;
                        //pvc.Text = PlotHelpers.EvalDisplayText("Cumulative plot",
                        //    tsd.SourceTimeSeries, defaultLang: defaultLang);
                        //var wpfPlot = pvc.WpfPlot;
                        //if (wpfPlot == null)
                        //    continue;

                        // initial state
                        PlotHelpers.SetOverallPlotProperties(null, plt, tsd.Args, defPlotHeight);

                        // generate cumulative data
                        //var cum = tsd.DataSet.GenerateCumulativeData(pvc.LatestSamplePosition);
                        var cum = tsd.DataSet.GenerateCumulativeData(10); // TODO: Hardcoded value -1 instead of using LatestSamplePosition
                        var cumdi = GenerateCumulativeDataItems(cum, defaultLang);
                        var plottable = GenerateCumulativePlottable(plt, cumdi, tsd.Args);
                        if (plottable == null)
                            continue;
                        //pvc.ActivePlottable = plottable;

                        // render the plottable into panel
                        //panel.Children.Add(pvc);
                        plt.Render(/* skipIfCurrentlyRendering: true */);

                        plt.SaveFig("wwwroot/images/scottplot/smc_timeseries_clientid" + sessionNumber + ".png");

                        //res.Add(plt); //TODO: Cannot use plt
                    }
                    else
                    {
                        //
                        // Time series based plots (scatter, bars)
                        //

                        tsd.RederedCumulative = false;

                        // start new group
                        //var pvc = new WpfPlotViewControlHorizontal();
                        //tsd.UsedPlot = pvc;
                        //pvc.Text = PlotHelpers.EvalDisplayText("Time Series plot",
                        //    tsd.SourceTimeSeries, defaultLang: defaultLang);
                        //pvc.AutoScaleX = true;
                        //pvc.AutoScaleY = true;
                        //var wpfPlot = pvc.WpfPlot;
                        //if (wpfPlot == null)
                        //    continue;

                        // initial state
                        PlotHelpers.SetOverallPlotProperties(null, plt, tsd.Args, defPlotHeight);

                        ScottPlot.Plot lastPlot = null;
                        var xLabels = "Time ( ";
                        int yAxisNum = 0;

                        // TODO: wpfPlot name variable
                        var wpfPlot = plt;

                        // some basic attributes
                        lastPlot = wpfPlot;

                        // make a list of plottables in order to sort by order
                        var moveOrder = new List<Tuple<TimeSeriesDataSet, int?>>();

                        // for each signal
                        double? yMin = null, yMax = null;
                        TimeSeriesDataSet lastTimeRecord = null;

                        foreach (var tsds in tsd.DataSet)
                        {
                            // skip?
                            if (tsds.Args?.skip == true)
                                continue;

                            // if its a time axis, skip but remember for following axes
                            if (tsds.TimeAxis != TimeSeriesTimeAxis.None)
                            {
                                lastTimeRecord = tsds;
                                xLabels += "" + tsds.DataSetId + " ";
                                continue;
                            }

                            // add to later sort order
                            moveOrder.Add(new Tuple<TimeSeriesDataSet, int?>(tsds, tsds.Args?.order));

                            // cannot render without time record
                            var timeDStoUse = tsds.AssignedTimeDS;
                            if (timeDStoUse == null)
                                timeDStoUse = lastTimeRecord;
                            if (timeDStoUse == null)
                                continue;
                            tsds.AssignedTimeDS = timeDStoUse;

                            // Create filtered x- and y-axis lists
                            List<double> xsFilteredList = new List<double>();
                            List<double> ysFilteredList = new List<double>();

                            // Apply date filter
                            for (int i = 0; i < tsds.AssignedTimeDS.Data.Length; i++)
                            {
                                DateTime xsTimestampDateTime = DateTime.FromOADate(tsds.AssignedTimeDS.Data[i]);
                                // is in date filter range
                                if (xsTimestampDateTime >= filterFromDate && xsTimestampDateTime <= filterToDate)
                                {
                                    // add values to filtered list
                                    xsFilteredList.Add(tsds.AssignedTimeDS.Data[i]);
                                    ysFilteredList.Add(tsds.Data[i]);
                                }
                            }

                            // compare (fix?) render limits
                            var rlt = timeDStoUse.GetRenderLimits();
                            var rld = tsds.GetRenderLimits();
                            if (rlt == null || rld == null || rlt.Min != rld.Min || rlt.Max != rld.Max)
                            {
                                // TODO: error output
                                //log?.Error($"When rendering data set {tsds.DataSetId} different " +
                                //    $"dimensions for X and Y.");
                                Console.WriteLine($"When rendering data set {tsds.DataSetId} different " + $"dimensions for X and Y.");
                                continue;
                            }

                            // integrate args
                            if (tsds.Args != null)
                            {
                                if (tsds.Args.ymin.HasValue)
                                    yMin = Nullable.Compare(tsds.Args.ymin, yMin) > 0 ? tsds.Args.ymin : yMin;

                                if (tsds.Args.ymax.HasValue)
                                    yMax = Nullable.Compare(yMax, tsds.Args.ymax) > 0 ? yMax : tsds.Args.ymax;
                            }

                            // factory new Plottable

                            ScottPlot.Plottable.BarPlot bars = null;
                            ScottPlot.Plottable.ScatterPlot scatter = null;

                            if (tsds.Args != null && tsds.Args.type == PlotArguments.Type.Bars)
                            {
                                // Bars
                                bars = wpfPlot.AddBar(
                                    positions: timeDStoUse.RenderDataToLimits(),
                                    values: tsds.RenderDataToLimits());

                                PlotHelpers.SetPlottableProperties(scatter, tsds.Args);

                                // customize the width of bars (80% of the inter-position distance looks good)
                                if (timeDStoUse.Data.Length >= 2)
                                {
                                    // Note: pretty trivial approach, yet
                                    var timeDelta = (timeDStoUse.Data[1] - timeDStoUse.Data[0]);
                                    var bw = timeDelta * .8;

                                    // apply bar width?
                                    if (tsds.Args?.barwidth != null)
                                        bw *= Math.Max(0.0, Math.Min(1.0, (tsds.Args?.barwidth).Value));

                                    // set
                                    bars.BarWidth = bw;

                                    // bar ofsett
                                    var extraOfs = 0.0;
                                    if (tsds.Args?.barofs != null)
                                        extraOfs = (tsds.Args?.barofs).Value;
                                    var bo = timeDelta * (-0.0 + 0.5 * extraOfs);
                                    bars.PositionOffset = bo;

                                    // remember
                                    tsds.RenderedBarWidth = bw;
                                    tsds.RenderedBarOffet = bo;
                                }

                                bars.Label = PlotHelpers.EvalDisplayText("" + tsds.DataSetId,
                                    tsds.DataPoint, tsds.DataPointCD,
                                    addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false);

                                tsds.Plottable = bars;
                            }
                            else
                            {
                                if (xsFilteredList.Count > 0 && ysFilteredList.Count > 0)
                                {
                                    // Default: Scatter plot
                                    scatter = wpfPlot.AddScatter(
                                        //xs: timeDStoUse.Data,
                                        //ys: tsds.Data,
                                        xs: xsFilteredList.ToArray(),
                                        ys: ysFilteredList.ToArray(),
                                        label: PlotHelpers.EvalDisplayText("" + tsds.DataSetId,
                                            tsds.DataPoint, tsds.DataPointCD,
                                            addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false));

                                    PlotHelpers.SetPlottableProperties(scatter, tsds.Args);

                                    tsds.Plottable = scatter;
                                    // TODO: Need to update time series data set ... !
                                    //var rl = tsds.GetRenderLimits();
                                    //scatter.MinRenderIndex = rl.Min;
                                    //scatter.MaxRenderIndex = rl.Max;
                                }
                            }

                            // axis treatment?
                            bool sameAxis = (tsds.Args?.sameaxis == true) && (yAxisNum != 0);
                            int assignAxis = -1;
                            ScottPlot.Renderable.Axis yAxis3 = null;
                            if (!sameAxis)
                            {
                                yAxisNum++;
                                if (yAxisNum >= 2)
                                {
                                    yAxis3 = wpfPlot.AddAxis(ScottPlot.Renderable.Edge.Right, axisIndex: yAxisNum);
                                    assignAxis = yAxisNum;
                                }
                            }
                            else
                                // take last one
                                assignAxis = yAxisNum - 1;

                            if (assignAxis >= 0)
                            {
                                tsds.RenderedYaxisIndex = assignAxis;

                                if (scatter != null)
                                {
                                    scatter.YAxisIndex = assignAxis;
                                    if (yAxis3 != null)
                                        yAxis3.Color(scatter.Color);
                                }

                                if (bars != null)
                                {
                                    bars.YAxisIndex = assignAxis;
                                    if (yAxis3 != null)
                                        yAxis3.Color(bars.FillColor);
                                }
                            }
                        }

                        // now sort for order
                        moveOrder.Sort((mo1, mo2) => Nullable.Compare(mo1.Item2, mo2.Item2));
                        foreach (var mo in moveOrder)
                            if (mo?.Item1?.Plottable != null)
                                wpfPlot.MoveLast(mo.Item1.Plottable);

                        // apply some more args to the group
                        if (yMin.HasValue)
                        {
                            wpfPlot.SetAxisLimits(yMin: yMin.Value);
                            //pvc.AutoScaleY = false;
                        }

                        if (yMax.HasValue)
                        {
                            wpfPlot.SetAxisLimits(yMax: yMax.Value);
                            //pvc.AutoScaleY = false;
                        }

                        // time axis
                        if (lastTimeRecord != null &&
                            (lastTimeRecord.TimeAxis == TimeSeriesTimeAxis.Utc
                                || lastTimeRecord.TimeAxis == TimeSeriesTimeAxis.Tai))
                        {
                            wpfPlot.XAxis.DateTimeFormat(true);
                        }

                        // for the last plot ..
                        if (true /* lastPlot != null */)
                        {
                            xLabels += ")";
                            lastPlot.XLabel(xLabels);
                        }

                        // render the plot into panel                        
                        //panel.Children.Add(pvc);
                        //pvc.ButtonClick += (sender, ndx) =>
                        //{
                        //    if (ndx == 5)
                        //    {
                        //        // perform a customised X/Y axis reset

                        //        // for X find UTC timescale and reset manually
                        //        var tsutc = tsd.FindDataForTimeAxis(findUtc: true, findTai: true);
                        //        if (tsutc?.ValueLimits != null
                        //            && tsutc.ValueLimits.IsValid)
                        //        {
                        //            var sp = tsutc.ValueLimits.Span;
                        //            wpfPlot.Plot.SetAxisLimitsX(
                        //                tsutc.ValueLimits.Min - 0.05 * sp, tsutc.ValueLimits.Max + 0.05 * sp);
                        //        }

                        //        // for Y used default
                        //        wpfPlot.Plot.AxisAutoY();

                        //        // commit
                        //        wpfPlot.Render();

                        //        // no default
                        //        return;
                        //    }
                        //    pvc.DefaultButtonClicked(sender, ndx);
                        //};
                        //wpfPlot.Render();
                        wpfPlot.SaveFig("wwwroot/images/scottplot/smc_timeseries_clientid" + sessionNumber + ".png");
                        //res.Add(pvc);
                    }
                }

                // return groups for notice
                return res;
            }

            //        public List<int> RefreshRenderedTimeSeries(StackPanel panel, string defaultLang, LogInstance log)
            //        {
            //            // first access
            //            var res = new List<int>();
            //            if (panel == null)
            //                return null;

            //            // go over all groups                
            //            foreach (var tsd in this)
            //            {
            //                // skip?
            //                if (tsd.Args?.skip == true)
            //                    continue;

            //                // general plot data
            //                if (tsd.UsedPlot is IWpfPlotViewControl ipvc)
            //                {
            //                    ipvc.Text = PlotHelpers.EvalDisplayText("Time Series plot",
            //                        tsd.SourceTimeSeries, defaultLang: defaultLang);
            //                }

            //                // which kind of chart?
            //                if (tsd.RederedCumulative)
            //                {
            //                    //
            //                    // Cumulative plots (e.g. the last sample, bars, pie, ..)
            //                    //

            //                    if (tsd.UsedPlot is WpfPlotViewControlCumulative pvc)
            //                    {
            //                        // remove old chart
            //                        if (pvc.ActivePlottable != null)
            //                            tsd.UsedPlot.WpfPlot.Plot.Remove(pvc.ActivePlottable);

            //                        // generate cumulative data
            //                        var cum = tsd.DataSet.GenerateCumulativeData(pvc.LatestSamplePosition);
            //                        var cumdi = GenerateCumulativeDataItems(cum, defaultLang);
            //                        var plottable = GenerateCumulativePlottable(tsd.UsedPlot.WpfPlot, cumdi, tsd.Args);
            //                        if (plottable == null)
            //                            continue;
            //                        pvc.ActivePlottable = plottable;

            //                        // render the plot into panel
            //                        tsd.UsedPlot.WpfPlot.Render();
            //                    }
            //                }
            //                else
            //                {
            //                    //
            //                    // Time series based plots (scatter, bars)
            //                    //

            //                    // Note: the original approach for re-rendering was to leave the individual plottables
            //                    // in place and to replace/ "update" the data. But:
            //                    // For Bars: not supported by the API
            //                    // For Scatter: Update() provided, but same strange errors led to non-rendering charts
            //                    // Therefore: the plottables are tediously recreated; this is not optimal in terms
            //                    // of performance!

            //                    // access
            //                    if (tsd.UsedPlot?.WpfPlot == null)
            //                        continue;

            //                    double maxRenderX = double.MinValue;

            //                    // find valid data sets
            //                    foreach (var tsds in tsd.DataSet)
            //                    {
            //                        // skip?
            //                        if (tsds.Args?.skip == true)
            //                            continue;

            //                        // required to be no time axis, but to have a time axis
            //                        if (tsds.TimeAxis != TimeSeriesTimeAxis.None)
            //                            continue;
            //                        if (tsds.AssignedTimeDS == null)
            //                            continue;

            //                        // compare (fix?) render limits
            //                        var rlt = tsds.AssignedTimeDS.GetRenderLimits();
            //                        var rld = tsds.GetRenderLimits();
            //                        if (rlt == null || rld == null || rlt.Min != rld.Min || rlt.Max != rld.Max)
            //                        {
            //                            log?.Error($"When rendering data set {tsds.DataSetId} different " +
            //                                $"dimensions for X and Y.");
            //                            continue;
            //                        }

            //                        if (tsds.Plottable is ScottPlot.Plottable.BarPlot bars)
            //                        {
            //                            // need to remove old bars
            //                            var oldBars = bars;
            //                            tsd.UsedPlot.WpfPlot.Plot.Remove(bars);

            //                            // Bars (redefine!!)
            //                            bars = tsd.UsedPlot.WpfPlot.Plot.AddBar(
            //                                positions: tsds.AssignedTimeDS.RenderDataToLimits(),
            //                                values: tsds.RenderDataToLimits());
            //                            tsds.Plottable = bars;

            //                            // tedious re-assign of style
            //                            bars.FillColor = oldBars.FillColor;
            //                            bars.FillColorNegative = oldBars.FillColorNegative;
            //                            bars.FillColorHatch = oldBars.FillColorHatch;
            //                            bars.HatchStyle = oldBars.HatchStyle;
            //                            bars.BorderLineWidth = oldBars.BorderLineWidth;

            //                            // set Yaxis if available
            //                            if (tsds.RenderedYaxisIndex.HasValue)
            //                                bars.YAxisIndex = tsds.RenderedYaxisIndex.Value;

            //                            // always in the background
            //                            // Note: not in sync with sortorder!
            //                            tsd.UsedPlot.WpfPlot.Plot.MoveFirst(bars);

            //                            // use the already decided width, offset of bars
            //                            if (tsds.RenderedBarWidth.HasValue)
            //                                bars.BarWidth = tsds.RenderedBarWidth.Value;
            //                            if (tsds.RenderedBarOffet.HasValue)
            //                                bars.PositionOffset = tsds.RenderedBarOffet.Value;

            //                            bars.Label = PlotHelpers.EvalDisplayText("" + tsds.DataSetId,
            //                                tsds.DataPoint, tsds.DataPointCD,
            //                                addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false);

            //                            // eval latest X for later setting
            //                            var latestX = tsds.AssignedTimeDS.Data[rlt.Max];
            //                            if (latestX > maxRenderX)
            //                                maxRenderX = latestX;

            //                        }

            //                        if (tsds.Plottable is ScottPlot.Plottable.ScatterPlot scatter)
            //                        {
            //#if __not_working

            //                            // just set the render limits to new values
            //                            scatter.Update(tsds.AssignedTimeDS.Data, tsds.Data);
            //                                scatter.MinRenderIndex = rld.Min;
            //                                scatter.MaxRenderIndex = rld.Max;
            //#endif

            //                            // need to remove old bars
            //                            var oldScatter = scatter;
            //                            tsd.UsedPlot.WpfPlot.Plot.Remove(scatter);

            //                            // re-create scatter
            //                            scatter = tsd.UsedPlot.WpfPlot.Plot.AddScatter(
            //                                xs: tsds.AssignedTimeDS.RenderDataToLimits(),
            //                                ys: tsds.RenderDataToLimits(),
            //                                label: PlotHelpers.EvalDisplayText("" + tsds.DataSetId,
            //                                    tsds.DataPoint, tsds.DataPointCD,
            //                                    addMinimalTxt: true, defaultLang: defaultLang, useIdShort: false));

            //                            // tedious re-assign of style
            //                            scatter.Color = oldScatter.Color;
            //                            scatter.LineStyle = oldScatter.LineStyle;
            //                            scatter.MarkerShape = oldScatter.MarkerShape;
            //                            scatter.LineWidth = oldScatter.LineWidth;
            //                            scatter.ErrorLineWidth = oldScatter.ErrorLineWidth;
            //                            scatter.ErrorCapSize = oldScatter.ErrorCapSize;
            //                            scatter.MarkerSize = oldScatter.MarkerSize;
            //                            scatter.StepDisplay = oldScatter.StepDisplay;

            //                            // set Yaxis and other attributes if available
            //                            if (tsds.RenderedYaxisIndex.HasValue)
            //                                scatter.YAxisIndex = tsds.RenderedYaxisIndex.Value;

            //                            if (true == tsds.Args?.linewidth.HasValue)
            //                                scatter.LineWidth = tsds.Args.linewidth.Value;

            //                            if (true == tsds.Args?.markersize.HasValue)
            //                                scatter.MarkerSize = (float)tsds.Args.markersize.Value;

            //                            // always in the foreground
            //                            // Note: not in sync with sortorder!
            //                            tsd.UsedPlot.WpfPlot.Plot.MoveLast(scatter);

            //                            tsds.Plottable = scatter;

            //                            // eval latest X for later setting
            //                            var latestX = tsds.AssignedTimeDS.Data[rlt.Max];
            //                            if (latestX > maxRenderX)
            //                                maxRenderX = latestX;

            //                            if (tsd.UsedPlot.AutoScaleY
            //                                && tsds.ValueLimits.Min != double.MaxValue
            //                                && tsds.ValueLimits.Max != double.MinValue)
            //                            {
            //                                var ai = scatter.YAxisIndex;
            //                                var hy = tsds.ValueLimits.Max - tsds.ValueLimits.Min;
            //                                tsd.UsedPlot.WpfPlot.Plot.SetAxisLimits(
            //                                    yAxisIndex: ai,
            //                                    yMin: tsds.ValueLimits.Min - hy * 0.1,
            //                                    yMax: tsds.ValueLimits.Max + hy * 0.1);
            //                            }

            //                        }

            //                    }

            //                    // remain the zoom level, scroll to lastest x
            //                    if (maxRenderX != double.MinValue && tsd.UsedPlot.AutoScaleX)
            //                    {
            //                        var ax = tsd.UsedPlot.WpfPlot.Plot.GetAxisLimits();
            //                        var wx = (ax.XMax - ax.XMin);
            //                        var XMinNew = maxRenderX - 0.9 * wx;
            //                        var XMaxNew = maxRenderX + 0.1 * wx;
            //                        if (XMaxNew > ax.XMax)
            //                            tsd.UsedPlot.WpfPlot.Plot.SetAxisLimitsX(XMinNew, XMaxNew);
            //                    }

            //                    // render the plot into panel
            //                    tsd.UsedPlot.WpfPlot.Render();
            //                }
            //            }

            //            // return groups for notice
            //            return res;
            //        }
        }

        public class TimeSeriesData
        {
            public AdminShell.SubmodelElementCollection SourceTimeSeries;

            public PlotArguments Args;

            public ListOfTimeSeriesDataSet DataSet = new ListOfTimeSeriesDataSet();

            // the time series might have different time axis for the records (not the variables)
            public Dictionary<TimeSeriesTimeAxis, TimeSeriesDataSet> TimeDsLookup =
                new Dictionary<TimeSeriesTimeAxis, TimeSeriesDataSet>();

            //-- public IWpfPlotViewControl UsedPlot;
            public bool RederedCumulative;
            public PlotArguments.Type UsedType;

            public TimeSeriesDataSet FindDataSetById(string dsid)
            {
                foreach (var tsd in DataSet)
                    if (tsd?.DataSetId == dsid)
                        return tsd;
                return null;
            }

            public TimeSeriesDataSet FindDataForTimeAxis(bool findUtc = false, bool findTai = false)
            {
                foreach (var tsd in DataSet)
                {
                    if (tsd == null)
                        continue;
                    if (tsd.TimeAxis == TimeSeriesTimeAxis.Utc && findUtc)
                        return tsd;
                    if (tsd.TimeAxis == TimeSeriesTimeAxis.Tai && findTai)
                        return tsd;
                }
                return null;
            }
        }

        public class TimeSeriesDataSet
        {
            public string DataSetId = "";
            public TimeSeriesTimeAxis TimeAxis;
            public AdminShell.Property DataPoint;
            public AdminShell.ConceptDescription DataPointCD;

            public PlotArguments Args = null;

            public TimeSeriesDataSet AssignedTimeDS;

            public TimeSeriesMinMaxInt DsLimits = TimeSeriesMinMaxInt.Invalid;
            public TimeSeriesMinMaxDouble ValueLimits = TimeSeriesMinMaxDouble.Invalid;

            protected TimeSeriesMinMaxInt _dataLimits;
            protected double[] data = new[] { 0.0 };
            public double[] Data { get { return data; } }

            public ScottPlot.Plottable.IPlottable Plottable;

            public double? RenderedBarWidth, RenderedBarOffet;
            public int? RenderedYaxisIndex;

            public void DataAdd(string json, bool fillTimeGaps = false)
            {
                // intermediate format
                var temp = new ListOfTimeSeriesDataPoint(json, TimeAxis);
                if (temp.Count < 1)
                    return;

                // check to adapt current shape of data
                var tempLimits = temp.GetMinMaxIndex();

                // now, if the first time, start with good limits
                if (_dataLimits == null)
                    _dataLimits = new TimeSeriesMinMaxInt() { Min = tempLimits.Min, Max = tempLimits.Min };

                // extend to the left?
                if (tempLimits.Min < _dataLimits.Min)
                {
                    // how much
                    var delta = _dataLimits.Min - tempLimits.Min;

                    // adopt the limits
                    var oldsize = _dataLimits.Max - _dataLimits.Min + 1;
                    _dataLimits.Min -= delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);

                    // shift to right
                    Array.Copy(data, 0, data, delta, oldsize);
                }

                // extend to the right (fairly often the case)
                if (tempLimits.Max > _dataLimits.Max)
                {
                    // how much
                    var delta = tempLimits.Max - _dataLimits.Max;
                    _dataLimits.Max += delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);
                }

                // fill to the left?
                if (fillTimeGaps && temp[0].Value.HasValue)
                {
                    var startIndex = temp[0].Index;
                    var startVal = temp[0].Value.Value;
                    for (int i = startIndex - 1; i >= _dataLimits.Min; i--)
                    {
                        // data to the left present? -> stop
                        if (data[i - _dataLimits.Min] >= 1.0)
                            break;

                        // no, fill to the left
                        data[i - _dataLimits.Min] = startVal;
                    }
                }

                // now, populate
                foreach (var dp in temp)
                    if (dp.Value.HasValue)
                    {
                        data[dp.Index - _dataLimits.Min] = dp.Value.Value;

                        if (dp.Index < DsLimits.Min)
                            DsLimits.Min = dp.Index;
                        if (dp.Index > DsLimits.Max)
                            DsLimits.Max = dp.Index;

                        if (dp.Value.Value < ValueLimits.Min)
                            ValueLimits.Min = dp.Value.Value;
                        if (dp.Value.Value > ValueLimits.Max)
                            ValueLimits.Max = dp.Value.Value;
                    }
            }

            public void DataAdd(int index, double value, int headroom = 1024)
            {
                // now, if the first time, start with good limits
                if (_dataLimits == null)
                    _dataLimits = new TimeSeriesMinMaxInt() { Min = index, Max = index };

                // extend to the left?
                if (index < _dataLimits.Min)
                {
                    // how much
                    var delta = _dataLimits.Min - index;

                    // adopt the limits
                    var oldsize = _dataLimits.Max - _dataLimits.Min + 1;
                    _dataLimits.Min -= delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);

                    // shift to right
                    Array.Copy(data, 0, data, delta, oldsize);
                }

                // extend to the right (fairly often the case)
                if (index > _dataLimits.Max)
                {
                    // how much, but respect headroom
                    var delta = Math.Max(index - _dataLimits.Max, headroom);
                    _dataLimits.Max += delta;

                    // resize to these limits
                    Array.Resize<double>(ref data, _dataLimits.Max - _dataLimits.Min + 1);
                }

                // now, populate
                data[index + _dataLimits.Min] = value;

                if (index < DsLimits.Min)
                    DsLimits.Min = index;
                if (index > DsLimits.Max)
                    DsLimits.Max = index;

                if (value < ValueLimits.Min)
                    ValueLimits.Min = value;
                if (value > ValueLimits.Max)
                    ValueLimits.Max = value;
            }

            /// <summary>
            /// Get the min and max used index values w.r.t. to Data[]
            /// </summary>
            /// <returns>Null in case of array</returns>
            public TimeSeriesMinMaxInt GetRenderLimits()
            {
                // reasonable data?
                if (DsLimits.Min == int.MaxValue || DsLimits.Max == int.MinValue)
                    return null;

                var res = new TimeSeriesMinMaxInt();
                res.Min = DsLimits.Min - _dataLimits.Min;
                res.Max = DsLimits.Max - /* DsLimits.Min - */ _dataLimits.Min;
                return res;
            }

#if __old
        public double[] RenderDataToLimitsOLD()
        {
            var rl = GetRenderLimits();
            if (rl == null)
                return null;
                
            var res = new double[1 + rl.Max - rl.Min];
            for (int i = rl.Min; i <= rl.Max; i++)
                res[i - rl.Min] = data[i - _dataLimits.Min];
            return res;
        }
#endif

            public double[] RenderDataToLimits(TimeSeriesMinMaxInt lim = null)
            {
                // defaults?
                if (lim == null)
                    lim = DsLimits;

                // reasonable data?
                if (lim == null || !lim.IsValid)
                    return null;

                // render
                var res = new double[1 + lim.Max - lim.Min];
                for (int i = lim.Min; i <= lim.Max; i++)
                    res[i - lim.Min] = data[i - _dataLimits.Min];
                return res;
            }

            public double? this[int index]
            {
                get
                {
                    if (index < DsLimits.Min || index > DsLimits.Max)
                        return null;
                    return data[index - _dataLimits.Min];
                }
            }
        }

        public class TimeSeriesDataPoint
        {
            public int Index = 0;
            public string ValStr = "";
            public double? Value;
        }

        public class ListOfTimeSeriesDataPoint : List<TimeSeriesDataPoint>
        {
            public ListOfTimeSeriesDataPoint() : base() { }

            public ListOfTimeSeriesDataPoint(string json, TimeSeriesTimeAxis timeAxis) : base()
            {
                Add(json, timeAxis);
            }

            public TimeSeriesMinMaxInt GetMinMaxIndex()
            {
                if (this.Count < 1)
                    return new TimeSeriesMinMaxInt();
                var res = TimeSeriesMinMaxInt.Invalid;
                foreach (var dp in this)
                {
                    if (dp.Index < res.Min)
                        res.Min = dp.Index;
                    if (dp.Index > res.Max)
                        res.Max = dp.Index;
                }
                return res;
            }

            public void Add(string json, TimeSeriesTimeAxis timeAxis)
            {
                // simple state machine approch to pseudo parse-json
                var state = 0;
                int i = 0;
                var err = false;
                var bufIndex = "";
                var bufValue = "";

                while (!err && i < json.Length)
                {

                    switch (state)
                    {
                        // expecting inner '['
                        case 0:
                            if (json[i] == '[')
                            {
                                // prepare first buffer
                                state = 1;
                                bufIndex = "";
                                i++;
                            }
                            else
                            if (json[i] == ',' || Char.IsWhiteSpace(json[i]))
                            {
                                // ignore whitespace
                                i++;
                            }
                            else
                            {
                                // break with error
                                err = true;
                            }
                            break;

                        // parsing 1st buffer: index
                        case 1:
                            if (json[i] == ',')
                            {
                                // prepare second buffer
                                state = 2;
                                bufValue = "";
                                i++;
                            }
                            else
                            if ("0123456789-+.TZ\"".IndexOf(json[i]) >= 0)
                            {
                                bufIndex += json[i];
                                i++;
                            }
                            else
                            if (Char.IsWhiteSpace(json[i]))
                            {
                                // ignore whitespace
                                i++;
                            }
                            else
                            {
                                // break with error
                                err = true;
                            }
                            break;

                        // parsing 2nd buffer: value
                        case 2:
                            if (json[i] == ']')
                            {
                                // ok, finalize
                                if (int.TryParse(bufIndex, out int iIndex))
                                {
                                    var dp = new TimeSeriesDataPoint()
                                    {
                                        Index = iIndex,
                                        ValStr = bufValue
                                    };

                                    if (timeAxis == TimeSeriesTimeAxis.Utc || timeAxis == TimeSeriesTimeAxis.Tai)
                                    {
                                        // strict time string
                                        if (DateTime.TryParseExact(bufValue,
                                            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture,
                                            DateTimeStyles.AdjustToUniversal, out DateTime dt))
                                        {
                                            dp.Value = dt.ToOADate();
                                        }
                                        else
                                        {
                                            ;
                                        }
                                    }
                                    else
                                    {
                                        // plain time or plain value
                                        if (double.TryParse(bufValue, NumberStyles.Float,
                                                CultureInfo.InvariantCulture, out double fValue))
                                            dp.Value = fValue;
                                    }
                                    this.Add(dp);
                                }

                                // next sample
                                state = 0;
                                i++;
                            }
                            else
                            if ("0123456789-+.:TZ\"".IndexOf(json[i]) >= 0)
                            {
                                bufValue += json[i];
                                i++;
                            }
                            else
                            if (Char.IsWhiteSpace(json[i]))
                            {
                                // ignore whitespace
                                i++;
                            }
                            else
                            {
                                // break with error
                                err = true;
                            }
                            break;
                    }

                }

            }
        }

        public class TimeSeriesMinMaxDouble
        {
            public double Min;
            public double Max;
            public bool IsValid { get { return Min != double.MaxValue && Max != double.MinValue; } }
            public double Span { get { return Max - Min; } }
            public static TimeSeriesMinMaxDouble Invalid =>
                new TimeSeriesMinMaxDouble() { Min = double.MaxValue, Max = double.MinValue };
        }

        public class TimeSeriesMinMaxInt
        {
            public int Min;
            public int Max;
            public bool IsValid { get { return Min != int.MaxValue && Max != int.MinValue; } }
            public int Span { get { return Max - Min; } }
            public static TimeSeriesMinMaxInt Invalid =>
                new TimeSeriesMinMaxInt() { Min = int.MaxValue, Max = int.MinValue };
        }

        public class ListOfTimeSeriesDataSet : List<TimeSeriesDataSet>
        {
            /// <summary>
            /// Only get the latest sample from the different data sets.
            /// </summary>
            /// <param name="sampleOffset">Negative value for offset from latest sample</param>
            public List<Tuple<TimeSeriesDataSet, double>> GenerateCumulativeData(int sampleOffset)
            {
                var res = new List<Tuple<TimeSeriesDataSet, double>>();

                foreach (var ds in this)
                {
                    // do not allow empty or time axis data sets
                    if (ds == null || ds.TimeAxis != TimeSeriesTimeAxis.None)
                        continue;

                    // get the last sample
                    var rl = ds.GetRenderLimits();
                    if (rl == null)
                        continue;
                    var i = rl.Max + sampleOffset;
                    if (i < rl.Min)
                        i = rl.Min;
                    var lastVal = ds[i];
                    if (!lastVal.HasValue)
                        continue;

                    // add
                    res.Add(new Tuple<TimeSeriesDataSet, double>(ds, lastVal.Value));
                }

                return res;
            }
        }

        public class PlotArguments
        {
            // ReSharper disable UnassignedField.Global

            /// <summary>
            /// Display title of the respective entity to be shown in the panel
            /// </summary>
            public string title;

            /// <summary>
            /// Symbolic name of a group, a plot shall assigned to
            /// </summary>            
            public string grp;

            /// <summary>
            /// C# string format string to format a double value pretty.
            /// Note: e.g. F4
            /// </summary>
            public string fmt;

            /// <summary>
            /// Unit to display.
            /// </summary>
            public string unit;

            /// <summary>
            /// Min and max values of the axes
            /// </summary>
            public double? xmin, ymin, xmax, ymax;

            /// <summary>
            /// Skip this plot in charts display
            /// </summary>
            public bool skip;

            /// <summary>
            /// Keep the plot on the same Y axis as the plot before
            /// </summary>
            public bool sameaxis;

            /// <summary>
            /// Plottables will be shown with ascending order
            /// </summary>
            public int order = -1;

            /// <summary>
            /// Width of plot line, size of its markers
            /// </summary>
            public double? linewidth, markersize;

            /// <summary>
            /// In order to display more than one bar plottable, set the bar-width to 0.5 or 0.33
            /// and the bar-offset to -0.5 .. +0.5
            /// </summary>
            public double? barwidth, barofs;

            /// <summary>
            /// Dimensions of the overall plot
            /// </summary>
            public double? height, width;

            /// <summary>
            /// For pie/bar-charts: initially display labels, values or percent values
            /// </summary>
            public bool labels, values, percent;

            /// <summary>
            /// Assign a predefined palette or style
            /// Palette: Aurora, Category10, Category20, ColorblindFriendly, Dark, DarkPastel, Frost, Microcharts, 
            ///          Nord, OneHalf, OneHalfDark, PolarNight, Redness, SnowStorm, Tsitsulin 
            /// Style: Black, Blue1, Blue2, Blue3, Burgundy, Control, Default, Gray1, Gray2, Light1, Light2, 
            ///        Monospace, Pink, Seaborn
            /// </summary>
            public string palette, style;

            public enum Type { None, Bars, Pie }

            /// <summary>
            /// Make a plot to be a bar or pie chart.
            /// Can be associated to TimeSeries or TimeSeriesVariable/ DataPoint
            /// </summary>
            public Type type;

            public enum Source { Timer, Event }

            /// <summary>
            /// Specify source for value updates.
            /// </summary>
            public Source src;

            /// <summary>
            /// Specifies the timer interval in milli-seconds. Minimum value 100ms.
            /// Applicable on: Submodel
            /// </summary>
            public int timer;

            /// <summary>
            /// Instead of displaying a list of plot items, display a set of tiles.
            /// Rows and columns can be assigned to the individual tiles.
            /// Applicable on: Submodel
            /// </summary>
            public bool tiles;

            /// <summary>
            /// Defines the zero-based row- and column position for tile based display.
            /// The span-settings allow stretching over multiple (>1) tiles.
            /// Applicable on: Properties
            /// </summary>
            public int? row, col, rowspan, colspan;

            // ReSharper enable UnassignedField.Global

            //public static bool HasContent(this string str)
            public static bool HasContent(string str)
            {
                return str != null && str.Trim() != "";
            }

            public static PlotArguments Parse(string json)
            {
                if (!HasContent(json))
                    return null;

                try
                {
                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject<PlotArguments>(json);
                    return res;
                }
                catch (Exception ex)
                {
                    LogInternally.That.SilentlyIgnoredError(ex);
                }

                return null;
            }

            public ScottPlot.Drawing.Palette GetScottPalette()
            {
                if (HasContent(palette) == true)
                    foreach (var pl in ScottPlot.Palette.GetPalettes())
                        if (pl.Name.ToLower().Trim() == palette.ToLower().Trim())
                            return pl;
                return null;
            }

            public ScottPlot.Styles.IStyle GetScottStyle()
            {
                if (HasContent(style) == true)
                    foreach (var st in ScottPlot.Style.GetStyles())
                        if (st.GetType().Name.ToLower().Trim() == style.ToLower().Trim())
                            return st;
                return null;
            }
        }

        public static void TimeSeriesAddSegmentData(
        ZveiTimeSeriesDataV10 pcts,
        AdminShell.Key.MatchMode mm,
        TimeSeriesData tsd,
        AdminShell.SubmodelElementCollection smcseg)
        {
            // access
            if (pcts == null || smcseg == null)
                return;

            // challenge is to select SMes, which are NOT from a known semantic id!
            var tsvAllowed = new[]
            {
            pcts.CD_RecordId.GetSingleKey(),
            pcts.CD_UtcTime.GetSingleKey(),
            pcts.CD_TaiTime.GetSingleKey(),
            pcts.CD_Time.GetSingleKey(),
            pcts.CD_TimeDuration.GetSingleKey(),
            pcts.CD_ValueArray.GetSingleKey(),
            pcts.CD_ExternalDataFile.GetSingleKey()
        };

            var tsrAllowed = new[]
            {
            pcts.CD_RecordId.GetSingleKey(),
            pcts.CD_UtcTime.GetSingleKey(),
            pcts.CD_TaiTime.GetSingleKey(),
            pcts.CD_Time.GetSingleKey(),
            pcts.CD_TimeDuration.GetSingleKey(),
            pcts.CD_ValueArray.GetSingleKey()
        };

            // find variables?
            foreach (var smcvar in smcseg.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                pcts.CD_TimeSeriesVariable.GetReference(), mm))
            {
                // makes only sense with record id
                var recid = "" + smcvar.value.FindFirstSemanticIdAs<AdminShell.Property>(
                    pcts.CD_RecordId.GetReference(), mm)?.value?.Trim();
                if (recid.Length < 1)
                    continue;

                // add need a value array as well!
                var valarr = "" + smcvar.value.FindFirstSemanticIdAs<AdminShell.Blob>(
                    pcts.CD_ValueArray.GetReference(), mm)?.value?.Trim();
                if (valarr.Length < 1)
                    continue;

                // already have a dataset with that id .. or make new?
                var ds = tsd.FindDataSetById(recid);
                if (ds == null)
                {
                    // add
                    ds = new TimeSeriesDataSet() { DataSetId = recid };
                    tsd.DataSet.Add(ds);

                    // at this very moment, check if this is a time series
                    var timeSpec = DetectTimeSpecifier(pcts, mm, smcvar);
                    if (timeSpec != null)
                        ds.TimeAxis = timeSpec.Item1;

                    // find a DataPoint description?
                    var pdp = smcvar.value.FindFirstAnySemanticId<AdminShell.Property>(tsvAllowed, mm,
                        invertAllowed: true);
                    if (pdp != null && ds.DataPoint == null)
                    {
                        ds.DataPoint = pdp;
                        //----ds.DataPointCD = _package?.AasEnv?.FindConceptDescription(pdp.semanticId);
                    }

                    // plot arguments for record?
                    ds.Args = PlotArguments.Parse(smcvar.HasQualifierOfType("TimeSeries.Args")?.value);
                }

                // now try add the value array
                ds.DataAdd(valarr, fillTimeGaps: ds.TimeAxis != TimeSeriesTimeAxis.None);
            }

            // find records?
            foreach (var smcrec in smcseg.value.FindAllSemanticIdAs<AdminShell.SubmodelElementCollection>(
                pcts.CD_TimeSeriesRecord.GetReference(), mm))
            {
                // makes only sense with a numerical record id
                var recid = "" + smcrec.value.FindFirstSemanticIdAs<AdminShell.Property>(
                    pcts.CD_RecordId.GetReference(), mm)?.value?.Trim();
                if (recid.Length < 1)
                    continue;
                if (!int.TryParse(recid, out var dataIndex))
                    continue;

                // to prevent attacks, restrict index
                if (dataIndex < 0 || dataIndex > 16 * 1024 * 1024)
                    continue;

                // but, in this case, the dataset id's and data comes from individual
                // data points
                foreach (var pdp in smcrec.value.FindAllSemanticId<AdminShell.Property>(tsrAllowed, mm,
                        invertAllowed: true))
                {
                    // the dataset id is?
                    var dsid = "" + pdp.idShort;
                    if (!(dsid != null && dsid.Trim() != ""))
                        continue;

                    // query avilable information on the time
                    var timeSpec = DetectTimeSpecifier(pcts, mm, smcrec);
                    if (timeSpec == null)
                        continue;

                    // already have a dataset with that id .. or make new?
                    var ds = tsd.FindDataSetById(dsid);
                    if (ds == null)
                    {
                        // add
                        ds = new TimeSeriesDataSet() { DataSetId = dsid };
                        tsd.DataSet.Add(ds);

                        // find a DataPoint description? .. store it!
                        if (ds.DataPoint == null)
                        {
                            ds.DataPoint = pdp;
                            //----ds.DataPointCD = _package?.AasEnv?.FindConceptDescription(pdp.semanticId);
                        }

                        // now fix (one time!) the time data set for this data set
                        if (tsd.TimeDsLookup.ContainsKey(timeSpec.Item1))
                            ds.AssignedTimeDS = tsd.TimeDsLookup[timeSpec.Item1];
                        else
                        {
                            // create this
                            ds.AssignedTimeDS = new TimeSeriesDataSet()
                            {
                                DataSetId = "Time_" + timeSpec.Item1.ToString()
                            };
                            tsd.TimeDsLookup[timeSpec.Item1] = ds.AssignedTimeDS;
                        }

                        // plot arguments for datapoint?
                        ds.Args = PlotArguments.Parse(pdp.HasQualifierOfType("TimeSeries.Args")?.value);
                    }

                    // now access the value of the data point as float value
                    if (!double.TryParse(pdp.value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var dataValue))
                        continue;

                    // TimeDS and time is required
                    if (ds.AssignedTimeDS == null)
                        continue;

                    var tm = SpecifiedTimeToDouble(timeSpec.Item1, timeSpec.Item2.value);
                    if (!tm.HasValue)
                        continue;

                    // ok, push the data into the dataset
                    ds.AssignedTimeDS.DataAdd(dataIndex, tm.Value);
                    ds.DataAdd(dataIndex, dataValue);
                }
            }
        }

        protected static double? SpecifiedTimeToDouble(
            TimeSeriesTimeAxis timeAxis, string bufValue)
        {
            if (timeAxis == TimeSeriesTimeAxis.Utc || timeAxis == TimeSeriesTimeAxis.Tai)
            {
                // strict time string
                if (DateTime.TryParseExact(bufValue,
                    "yyyy-MM-dd'T'HH:mm:ssZ", CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal, out DateTime dt))
                {
                    return dt.ToOADate();
                }
            }

            // plain time or plain value
            if (double.TryParse(bufValue, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double fValue))
                return fValue;

            // no?
            return null;
        }

        public class ZveiTimeSeriesDataV10 : AasxDefinitionBase
        {
            public static ZveiTimeSeriesDataV10 Static = new ZveiTimeSeriesDataV10();

            public AdminShell.Submodel
                SM_TimeSeriesData;

            public AdminShell.ConceptDescription
                CD_TimeSeries,
                CD_Name,
                CD_Description,
                CD_TimeSeriesSegment,
                CD_RecordCount,
                CD_StartTime,
                CD_EndTime,
                CD_SamplingInterval,
                CD_SamplingRate,
                CD_TimeSeriesRecord,
                CD_RecordId,
                CD_UtcTime,
                CD_TaiTime,
                CD_Time,
                CD_TimeDuration,
                CD_TimeSeriesVariable,
                CD_ValueArray,
                CD_ExternalDataFile;

            public ZveiTimeSeriesDataV10()
            {
                // info
                this.DomainInfo = "Basic model for the modeling of time series data (ZVEI) v1.0";

                // Referable
                this.ReadLibrary(
                    Assembly.GetExecutingAssembly(), "Resources." + "ZveiTimeSeriesDataV10.json");
                this.RetrieveEntriesFromLibraryByReflection(typeof(ZveiTimeSeriesDataV10), useFieldNames: true);
            }
        }

        protected static Tuple<TimeSeriesTimeAxis, AdminShell.Property>
        DetectTimeSpecifier(
            ZveiTimeSeriesDataV10 pcts,
            AdminShell.Key.MatchMode mm,
            AdminShell.SubmodelElementCollection smc)
        {
            // access
            if (smc?.value == null || pcts == null)
                return null;

            // detect
            AdminShell.Property prop = null;
            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_UtcTime.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Utc, prop);

            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_TaiTime.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Tai, prop);

            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_Time.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Plain, prop);

            prop = smc.value.FindFirstSemanticIdAs<AdminShell.Property>(pcts.CD_TimeDuration.GetReference(), mm);
            if (prop != null)
                return new Tuple<TimeSeriesTimeAxis, AdminShell.Property>(TimeSeriesTimeAxis.Plain, prop);

            // no
            return null;
        }

        public class AasxDefinitionBase
        {
            //
            // Inner classes
            //

            public class LibraryEntry
            {
                public string name = "";
                public string contents = "";

                public LibraryEntry() { }
                public LibraryEntry(string name, string contents)
                {
                    this.name = name;
                    this.contents = contents;
                }
            }

            //
            // Fields
            //

            protected Dictionary<string, LibraryEntry> theLibrary = new Dictionary<string, LibraryEntry>();

            protected List<AdminShell.Referable> theReflectedReferables = new List<AdminShell.Referable>();

            public string DomainInfo = "";

            //
            // Constructors
            //

            public AasxDefinitionBase() { }

            public AasxDefinitionBase(Assembly assembly, string resourceName)
            {
                this.theLibrary = BuildLibrary(assembly, resourceName);
            }

            //
            // Rest
            //

            public void ReadLibrary(Assembly assembly, string resourceName)
            {
                this.theLibrary = BuildLibrary(assembly, resourceName);
            }

            protected Dictionary<string, LibraryEntry> BuildLibrary(Assembly assembly, string resourceName)
            {
                // empty result
                var res = new Dictionary<string, LibraryEntry>();

                // access resource
                //var stream = assembly.GetManifestResourceStream(resourceName);
                //if (stream == null)
                //    return res;

                // read text
                //TextReader tr = new StreamReader(stream);
                //var jsonStr = tr.ReadToEnd();
                var jsonStr = "{ \"SM_TimeSeriesData\": { \"semanticId\": { \"keys\": [ { \"type\": \"GlobalReference\", \"local\": false, \"value\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/1/0\", \"index\": 0, \"idType\": \"IRI\" } ] }, \"qualifiers\": [], \"hasDataSpecification\": [], \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TimeSeriesData\", \"category\": null, \"modelType\": { \"name\": \"Submodel\" }, \"kind\": \"Template\", \"descriptions\": [ { \"language\": \"de\", \"text\": \"Enthält Zeitreihendaten und Referenzen auf Zeitreihendaten, um diese entlang des Asset Lebenszyklus aufzufinden und semantisch zu beschreiben.\" }, { \"language\": \"en\", \"text\": \"Contains time series data and references to time series data to discover and semantically describe them along the asset lifecycle.\" } ] }, \"CD_TimeSeries\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeries/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TimeSeries\", \"category\": null, \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitreihe\" }, { \"language\": \"en\", \"text\": \"Time series\" } ], \"shortName\": [ { \"language\": \"de\", \"text\": \"Zeitreihe\" }, { \"language\": \"en\", \"text\": \"TimeSeries\" } ], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Abfolge von Datenpunkten in aufeinanderfolgender Reihenfolge über einen bestimmten Zeitraum.\" }, { \"language\": \"en\", \"text\": \"Sequence of data points in successive order over a specified period of time.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_Name\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/Name/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"Name\", \"category\": \"PARAMETER\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Name der Zeitreihe\" }, { \"language\": \"en\", \"text\": \"Name of the time series\" } ], \"shortName\": [ { \"language\": \"de\", \"text\": \"Name\" }, { \"language\": \"en\", \"text\": \"Name\" } ], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"STRING_TRANSLATABLE\", \"definition\": [ { \"language\": \"de\", \"text\": \"Aussagekräftiger Name zur Beschriftung.\" }, { \"language\": \"en\", \"text\": \"Meaningful name for labeling\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_Description\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/Description/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"Description\", \"category\": \"PARAMETER\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Beschreibung der Zeitreihe\" }, { \"language\": \"en\", \"text\": \"Description of the time series\" } ], \"shortName\": [ { \"language\": \"de\", \"text\": \"Beschreibung\" }, { \"language\": \"en\", \"text\": \"Description\" } ], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"STRING_TRANSLATABLE\", \"definition\": [ { \"language\": \"de\", \"text\": \"Kurze Beschreibung der Zeitreihendaten.\" }, { \"language\": \"en\", \"text\": \"Short description of the time series.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_TimeSeriesSegment\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesSegment/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TimeSeriesSegment\", \"category\": null, \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitreihensegment\" }, { \"language\": \"en\", \"text\": \"Time series segment\" } ], \"shortName\": [ { \"language\": \"de\", \"text\": \"Segment\" }, { \"language\": \"en\", \"text\": \"Segment\" } ], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Abfolge von Datenpunkten in aufeinanderfolgender Reihenfolge über einen bestimmten Zeitraum.\" }, { \"language\": \"en\", \"text\": \"Sequence of data points in successive order over a specified period of time.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_RecordCount\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/RecordCount/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"RecordCount\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Anzahl der Datensätze\" }, { \"language\": \"en\", \"text\": \"Record count\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"REAL_COUNT\", \"definition\": [ { \"language\": \"de\", \"text\": \"Gibt an, wie viele Datensätze in einem Segment vorhanden sind.\" }, { \"language\": \"en\", \"text\": \"Indicates how many records are present in a segment.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_StartTime\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/StartTime/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"StartTime\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Startzeit\" }, { \"language\": \"en\", \"text\": \"Start time\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Enthält den ersten aufgezeichneten Zeitstempel des Zeitreihensegments und stellt somit den Anfang einer Zeitreihe dar. Zeitformat und -skala entspricht dem der Zeitreihe.\" }, { \"language\": \"en\", \"text\": \"Contains the first recorded timestamp of the time series segment and thus represents the beginning of a time series. Time format and scale corresponds to that of the time series.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_EndTime\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/EndTime/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"EndTime\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Endzeit\" }, { \"language\": \"en\", \"text\": \"End time\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Enthält den letzten aufgezeichneten Zeitstempel des Zeitreihensegments und stellt somit das Ende einer Zeitreihe dar. Zeitformat und -skala entspricht dem der Zeitreihe.\" }, { \"language\": \"en\", \"text\": \"Contains the last recorded timestamp of the time series segment and thus represents the end of a time series. Time format and scale corresponds to that of the time series.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_SamplingInterval\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/SamplingInterval/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"SamplingInterval\", \"category\": \"PARAMETER\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Abtastintervall\" }, { \"language\": \"en\", \"text\": \"Sampling interval\" } ], \"shortName\": [], \"unit\": \"s\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": \"t\", \"dataType\": \"REAL_MEASURE\", \"definition\": [ { \"language\": \"de\", \"text\": \"Der zeitliche Abstand zwischen zwei Datenpunkten (Länge eines Zyklus).\" }, { \"language\": \"en\", \"text\": \"The time period between two time series records (Length of cycle).\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_SamplingRate\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/SamplingRate/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"SamplingRate\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Abtastrate\" }, { \"language\": \"en\", \"text\": \"Sampling rate\" } ], \"shortName\": [], \"unit\": \"Hz\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"REAL_MEASURE\", \"definition\": [ { \"language\": \"de\", \"text\": \"Definiert die Anzahl der Abtastungen pro Sekunde für eine regelmäßige Zeitreihe in Hz.\" }, { \"language\": \"en\", \"text\": \"Defines the number of samples per second for a regular time series in Hz.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_TimeSeriesRecord\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesRecord/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TimeSeriesRecord\", \"category\": null, \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitreihen-Datensatz\" }, { \"language\": \"en\", \"text\": \"Time series record\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Ein Zeitreihen-Datensatz ist durch seine ID innerhalb der Zeitreihe eindeutig und beinhaltet die auf die ID referenzierten Zeitstempel und Variablenwerte. Vergleichbar mit einer Zeile in einer Tabelle.\" }, { \"language\": \"en\", \"text\": \"A time series record is unique by its ID within the time series and contains the timestamps and variable values referenced to the ID. Similar to a row in a table.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_RecordId\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/RecordId/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"RecordId\", \"category\": \"PARAMETER\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"ID\" }, { \"language\": \"en\", \"text\": \"ID\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"STRING\", \"definition\": [ { \"language\": \"en\", \"text\": \"Labels the record within a time series with a unique ID.\" }, { \"language\": \"de\", \"text\": \"Kennzeichnet den Datensatz innerhalb einer Zeitreihe mit einer eindeutigen ID. \" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_UtcTime\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/UtcTime/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"UtcTime\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitstempel UTC\" }, { \"language\": \"en\", \"text\": \"timestamp UTC\" } ], \"shortName\": [ { \"language\": \"de\", \"text\": \"Zeitstempel UTC\" }, { \"language\": \"en\", \"text\": \"timestamp UTC\" } ], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"TIMESTAMP\", \"definition\": [ { \"language\": \"de\", \"text\": \"Zeitstempel nach ISO 8601 auf der Zeitskala der koordinierten Weltzeit (UTC).\" }, { \"language\": \"en\", \"text\": \"Timestamp according to ISO 8601 on the timescale ccordinated universal time (UTC).\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_TaiTime\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/TaiTime/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TaiTime\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitstempel TAI\" }, { \"language\": \"en\", \"text\": \"timestamp TAI\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"TIMESTAMP\", \"definition\": [ { \"language\": \"de\", \"text\": \"Zeitstempel nach ISO 8601 auf der Zeitskala internationale Atomzeit (TAI).\" }, { \"language\": \"en\", \"text\": \"Timestamp according to ISO 8601 on the timescale international atomic time (TAI).\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_Time\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/Time/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"Time\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitstempel\" }, { \"language\": \"en\", \"text\": \"Timestamp\" } ], \"shortName\": [], \"unit\": \"s\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": \"t\", \"dataType\": \"REAL_MEASURE\", \"definition\": [ { \"language\": \"de\", \"text\": \"Zeitpunktangabe in Sekunden. Zeitpunkte referenzieren auf die Startzeit des Zeitreihensegments.\" }, { \"language\": \"en\", \"text\": \"Point of Time in seconds. Time points refer to the start time of the time series segment.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_TimeDuration\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeDuration/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TimeDuration\", \"category\": \"VARIABLE\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitdauer\" }, { \"language\": \"en\", \"text\": \"Timeduration\" } ], \"shortName\": [], \"unit\": \"s\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": \"\", \"dataType\": \"REAL_MEASURE\", \"definition\": [ { \"language\": \"de\", \"text\": \"Angabe der zeitlichen Dauer in Sekunden (Anzahl der Sekunden). Zeitdauern referenzieren auf den jeweils vorangegangenen Eintrag im Zeitreihensegment.\" }, { \"language\": \"en\", \"text\": \"Time duration specification in seconds. (number of seconds). Time durations refer to the previous entry in the time series segment.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_TimeSeriesVariable\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesVariable/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"TimeSeriesVariable\", \"category\": null, \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Zeitreihenvariable\" }, { \"language\": \"en\", \"text\": \"Time series variable\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Eine Zeitreihenvariable bildet eine Wertereihe, bestehend aus RecordID und einer weiteren Dimension der Zeitreihe, als Array ab. Vergleichbar einer Spalte in einer Tabelle. Variablen können Zeitstempel oder Datenpunkte sein.\" }, { \"language\": \"en\", \"text\": \"A time series variable contains the sequence of values, consisting of RecordID and another dimension of the time series, as an array. Similar to a column in a table. Variables can be timestamps or data points.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_ValueArray\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/ValueArray/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"ValueArray\", \"category\": \"PARAMETER\", \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Wertereihe\" }, { \"language\": \"en\", \"text\": \"Value Array\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Wertereihe einer einer Zeitreihe. Die Reihenfolge der Dimensionen und deren semantische Beueutung ergibt sich aus den Definitionen innerhalb der Zeitreihenvariable.  \" }, { \"language\": \"en\", \"text\": \"Values of a time series. The order of the dimensions and their semantic meaning results from the definitions within the time series variable.  \" } ] } } ], \"isCaseOf\": [], \"descriptions\": null }, \"CD_ExternalDataFile\": { \"identification\": { \"idType\": \"IRI\", \"id\": \"https://admin-shell.io/sandbox/zvei/TimeSeriesData/ExternalDataFile/1/0\" }, \"administration\": { \"version\": \"1\", \"revision\": \"0\" }, \"idShort\": \"ExternalDataFile\", \"category\": null, \"modelType\": { \"name\": \"ConceptDescription\" }, \"embeddedDataSpecifications\": [ { \"dataSpecification\": { \"keys\": [] }, \"dataSpecificationContent\": { \"preferredName\": [ { \"language\": \"de\", \"text\": \"Externe Datendatei\" }, { \"language\": \"en\", \"text\": \"External data file\" } ], \"shortName\": [], \"unit\": \"\", \"unitId\": null, \"valueFormat\": null, \"sourceOfDefinition\": null, \"symbol\": null, \"dataType\": \"\", \"definition\": [ { \"language\": \"de\", \"text\": \"Externe Datendatei, welche Zeitreihendaten in einem beliebigen Format beinhaltet.\" }, { \"language\": \"en\", \"text\": \"External data file containing time series data in any format.\" } ] } } ], \"isCaseOf\": [], \"descriptions\": null } }";
                //stream.Close();

                // Parse into root
                var root = JObject.Parse(jsonStr);

                // decompose
                foreach (var child in root.Children())
                {
                    // just look for 1. level properties
                    var prop = child as JProperty;
                    if (prop == null)
                        continue;

                    // ok
                    var name = prop.Name;
                    var contents = prop.Value.ToString();

                    // populate
                    res.Add(name, new LibraryEntry(name, contents));
                }

                return res;
            }

            public LibraryEntry RetrieveEntry(string name)
            {
                // simple access
                if (theLibrary == null || name == null || !theLibrary.ContainsKey(name))
                    return null;

                // return
                return theLibrary[name];
            }

            public T RetrieveReferable<T>(string name) where T : AdminShell.Referable
            {
                // entry
                var entry = this.RetrieveEntry(name);
                if (entry == null || entry.contents == null)
                    return null;

                // try de-serialize
                try
                {
                    var r = JsonConvert.DeserializeObject<T>(entry.contents);
                    return r;
                }
                catch (Exception ex)
                {
                    AdminShellNS.LogInternally.That.SilentlyIgnoredError(ex);
                    return null;
                }
            }

            public static AdminShell.ConceptDescription CreateSparseConceptDescription(
                string lang,
                string idType,
                string idShort,
                string id,
                string definitionHereString,
                AdminShell.Reference isCaseOf = null)
            {
                // access
                if (idShort == null || idType == null || id == null)
                    return null;

                // create CD
                var cd = AdminShell.ConceptDescription.CreateNew(idShort, idType, id);
                var dsiec = cd.CreateDataSpecWithContentIec61360();
                dsiec.preferredName = new AdminShellV20.LangStringSetIEC61360(lang, "" + idShort);
                dsiec.definition = new AdminShellV20.LangStringSetIEC61360(lang,
                    "" + AdminShellUtil.CleanHereStringWithNewlines(nl: " ", here: definitionHereString));

                // options
                if (isCaseOf != null)
                    cd.IsCaseOf = new List<AdminShell.Reference>(new[] { isCaseOf });

                // ok
                return cd;
            }

            /// <summary>
            /// This attribute indicates, that the attributed member shall be looked up by its name
            /// in the library.
            /// </summary>
            [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true)]
            public class RetrieveReferableForField : System.Attribute
            {
            }

            public virtual AdminShell.Referable[] GetAllReferables()
            {
                return this.theReflectedReferables?.ToArray();
            }

            public void RetrieveEntriesFromLibraryByReflection(Type typeToReflect = null,
                bool useAttributes = false, bool useFieldNames = false)
            {
                // access
                if (this.theLibrary == null || typeToReflect == null)
                    return;

                // remember found Referables
                this.theReflectedReferables = new List<AdminShell.Referable>();

                // reflection
                foreach (var fi in typeToReflect.GetFields())
                {
                    // libName
                    var libName = "" + fi.Name;

                    // test
                    var ok = false;
                    var isSM = fi.FieldType == typeof(AdminShell.Submodel);
                    var isCD = fi.FieldType == typeof(AdminShell.ConceptDescription);

                    if (useAttributes && fi.GetCustomAttribute(typeof(RetrieveReferableForField)) != null)
                        ok = true;

                    if (useFieldNames && isSM && libName.StartsWith("SM_"))
                        ok = true;

                    if (useFieldNames && isCD && libName.StartsWith("CD_"))
                        ok = true;

                    if (!ok)
                        continue;

                    // access library
                    if (isSM)
                    {
                        var sm = this.RetrieveReferable<AdminShell.Submodel>(libName);
                        fi.SetValue(this, sm);
                        this.theReflectedReferables.Add(sm);
                    }
                    if (isCD)
                    {
                        var cd = this.RetrieveReferable<AdminShell.ConceptDescription>(libName);
                        fi.SetValue(this, cd);
                        this.theReflectedReferables.Add(cd);
                    }
                }
            }

            public void AddEntriesByReflection(Type typeToReflect = null,
                bool useAttributes = false, bool useFieldNames = false)
            {
                // access
                if (typeToReflect == null)
                    return;

                // reflection
                foreach (var fi in typeToReflect.GetFields())
                {
                    // libName
                    var fiName = "" + fi.Name;

                    // test
                    var ok = false;
                    var isSM = fi.FieldType == typeof(AdminShell.Submodel);
                    var isCD = fi.FieldType == typeof(AdminShell.ConceptDescription);

                    if (useAttributes && fi.GetCustomAttribute(typeof(RetrieveReferableForField)) != null)
                        ok = true;

                    if (useFieldNames && isSM && fiName.StartsWith("SM_"))
                        ok = true;

                    if (useFieldNames && isCD && fiName.StartsWith("CD_"))
                        ok = true;

                    if (!ok)
                        continue;

                    // add
                    var rf = fi.GetValue(this) as AdminShell.Referable;
                    if (rf != null)
                        this.theReflectedReferables.Add(rf);
                }
            }
        }

        public interface IWpfPlotViewControl
        {
            //ScottPlot.WpfPlot WpfPlot { get; }
            //ContentControl ContentControl { get; }

            string Text { get; set; }

            bool AutoScaleX { get; set; }
            bool AutoScaleY { get; set; }
        }

        public static class PlotHelpers
        {
            //public static Brush BrushFrom(System.Drawing.Color col)
            //{
            //    return new SolidColorBrush(Color.FromArgb(col.A, col.R, col.G, col.B));
            //}

            public static void SetOverallPlotProperties(
                IWpfPlotViewControl pvc,
                ScottPlot.Plot wpfPlot,
                PlotArguments args,
                double defPlotHeight)
            {
                if (wpfPlot != null)
                {
                    var pal = args?.GetScottPalette();
                    if (pal != null)
                        wpfPlot.Palette = pal;
                    var stl = args?.GetScottStyle();
                    if (stl != null)
                        wpfPlot.Style(stl);

                    var legend = wpfPlot.Legend(location: Alignment.UpperRight);
                    legend.FontSize = 9.0f;
                }

                //var cc = pvc?.ContentControl;
                //if (cc != null)
                //{
                //    var height = defPlotHeight;
                //    if (true == args?.height.HasValue)
                //        height = args.height.Value;
                //    cc.MinHeight = height;
                //    cc.MaxHeight = height;
                //}
            }

            public static void SetPlottableProperties(
                ScottPlot.Plottable.BarPlot bars,
                PlotArguments args)
            {
            }

            public static void SetPlottableProperties(
                ScottPlot.Plottable.ScatterPlot scatter,
                PlotArguments args)
            {
                // access
                if (scatter == null)
                    return;

                // set
                if (true == args?.linewidth.HasValue)
                    scatter.LineWidth = args.linewidth.Value;

                if (true == args?.markersize.HasValue)
                    scatter.MarkerSize = (float)args.markersize.Value;
            }

            public static void SetPlottableProperties(
                ScottPlot.Plottable.SignalPlot signal,
                PlotArguments args)
            {
                // access
                if (signal == null)
                    return;

                // set
                if (true == args?.linewidth.HasValue)
                    signal.LineWidth = args.linewidth.Value;

                if (true == args?.markersize.HasValue)
                    signal.MarkerSize = (float)args.markersize.Value;
            }

            public static string EvalDisplayText(
                    string minmalText, AdminShell.SubmodelElement sme,
                    AdminShell.ConceptDescription cd = null,
                    bool addMinimalTxt = false,
                    string defaultLang = null,
                    bool useIdShort = true)
            {
                var res = "" + minmalText;
                if (sme != null)
                {
                    // best option: description of the SME itself
                    string better = sme.description?.GetDefaultStr(defaultLang);

                    // if still none, simply use idShort
                    // SME specific non-multi-lang found better than CD multi-lang?!
                    if (!HasContent(better) && useIdShort)
                        better = sme.idShort;

                    // no? then look for CD information
                    if (cd != null)
                    {
                        if (!HasContent(better))
                            better = cd.GetDefaultPreferredName(defaultLang);
                        if (!HasContent(better))
                            better = cd.idShort;
                        if (HasContent(better) && true == HasContent(cd.IEC61360Content?.unit))
                            better += $" [{cd.IEC61360Content?.unit}]";
                    }

                    if (HasContent(better))
                    {
                        res = better;
                        if (addMinimalTxt)
                            res += $" ({minmalText})";
                    }
                }
                return res;
            }

            public static bool HasContent(string str)
            {
                return str != null && str.Trim() != "";
            }
        }

        public class CumulativeDataItems
        {
            public List<string> Label = new List<string>();
            public List<double> Position = new List<double>();
            public List<double> Value = new List<double>();
        }

        public static ScottPlot.Plottable.IPlottable GenerateCumulativePlottable(
        ScottPlot.Plot wpfPlot,
        CumulativeDataItems cumdi,
        PlotArguments args)
        {
            // access
            if (wpfPlot == null || cumdi == null || args == null)
                return null;

            if (args.type == PlotArguments.Type.Pie)
            {
                var pie = wpfPlot.AddPie(cumdi.Value.ToArray());
                pie.SliceLabels = cumdi.Label.ToArray();
                pie.ShowLabels = args.labels;
                pie.ShowValues = args.values;
                pie.ShowPercentages = args.percent;
                pie.SliceFont.Size = 9.0f;
                return pie;
            }

            if (args.type == PlotArguments.Type.Bars)
            {
                var bar = wpfPlot.AddBar(cumdi.Value.ToArray());
                wpfPlot.XTicks(cumdi.Position.ToArray(), cumdi.Label.ToArray());
                bar.ShowValuesAboveBars = args.values;
                return bar;
            }

            return null;
        }
    }
}
