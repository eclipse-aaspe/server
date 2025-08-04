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

namespace Contracts.QueryResult
{
    public class QResult
    {
        public static int DefaultPageSize = 1000;
        public int Count { get; set; }
        public int TotalCount { get; set; }
        public int PageFrom { get; set; }
        public int PageSize { get; set; }
        public int? LastID { get; set; }
        public List<string> Messages { get; set; }
        public List<SMResult> SMResults { get; set; }
        public List<SMEResult> SMEResults { get; set; }
        public List<string> SQL { get; set; }
        public bool WithSelect;
    }
}

