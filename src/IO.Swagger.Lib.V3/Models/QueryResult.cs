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

namespace IO.Swagger.Lib.V3.Models;

using Contracts.Pagination;
using IO.Swagger.Models;
using System.Collections.Generic;
using System;

public class QueryResult
{
    /// <summary>
     /// Gets or sets the list of items in the paged result.
     /// </summary>
    public List<object?> result { get; set; }

    /// <summary>
    /// Gets or sets the paging metadata for the paged result.
    /// </summary>
    public QueryResultPagingMetadata? paging_metadata { get; set; }
}
