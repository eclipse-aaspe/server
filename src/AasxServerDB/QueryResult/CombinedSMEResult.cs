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

namespace AasxServerDB
{
    public partial class Query
    {
        public class CombinedSMEResult
        {
            public string? SM_Identifier { get; set; }
            public string? IdShortPath { get; set; }
            public string? SME_TimeStamp { get; set; }
            public string? V_Value { get; set; }
        }
    }
}