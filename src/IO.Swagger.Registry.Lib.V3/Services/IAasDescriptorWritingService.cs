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

namespace IO.Swagger.Registry.Lib.V3.Services;

using System;
using System.Collections.Generic;
using Models;

/// <summary>
/// Provides services to manage Asset Administration Shell (AAS) descriptors and their properties within submodel registries.
/// </summary>
public interface IAasDescriptorWritingService
{
    /// <summary>
    /// Adds a new entry to the AAS registry.
    /// </summary>
    /// <param name="ad">The AAS descriptor to add.</param>
    /// <param name="aasRegistry">The AAS registry submodel.</param>
    /// <param name="submodelRegistry">The submodel registry.</param>
    /// <param name="timestamp">The timestamp for when the entry is added.</param>
    /// <param name="aasID">The ID of the AAS.</param>
    /// <param name="assetID">The ID of the asset.</param>
    /// <param name="endpoint">The endpoint for the AAS.</param>
    void AddNewEntry(AssetAdministrationShellDescriptor ad, ISubmodel aasRegistry, ISubmodel submodelRegistry, DateTime timestamp, string? aasID, string? assetID,
                     string? endpoint);

    /// <summary>
    /// Overwrites an existing entry in the AAS registry if the IDs match.
    /// </summary>
    /// <param name="ad">The AAS descriptor to overwrite.</param>
    /// <param name="aasRegistry">The AAS registry submodel.</param>
    /// <param name="timestamp">The timestamp for when the entry is overwritten.</param>
    /// <param name="initial">Indicates if this is the initial entry.</param>
    /// <param name="aasID">The ID of the AAS.</param>
    /// <param name="assetID">The ID of the asset.</param>
    /// <param name="endpoint">The endpoint for the AAS.</param>
    /// <returns>True if the entry was overwritten, otherwise false.</returns>
    bool OverwriteExistingEntryForEdenticalIds(AssetAdministrationShellDescriptor ad, ISubmodel aasRegistry, DateTime timestamp, bool initial, string? aasID,
                                               string? assetID,
                                               string? endpoint);

    /// <summary>
    /// Processes a submodel descriptor and adds it to the submodel registry.
    /// </summary>
    /// <param name="sd">The submodel descriptor to process.</param>
    /// <param name="timestamp">The timestamp for the processing.</param>
    /// <param name="parentCollection">The parent collection to which the descriptor will be added.</param>
    /// <param name="submodelRegistry">The submodel registry.</param>
    void ProcessSubmodelDescriptor(SubmodelDescriptor sd, DateTime timestamp, List<ISubmodelElement> parentCollection,
                                   ISubmodel submodelRegistry);
}