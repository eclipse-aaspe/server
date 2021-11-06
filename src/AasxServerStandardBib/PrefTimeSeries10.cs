using System;
using System.Collections.Generic;
using System.Text;
using AdminShellNS;

namespace AasxTimeSeries
{
    public static class PrefTimeSeries10
    {
        public static AdminShell.Key CD_TimeSeries =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeries/1/0");

        public static AdminShell.Key CD_TimeSeriesSegment =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesSegment/1/0");

        public static AdminShell.Key CD_TimeSeriesVariable =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesVariable/1/0");

        public static AdminShell.Key CD_ValueArray =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/ValueArray/1/0");

        public static AdminShell.Key CD_RecordId =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/RecordId/1/0");

        public static AdminShell.Key CD_UtcTime =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/UtcTime/1/0");

        public static AdminShell.Key CD_GeneratedInteger =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/Sample/GeneratedInteger/1/0");

        public static AdminShell.Key CD_GeneratedFloat =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/Sample/GeneratedFloat/1/0");

        public static AdminShell.Key CD_RecordCount =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/RecordCount/1/0");

        public static AdminShell.Key CD_StartTime =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/StartTime/1/0");

        public static AdminShell.Key CD_EndTime =
            new AdminShell.Key(AdminShell.Key.ConceptDescription, false, AdminShell.Identification.IRI,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/EndTime/1/0");

    }
}
