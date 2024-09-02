/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace AasxTimeSeries
{
    public static class PrefTimeSeries10
    {
        public static Key CD_TimeSeries =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeries/1/0");

        public static Key CD_TimeSeriesSegment =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesSegment/1/0");

        public static Key CD_TimeSeriesVariable =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/TimeSeriesVariable/1/0");

        public static Key CD_ValueArray =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/ValueArray/1/0");

        public static Key CD_RecordId =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/RecordId/1/0");

        public static Key CD_UtcTime =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/UtcTime/1/0");

        public static Key CD_GeneratedInteger =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/Sample/GeneratedInteger/1/0");

        public static Key CD_GeneratedFloat =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/Sample/GeneratedFloat/1/0");

        public static Key CD_RecordCount =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/RecordCount/1/0");

        public static Key CD_StartTime =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/StartTime/1/0");

        public static Key CD_EndTime =
            new Key(KeyTypes.ConceptDescription,
            "https://admin-shell.io/sandbox/zvei/TimeSeriesData/EndTime/1/0");

    }
}
