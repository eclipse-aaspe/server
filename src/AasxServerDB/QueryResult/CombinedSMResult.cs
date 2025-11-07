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

namespace AasxServerDB
{
    using System;
    using System.Collections.Generic;
    using AasCore.Aas3_0;

    public partial class Query
    {
        private class CombinedSMResult
        {
            public int? SM_Id { get; set; }
            public string? Identifier { get; set; }
            public string? TimeStampTree { get; set; }
            public List<string>? MatchPathList { get; set; }
        }
        private class CombinedSMResultWithAas
        {
            public int? AAS_Id { get; set; }
            public int? SM_Id { get; set; }
            public string? Identifier { get; set; }
            public string? TimeStampTree { get; set; }
            public List<string>? MatchPathList { get; set; }
        }
    }
}
