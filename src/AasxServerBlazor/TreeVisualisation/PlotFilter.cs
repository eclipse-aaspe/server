using System;
using AasxServerStandardBib;

namespace AasxServerBlazor.TreeVisualisation;

public class PlotFilter
{
    public DateTime FromDate { get; set; }
    public DateTime FromTime { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime ToTime { get; set; }
    public DateTime CombinedFromDate { get; private set; }
    public DateTime CombinedToDate { get; private set; }

    public PlotFilter()
    {
        SetInitialFilterState();
    }

    public void SetInitialFilterState()
    {
        ResetTimeOfDates();
        var initialFromDate = DateTime.Now.AddYears(-3);
        var initialToDate = DateTime.Now.AddYears(3);
        FromDate = initialFromDate;
        ToDate = initialToDate;
        CombinedFromDate = initialFromDate;
        CombinedToDate = initialToDate;
    }

    public void SetFilterStateDay(int offset)
    {
        ResetTimeOfDates();
        var day = DateTime.Today.AddDays(offset);
        var midnight = new TimeSpan(0, 23, 59, 59);
        var endOfDay = day.Add(midnight);

        FromDate = day;
        FromTime = day;
        ToDate = endOfDay;
        ToTime = endOfDay;
        CombinedFromDate = day;
        CombinedToDate = endOfDay;
    }

    private void ResetTimeOfDates()
    {
        // Reset to 0 AM
        FromDate = new DateTime(FromDate.Year, FromDate.Month, FromDate.Day, 0, 0, 0);
        ToDate = new DateTime(ToDate.Year, ToDate.Month, ToDate.Day, 0, 0, 0);
    }

    public void UpdateFilter(TimeSeriesPlotting.ListOfTimeSeriesData timeSeriesData, int sessionNumber)
    {
        ResetTimeOfDates();
        var fromTimeSpan = FromTime.TimeOfDay;
        var toTimeSpan = ToTime.TimeOfDay;
        CombinedFromDate = FromDate.Add(fromTimeSpan);
        CombinedToDate = ToDate.Add(toTimeSpan);
        timeSeriesData?.RenderTimeSeries(defPlotHeight: 200, "en", sessionNumber, CombinedFromDate, CombinedToDate);
    }
}