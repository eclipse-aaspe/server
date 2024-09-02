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

/// <summary>
/// Provides methods for extracting and manipulating properties within a submodel element collection.
/// </summary>
public interface ISubmodelPropertyExtractionService
{
    /// <summary>
    /// Finds and returns properties from a submodel element collection that match specific criteria.
    /// </summary>
    /// <param name="elementCollection">The collection of submodel elements to search through.</param>
    /// <param name="aasID">The ID of the Asset Administration Shell to match against.</param>
    /// <param name="assetID">The ID of the asset to match against.</param>
    /// <returns>
    /// A tuple containing:
    /// - The count of found properties matching the criteria.
    /// - The property with IdShort "descriptorJSON", if found.
    /// - The property with IdShort "endpoint", if found.
    /// </returns>
    (int found, Property? jsonProperty, Property? endpointProperty) FindMatchingProperties(SubmodelElementCollection elementCollection, string? aasID, string? assetID);

    /// <summary>
    /// Adds a new property to a submodel element collection.
    /// </summary>
    /// <param name="collection">The collection to which the property will be added.</param>
    /// <param name="idShort">The IdShort of the property.</param>
    /// <param name="value">The value of the property.</param>
    /// <param name="timestamp">The timestamp for when the property is created.</param>
    /// <param name="checkEmpty">If true, the property is only added if the value is not empty.</param>
    /// <example>
    /// <code>
    /// var collection = new SubmodelElementCollection();
    /// SubmodelPropertyExtractionService.AddPropertyToCollection(collection, "exampleID", "exampleValue", DateTime.UtcNow);
    /// </code>
    /// </example>
    void AddPropertyToCollection(SubmodelElementCollection collection, string idShort, string value, DateTime timestamp, bool checkEmpty = false);

    /// <summary>
    /// Creates a new submodel element collection with a specified IdShort and timestamp.
    /// </summary>
    /// <param name="idShort">The IdShort of the collection.</param>
    /// <param name="timestamp">The timestamp for when the collection is created.</param>
    /// <returns>A new instance of <see cref="SubmodelElementCollection"/>.</returns>
    /// <example>
    /// <code>
    /// var collection = SubmodelPropertyExtractionService.CreateSubmodelElementCollection("exampleID", DateTime.UtcNow);
    /// </code>
    /// </example>
    SubmodelElementCollection CreateSubmodelElementCollection(string idShort, DateTime timestamp);
}