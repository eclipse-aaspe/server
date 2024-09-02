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
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Extensions;
using global::Extensions;
using Models;
using Serializers;

public class AasDescriptorWritingService : IAasDescriptorWritingService
{
    private static int submodelRegistryCount;
    private readonly ISubmodelPropertyExtractionService _submodelPropertyExtractionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AasDescriptorWritingService"/> class.
    /// </summary>
    /// <param name="submodelPropertyExtractionService">The service for extracting properties from submodel elements.</param>
    public AasDescriptorWritingService(ISubmodelPropertyExtractionService submodelPropertyExtractionService)
    {
        _submodelPropertyExtractionService = submodelPropertyExtractionService;
    }

    /// <inheritdoc />
    public void AddNewEntry(AssetAdministrationShellDescriptor ad, ISubmodel aasRegistry, ISubmodel submodelRegistry, DateTime timestamp, string? aasID, string? assetID,
                            string? endpoint)
    {
        if (aasRegistry is not {SubmodelElements: not null})
        {
            return;
        }

        var collection = _submodelPropertyExtractionService.CreateSubmodelElementCollection($"ShellDescriptor_{aasRegistry.SubmodelElements.Count}", timestamp);

        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "idShort", ad.IdShort ?? string.Empty, timestamp);
        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "aasID", aasID ?? string.Empty, timestamp);
        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "assetID", assetID ?? string.Empty, timestamp, checkEmpty: true);
        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "endpoint", endpoint ?? string.Empty, timestamp);
        _submodelPropertyExtractionService.AddPropertyToCollection(collection, "descriptorJSON", DescriptorSerializer.ToJsonObject(ad)?.ToJsonString() ?? string.Empty,
                                                                   timestamp);

        aasRegistry?.SubmodelElements.Add(collection);

        if (ad.SubmodelDescriptors == null)
        {
            return;
        }

        foreach (var sd in ad.SubmodelDescriptors)
        {
            if (collection.Value != null && submodelRegistry != null)
            {
                ProcessSubmodelDescriptor(sd, timestamp, collection.Value, submodelRegistry);
            }
        }
    }

    /// <inheritdoc />
    public bool OverwriteExistingEntryForEdenticalIds(AssetAdministrationShellDescriptor ad, ISubmodel aasRegistry, DateTime timestamp, bool initial, string? aasID,
                                                      string? assetID,
                                                      string? endpoint)
    {
        if (initial || aasRegistry?.SubmodelElements is null)
        {
            return false;
        }

        foreach (var element in aasRegistry.SubmodelElements)
        {
            if (element is not SubmodelElementCollection elementCollection)
            {
                continue;
            }

            var (found, jsonProperty, endpointProperty) = _submodelPropertyExtractionService.FindMatchingProperties(elementCollection, aasID, assetID);

            if (found != 2 || jsonProperty == null)
            {
                continue;
            }

            UpdateProperties(jsonProperty, endpointProperty, ad, timestamp, endpoint);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates properties of an existing AAS descriptor.
    /// </summary>
    /// <param name="jsonProperty">The JSON property to update.</param>
    /// <param name="endpointProperty">The endpoint property to update.</param>
    /// <param name="ad">The AAS descriptor.</param>
    /// <param name="timestamp">The timestamp for the update.</param>
    /// <param name="endpoint">The endpoint for the AAS.</param>
    private static void UpdateProperties(Property jsonProperty, Property? endpointProperty, AssetAdministrationShellDescriptor ad, DateTime timestamp, string? endpoint)
    {
        var jsonString = DescriptorSerializer.ToJsonObject(ad)?.ToJsonString();

        jsonProperty.TimeStampCreate = timestamp;
        jsonProperty.TimeStamp       = timestamp;
        jsonProperty.Value           = jsonString;

        if (endpointProperty != null)
        {
            endpointProperty.TimeStampCreate = timestamp;
            endpointProperty.TimeStamp       = timestamp;
            endpointProperty.Value           = endpoint;
        }

        Console.WriteLine("Replace Descriptor:");
        Console.WriteLine(jsonString);
    }

    /// <inheritdoc />
    public void ProcessSubmodelDescriptor(SubmodelDescriptor sd, DateTime timestamp, List<ISubmodelElement> parentCollection,
                                          ISubmodel submodelRegistry)
    {
        var submodelCollection = _submodelPropertyExtractionService.CreateSubmodelElementCollection($"SubmodelDescriptor_{submodelRegistryCount++}", timestamp);

        _submodelPropertyExtractionService.AddPropertyToCollection(submodelCollection, "idShort", sd.IdShort ?? string.Empty, timestamp);
        _submodelPropertyExtractionService.AddPropertyToCollection(submodelCollection, "submodelID", sd.Id ?? string.Empty, timestamp);
        _submodelPropertyExtractionService.AddPropertyToCollection(submodelCollection, "semanticID", sd.SemanticId?.GetAsExactlyOneKey()?.Value ?? string.Empty, timestamp,
                                                                   checkEmpty: true);

        if (sd.Endpoints?.Count > 0)
        {
            var endpoint = sd.Endpoints[0].ProtocolInformation?.Href;
            _submodelPropertyExtractionService.AddPropertyToCollection(submodelCollection, "endpoint", endpoint ?? string.Empty, timestamp);
        }

        _submodelPropertyExtractionService.AddPropertyToCollection(submodelCollection, "descriptorJSON", DescriptorSerializer.ToJsonObject(sd)?.ToJsonString() ?? string.Empty,
                                                                   timestamp);

        var federatedCollection = _submodelPropertyExtractionService.CreateSubmodelElementCollection("federatedElements", timestamp);
        submodelCollection.Value?.Add(federatedCollection);

        submodelRegistry.SubmodelElements?.Add(submodelCollection);
        submodelRegistry.SetAllParents(timestamp);

        var referenceElement = new ReferenceElement(idShort: $"ref_Submodel_{submodelRegistryCount}")
                               {
                                   TimeStampCreate = timestamp, TimeStamp = timestamp, Value = submodelCollection.GetModelReference(true)
                               };

        referenceElement.ReverseReferenceKeys();
        parentCollection?.Add(referenceElement);

        if (sd.IdShort == "NameplateVC" && sd.Endpoints?.Count > 0)
        {
            var ep = sd.Endpoints[0].ProtocolInformation?.Href;
            _submodelPropertyExtractionService.AddPropertyToCollection(submodelCollection, "NameplateVC", ep ?? string.Empty, timestamp);
        }

        if (sd.FederatedElements != null)
        {
            ProcessFederatedElements(sd.FederatedElements, federatedCollection, timestamp);
        }
    }

    /// <summary>
    /// Processes federated elements and adds them to a federated collection.
    /// </summary>
    /// <param name="federatedElements">The federated elements to process.</param>
    /// <param name="federatedCollection">The collection to which the elements will be added.</param>
    /// <param name="timestamp">The timestamp for the processing.</param>
    private static void ProcessFederatedElements(IEnumerable<string> federatedElements, SubmodelElementCollection federatedCollection, DateTime timestamp)
    {
        foreach (var fe in federatedElements)
        {
            try
            {
                var node = System.Text.Json.JsonSerializer.Deserialize<JsonNode>(new MemoryStream(Encoding.UTF8.GetBytes(fe)));
                var sme  = Jsonization.Deserialize.ISubmodelElementFrom(node);

                if (sme == null)
                {
                    continue;
                }

                sme.TimeStampCreate = timestamp;
                sme.TimeStamp       = timestamp;
                federatedCollection.Value?.Add(sme);
            }
            catch
            {
                // ignored
            }
        }
    }
}