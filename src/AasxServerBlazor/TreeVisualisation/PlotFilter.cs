using System;
using AasxServerStandardBib;

namespace AasxServerBlazor.TreeVisualisation;

public class PlotFilter
{
    public DateTime fromDate { get; set; }
    public DateTime fromTime { get; set; }
    public DateTime toDate { get; set; }
    public DateTime toTime { get; set; }
    public DateTime combinedFromDate { get; set; }
    public DateTime combinedToDate { get; set; }

    public PlotFilter()
    {
        SetInitialFilterState();
    }

    public void SetInitialFilterState()
    {
        ResetTimeOfDates();
        var initialFromDate = DateTime.Now.AddYears(-3);
        var initialToDate = DateTime.Now.AddYears(3);
        fromDate = initialFromDate;
        toDate = initialToDate;
        combinedFromDate = initialFromDate;
        combinedToDate = initialToDate;
    }

    public void SetFilterStateDay(int offset)
    {
        ResetTimeOfDates();
        var day = DateTime.Today.AddDays(offset);
        var midnight = new System.TimeSpan(0, 23, 59, 59);
        var endOfDay = day.Add(midnight);

        fromDate = day;
        fromTime = day;
        toDate = endOfDay;
        toTime = endOfDay;
        combinedFromDate = day;
        combinedToDate = endOfDay;
    }

    public void ResetTimeOfDates()
    {
        // Reset to 0 AM
        fromDate = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0);
        toDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, 0, 0, 0);
    }

    public void UpdateFilter(TimeSeriesPlotting.ListOfTimeSeriesData tsd, int sessionNumber)
    {
        ResetTimeOfDates();
        var fromTimeSpan = fromTime.TimeOfDay;
        var toTimeSpan = toTime.TimeOfDay;
        combinedFromDate = fromDate.Add(fromTimeSpan);
        combinedToDate = toDate.Add(toTimeSpan);
        tsd?.RenderTimeSeries(defPlotHeight: 200, "en", sessionNumber, combinedFromDate, combinedToDate);
    }
}