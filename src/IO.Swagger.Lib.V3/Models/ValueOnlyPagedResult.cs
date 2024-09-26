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

using DataTransferObjects.ValueDTOs;
using IO.Swagger.Models;
using System.Collections.Generic;

namespace AdminShellNS.Lib.V3.Models
{
    public class ValueOnlyPagedResult
    {
        public List<IValueDTO>? result { get; set; }

        public PagedResultPagingMetadata? paging_metadata { get; set; }
    }
}
