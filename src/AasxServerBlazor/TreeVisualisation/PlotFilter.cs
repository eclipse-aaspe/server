using System;
using AasxServerStandardBib;

namespace AasxServerBlazor.TreeVisualisation
{
    /// <summary>
    /// Represents a filter used for plotting time series data.
    /// </summary>
    public class PlotFilter
    {
        public DateTime StartDate { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime CombinedStartDateTime { get; private set; }

        public DateTime CombinedEndDateTime { get; private set; }

        public PlotFilter()
        {
            SetInitialFilterState();
        }

        /// <summary>
        /// Sets the filter state to its initial values, spanning a range of three years from the current date.
        /// </summary>
        public void SetInitialFilterState()
        {
            var initialFromDate = DateTime.Now.AddYears(-3);
            var initialToDate = DateTime.Now.AddYears(3);
            SetFilterDates(initialFromDate, initialFromDate, initialToDate, initialToDate);
        }

        /// <summary>
        /// Sets the filter state for a specific day based on the given offset.
        /// </summary>
        /// <param name="offset">The offset from the current day.</param>
        public void SetFilterStateForDay(int offset)
        {
            var day = DateTime.Today.AddDays(offset);
            var endOfDay = day.Date.AddDays(1).AddTicks(-1);
            SetFilterDates(day, day, endOfDay, endOfDay);
        }

        private void SetFilterDates(DateTime fromDate, DateTime fromTime, DateTime toDate, DateTime toTime)
        {
            StartDate = fromDate;
            StartTime = fromTime;
            EndDate = toDate;
            EndTime = toTime;
            ResetTimeOfDates();
        }

        /// <summary>
        /// Resets the time part of start and end dates to midnight.
        /// </summary>
        private void ResetTimeOfDates()
        {
            StartDate = StartDate.Date;
            EndDate = EndDate.Date.AddDays(1).AddTicks(-1);
            CombinedStartDateTime = StartDate.Add(StartTime.TimeOfDay);
            CombinedEndDateTime = EndDate.Add(EndTime.TimeOfDay);
        }

        /// <summary>
        /// Applies the filter to the provided time series data.
        /// </summary>
        /// <param name="timeSeriesData">The time series data to be filtered.</param>
        /// <param name="sessionNumber">The session number for the filtered data.</param>
        public void ApplyFilterToTimeSeriesData(TimeSeriesPlotting.ListOfTimeSeriesData timeSeriesData, int sessionNumber)
        {
            ResetTimeOfDates();
            timeSeriesData?.RenderTimeSeries(defPlotHeight: 200, "en", sessionNumber, CombinedStartDateTime, CombinedEndDateTime);
        }
    }
}
