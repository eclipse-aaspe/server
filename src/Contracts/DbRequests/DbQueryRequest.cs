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

namespace Contracts.DbRequests;
public class DbQueryRequest
{
    public bool WithTotalCount { get; set; }

    public bool WithLastId { get; set; }

    public string SemanticId { get; set; }

    public string Identifier { get; set; }

    public string Diff { get; set; }

    public int PageFrom { get; set; }
    public int PageSize { get; set; }

    public string Expression { get; set; }

    //Submodel Element Request
    public string Requested { get; set; }
    public string SmSemanticId { get; set; }
    public string Contains { get; set; }
    public string Equal { get; set; }
    public string Lower { get; set; }
    public string Upper { get; set; }
}

