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

using IO.Swagger.Models;
using IO.Swagger.Registry.Lib.V3.Models;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Models;

public class AasDescriptorPagedResult
{
    public List<AssetAdministrationShellDescriptor>? result          { get; set; }
    public PagedResultPagingMetadata?                paging_metadata { get; set; }
}

public class SubmodelDescriptorPagedResult
{
    public List<SubmodelDescriptor>? result { get; set; }
    public PagedResultPagingMetadata? paging_metadata { get; set; }
}
