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

using System;

namespace AasxServerBlazor.Data
{
    public class BlazorSessionService : IDisposable
    {
        public static int sessionCounter;
        public int sessionNumber;

        public BlazorSessionService()
        {
            sessionNumber = ++sessionCounter;
        }

        public void Dispose()
        {
            System.IO.File.Delete($@"wwwroot/images/scottplot/smc_timeseries_clientid{sessionNumber}.png");
        }
    }
}
