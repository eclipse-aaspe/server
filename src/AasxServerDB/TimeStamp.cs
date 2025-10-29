/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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

namespace TimeStamp
{
    public static class TimeStamp
    {
        private const string FormatString = "yyyy-MM-dd HH:mm:ss.fff";
        private const string FormatStringSQL = "%Y-%m-%d %H:%M:%f";

        public static DateTime StringToDateTime(string stringDateTime)
        {
            try
            {
                return DateTime.Parse(stringDateTime).ToUniversalTime();
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public static string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString(FormatString);
        }

        public static string GetFormatStringSQL()
        {
            return FormatStringSQL;
        }
    }
}

