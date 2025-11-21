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
    public partial class Query
    {
        private class CombinedSMSMEV
        {
            public int? SM_Id { get; set; }
            public string? SM_SemanticId { get; set; }
            public string? SM_IdShort { get; set; }
            public string? SM_DisplayName { get; set; }
            public string? SM_Description { get; set; }
            public string? SM_Identifier { get; set; }
            public DateTime SM_TimeStampTree { get; set; }

            public string? SME_SemanticId { get; set; }
            public string? SME_IdShort { get; set; }
            public string? SME_IdShortPath { get; set; }
            public string? SME_DisplayName { get; set; }
            public string? SME_Description { get; set; }
            public int? SME_Id { get; set; }
            public DateTime SME_TimeStamp { get; set; }

            public string? V_Value { get; set; }
            public double? V_D_Value { get; set; } // needed for the WHERE at the ending 
        }
    }
}
