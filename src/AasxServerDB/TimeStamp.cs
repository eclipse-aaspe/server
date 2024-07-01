/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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
    static public class TimeStamp
    {
        static public DateTime StringToDateTime(string stringDateTime)
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

        static public string DateTimeToString(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }
}
