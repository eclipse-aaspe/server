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

/// <summary>
/// One distinct submodel template of the repository: a (IdShort, SemanticId) group over
/// SMSets with its occurrence count. Deterministic inventory of which submodel types
/// exist, independent of any sampling.
/// </summary>
public class DbSubmodelTemplateRow
{
    public string? IdShort { get; set; }

    public string? SemanticId { get; set; }

    /// <summary>Number of submodels with this exact IdShort+SemanticId combination.</summary>
    public int Count { get; set; }
}
