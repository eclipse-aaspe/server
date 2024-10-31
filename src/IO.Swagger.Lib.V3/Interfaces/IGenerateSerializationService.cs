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

using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces;

/// <summary>
/// Service responsible for generating serialized representations of Asset Administration Shells (AAS) and Submodels.
/// </summary>
public interface IGenerateSerializationService
{
    /// <summary>
    /// Generates a serialized environment containing the requested Asset Administration Shells (AAS) and Submodels.
    /// </summary>
    /// <param name="aasIds">Optional list of AAS IDs to include in the serialized output.</param>
    /// <param name="submodelIds">Optional list of Submodel IDs to include in the serialized output.</param>
    /// <returns>An <see cref="Environment"/> object containing the specified AAS and Submodels.</returns>
    Environment GenerateSerializationByIds(List<string?>? aasIds = null, List<string?>? submodelIds = null, bool? includeCD = false);
}