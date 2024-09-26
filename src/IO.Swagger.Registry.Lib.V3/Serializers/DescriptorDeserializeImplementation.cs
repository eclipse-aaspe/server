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

using IO.Swagger.Registry.Lib.V3.Models;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace IO.Swagger.Registry.Lib.V3.Serializers
{
    public static class DescriptorDeserializeImplementation
    {
        internal static AssetAdministrationShellDescriptor? AssetAdministrationShellDescriptorFrom(JsonNode node, out Reporting.Error? error)
        {
            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                                            $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            AdministrativeInformation administrativeInformation = null;
            AssetKind                 assetKind                 = AssetKind.NotApplicable;
            string?                   assetType                 = null;
            List<Endpoint>            endpoints                 = null;
            string?                   globalAssetId             = null;
            string?                   idShort                   = null;
            string?                   id                        = null;
            List<SpecificAssetId>     specificAssetIds          = null;
            List<SubmodelDescriptor>  submodelDescriptors       = null;

            foreach (var keyValue in obj)
            {
                if (keyValue.Value == null)
                {
                    continue;
                }

                switch (keyValue.Key)
                {
                    case "administration":
                    {
                        if (keyValue.Value == null)
                        {
                            continue;
                        }

                        administrativeInformation = Jsonization.Deserialize.AdministrativeInformationFrom(keyValue.Value);
                        break;
                    }
                    case "assetKind":
                    {
                        if (keyValue.Value == null)
                        {
                            continue;
                        }

                        assetKind = Jsonization.Deserialize.AssetKindFrom(keyValue.Value);
                        break;
                    }
                    case "assetType":
                    {
                        if (keyValue.Value == null)
                        {
                            continue;
                        }

                        assetType = StringFrom(
                                               keyValue.Value,
                                               out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "id"));
                            return null;
                        }

                        break;
                    }
                    case "endpoints":
                    {
                        if (keyValue.Value == null)
                        {
                            continue;
                        }

                        JsonArray? arrayEndpoints = keyValue.Value as JsonArray;
                        if (arrayEndpoints == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "endpoints"));
                            return null;
                        }

                        endpoints = new List<Endpoint>(
                                                       arrayEndpoints.Count);
                        int indexEndpoint = 0;
                        foreach (JsonNode? item in arrayEndpoints)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexEndpoint));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoints"));
                                return null;
                            }

                            Endpoint? parsedItem = EndpointFrom(
                                                                item ?? throw new System.InvalidOperationException(),
                                                                out error);
                            if (error != null)
                            {
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexEndpoint));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoint"));
                                return null;
                            }

                            endpoints.Add(
                                          parsedItem
                                          ?? throw new System.InvalidOperationException(
                                                                                        "Unexpected result null when error is null"));
                            indexEndpoint++;
                        }

                        break;
                    }
                    case "globalAssetId":
                    {
                        globalAssetId = StringFrom(
                                                   keyValue.Value,
                                                   out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "globalAssetId"));
                            return null;
                        }

                        break;
                    }
                    case "idShort":
                    {
                        idShort = StringFrom(
                                             keyValue.Value,
                                             out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "idShort"));
                            return null;
                        }

                        break;
                    }
                    case "id":
                    {
                        id = StringFrom(
                                        keyValue.Value,
                                        out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "id"));
                            return null;
                        }

                        break;
                    }
                    case "specificAssetIds":
                    {
                        JsonArray? arraySpecificAssetIds = keyValue.Value as JsonArray;
                        if (arraySpecificAssetIds == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "specificAssetIds"));
                            return null;
                        }

                        specificAssetIds = new List<SpecificAssetId>(arraySpecificAssetIds.Count);
                        int indexSpecificAssetId = 0;
                        foreach (JsonNode item in arraySpecificAssetIds)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexSpecificAssetId));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoints"));
                                return null;
                            }

                            SpecificAssetId specificAssetId = Jsonization.Deserialize.SpecificAssetIdFrom(item);
                            specificAssetIds.Add(specificAssetId);
                        }

                        break;
                    }
                    case "submodelDescriptors":
                    {
                        JsonArray? arraySubmodelDescriptors = keyValue.Value as JsonArray;
                        if (arraySubmodelDescriptors == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "submodelDescriptors"));
                            return null;
                        }

                        submodelDescriptors = new List<SubmodelDescriptor>(
                                                                           arraySubmodelDescriptors.Count);
                        int indexSubmodelDescriptors = 0;
                        foreach (JsonNode? item in arraySubmodelDescriptors)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexSubmodelDescriptors));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "submodelDescriptors"));
                                return null;
                            }

                            SubmodelDescriptor? parsedItem = SubmodelDescriptorFrom(
                                                                                    item ?? throw new System.InvalidOperationException(),
                                                                                    out error);
                            if (error != null)
                            {
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexSubmodelDescriptors));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "submodelDescriptors"));
                                return null;
                            }

                            submodelDescriptors.Add(
                                                    parsedItem
                                                    ?? throw new System.InvalidOperationException(
                                                                                                  "Unexpected result null when error is null"));
                            indexSubmodelDescriptors++;
                        }

                        break;
                    }
                }
            }

            return new AssetAdministrationShellDescriptor(administrativeInformation, assetKind, assetType, endpoints, globalAssetId, idShort, id, specificAssetIds,
                                                          submodelDescriptors);
        }

        internal static SubmodelDescriptor? SubmodelDescriptorFrom(JsonNode node, out Reporting.Error? error)
        {
            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                                            $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            AdministrativeInformation administration         = null;
            List<Endpoint>            endpoints              = null;
            string?                   idShort                = null;
            string?                   id                     = null;
            Reference                 semanticId             = null;
            List<Reference>           supplementalSemanticId = null;
            List<String>              federatedElements      = null;

            foreach (var keyValue in obj)
            {
                if (keyValue.Value == null)
                {
                    continue;
                }

                switch (keyValue.Key)
                {
                    case "administration":
                    {
                        administration = Jsonization.Deserialize.AdministrativeInformationFrom(keyValue.Value);
                        break;
                    }
                    case "endpoints":
                    {
                        if (keyValue.Value == null)
                        {
                            continue;
                        }

                        JsonArray? arrayEndpoints = keyValue.Value as JsonArray;
                        if (arrayEndpoints == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "endpoints"));
                            return null;
                        }

                        endpoints = new List<Endpoint>(
                                                       arrayEndpoints.Count);
                        int indexEndpoint = 0;
                        foreach (JsonNode? item in arrayEndpoints)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexEndpoint));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoints"));
                                return null;
                            }

                            Endpoint? parsedItem = EndpointFrom(
                                                                item ?? throw new System.InvalidOperationException(),
                                                                out error);
                            if (error != null)
                            {
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexEndpoint));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoint"));
                                return null;
                            }

                            endpoints.Add(
                                          parsedItem
                                          ?? throw new System.InvalidOperationException(
                                                                                        "Unexpected result null when error is null"));
                            indexEndpoint++;
                        }

                        break;
                    }
                    case "idShort":
                    {
                        idShort = StringFrom(
                                             keyValue.Value,
                                             out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "idShort"));
                            return null;
                        }

                        break;
                    }
                    case "id":
                    {
                        id = StringFrom(
                                        keyValue.Value,
                                        out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "id"));
                            return null;
                        }

                        break;
                    }
                    case "semanticId":
                    {
                        semanticId = Jsonization.Deserialize.ReferenceFrom(keyValue.Value);
                        break;
                    }
                    case "supplementalSemanticId":
                    {
                        JsonArray? arraySupplementalSemanticId = keyValue.Value as JsonArray;
                        if (arraySupplementalSemanticId == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "supplementalSemanticId"));
                            return null;
                        }

                        supplementalSemanticId = new List<Reference>(arraySupplementalSemanticId.Count);
                        int indexSpecificAssetId = 0;
                        foreach (JsonNode item in arraySupplementalSemanticId)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexSpecificAssetId));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "supplementalSemanticId"));
                                return null;
                            }

                            Reference suppSemanticId = Jsonization.Deserialize.ReferenceFrom(item);
                            supplementalSemanticId.Add(suppSemanticId);
                        }

                        break;
                    }
                    case "federatedElements":
                    {
                        JsonArray? arrayFederatedElements = keyValue.Value as JsonArray;
                        if (arrayFederatedElements == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "federatedElements"));
                            return null;
                        }

                        federatedElements = new List<string>(
                                                             arrayFederatedElements.Count);
                        int indexFederatedElements = 0;
                        foreach (JsonNode? item in arrayFederatedElements)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexFederatedElements));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "federatedElements"));
                                return null;
                            }

                            string? parsedItem = StringFrom(
                                                            item ?? throw new System.InvalidOperationException(),
                                                            out error);
                            if (error != null)
                            {
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexFederatedElements));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoint"));
                                return null;
                            }

                            federatedElements.Add(
                                                  parsedItem
                                                  ?? throw new System.InvalidOperationException(
                                                                                                "Unexpected result null when error is null"));
                            indexFederatedElements++;
                        }

                        break;
                    }
                }
            }

            return new SubmodelDescriptor(administration, endpoints, idShort, id, semanticId, supplementalSemanticId, federatedElements);
        }

        private static Endpoint? EndpointFrom(JsonNode node, out Reporting.Error? error)
        {
            error = null;

            var obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                                            $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            string?              _interface          = null;
            ProtocolInformation? protocolInformation = null;

            foreach (var keyValue in obj)
            {
                if (keyValue.Value == null)
                {
                    continue;
                }

                switch (keyValue.Key)
                {
                    case "interface":
                    {
                        _interface = StringFrom(
                                                keyValue.Value,
                                                out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "interface"));
                            return null;
                        }

                        break;
                    }
                    case "protocolInformation":
                    {
                        protocolInformation = ProtocolInformationFrom(
                                                                      keyValue.Value,
                                                                      out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "protocolInformation"));
                            return null;
                        }

                        break;
                    }
                }
            }

            return new Endpoint(_interface, protocolInformation);
        }

        private static ProtocolInformation? ProtocolInformationFrom(JsonNode node, out Reporting.Error? error)
        {
            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                                            $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            string?                                     href                    = null;
            string?                                     endpointProtocol        = null;
            List<string>                                endpointProtocolVersion = null;
            string?                                     subprotocol             = null;
            string?                                     subprotocolBody         = null;
            string?                                     subprotocolBodyEncoding = null;
            List<ProtocolInformationSecurityAttributes> securityAttributes      = null;

            foreach (var keyValue in obj)
            {
                if (keyValue.Value == null)
                {
                    continue;
                }

                switch (keyValue.Key)
                {
                    case "href":
                    {
                        href = StringFrom(
                                          keyValue.Value,
                                          out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "href"));
                            return null;
                        }

                        break;
                    }
                    case "endpointProtocol":
                    {
                        endpointProtocol = StringFrom(
                                                      keyValue.Value,
                                                      out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "endpointProtocol"));
                            return null;
                        }

                        break;
                    }
                    case "endpointProtocolVersion":
                    {
                        if (keyValue.Value is not JsonArray arrayEndpointsProtocolVersion)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "EndpointsProtocolVersion"));
                            return null;
                        }

                        endpointProtocolVersion = new List<string>(
                                                                   arrayEndpointsProtocolVersion.Count);
                        var indexEndpointsProtocolVersion = 0;
                        foreach (JsonNode? item in arrayEndpointsProtocolVersion)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexEndpointsProtocolVersion));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "EndpointsProtocolVersion"));
                                return null;
                            }

                            string? parsedItem = StringFrom(
                                                            item ?? throw new System.InvalidOperationException(),
                                                            out error);
                            if (error != null)
                            {
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexEndpointsProtocolVersion));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "endpoint"));
                                return null;
                            }

                            endpointProtocolVersion.Add(
                                                        parsedItem
                                                        ?? throw new System.InvalidOperationException(
                                                                                                      "Unexpected result null when error is null"));
                            indexEndpointsProtocolVersion++;
                        }

                        break;
                    }
                    case "subprotocol":
                    {
                        subprotocol = StringFrom(
                                                 keyValue.Value,
                                                 out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "subprotocol"));
                            return null;
                        }

                        break;
                    }
                    case "subprotocolBody":
                    {
                        subprotocolBody = StringFrom(
                                                     keyValue.Value,
                                                     out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "subprotocolBody"));
                            return null;
                        }

                        break;
                    }
                    case "subprotocolBodyEncoding":
                    {
                        subprotocolBodyEncoding = StringFrom(
                                                             keyValue.Value,
                                                             out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "subprotocolBodyEncoding"));
                            return null;
                        }

                        break;
                    }
                    case "securityAttributes":
                    {
                        JsonArray? arraySecurityAttributes = keyValue.Value as JsonArray;
                        if (arraySecurityAttributes == null)
                        {
                            error = new Reporting.Error(
                                                        $"Expected a JsonArray, but got {keyValue.Value.GetType()}");
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "securityAttributes"));
                            return null;
                        }

                        securityAttributes = new List<ProtocolInformationSecurityAttributes>(
                                                                                             arraySecurityAttributes.Count);
                        int indexSecurityAttributes = 0;
                        foreach (JsonNode? item in arraySecurityAttributes)
                        {
                            if (item == null)
                            {
                                error = new Reporting.Error(
                                                            "Expected a non-null item, but got a null");
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexSecurityAttributes));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "SecurityAttributes"));
                                return null;
                            }

                            ProtocolInformationSecurityAttributes? parsedItem = ProtocolInformationSecurityAttributesFrom(
                                 item ?? throw new System.InvalidOperationException(),
                                 out error);
                            if (error != null)
                            {
                                error.PrependSegment(
                                                     new Reporting.IndexSegment(
                                                                                indexSecurityAttributes));
                                error.PrependSegment(
                                                     new Reporting.NameSegment(
                                                                               "SecurityAttributes"));
                                return null;
                            }

                            securityAttributes.Add(
                                                   parsedItem
                                                   ?? throw new System.InvalidOperationException(
                                                                                                 "Unexpected result null when error is null"));
                            indexSecurityAttributes++;
                        }

                        break;
                    }
                }
            }

            return new ProtocolInformation(href, endpointProtocol, endpointProtocolVersion, subprotocol, subprotocolBody, subprotocolBodyEncoding, securityAttributes);
        }

        private static ProtocolInformationSecurityAttributes? ProtocolInformationSecurityAttributesFrom(JsonNode node, out Reporting.Error? error)
        {
            error = null;

            JsonObject? obj = node as JsonObject;
            if (obj == null)
            {
                error = new Reporting.Error(
                                            $"Expected a JsonObject, but got {node.GetType()}");
                return null;
            }

            ProtocolInformationSecurityAttributes.TypeEnum type  = ProtocolInformationSecurityAttributes.TypeEnum.NONE;
            string?                                        key   = null;
            string                                         value = null;

            foreach (var keyValue in obj)
            {
                if (keyValue.Value == null)
                {
                    continue;
                }

                switch (keyValue.Key)
                {
                    case "type":
                    {
                        //Enum.TryParse(typeof(ProtocolInformationSecurityAttributes.TypeEnum), out type);
                        break;
                    }
                    case "key":
                    {
                        key = StringFrom(
                                         keyValue.Value,
                                         out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "key"));
                            return null;
                        }

                        break;
                    }
                    case "value":
                    {
                        key = StringFrom(
                                         keyValue.Value,
                                         out error);
                        if (error != null)
                        {
                            error.PrependSegment(
                                                 new Reporting.NameSegment(
                                                                           "value"));
                            return null;
                        }

                        break;
                    }
                }
            }

            return new ProtocolInformationSecurityAttributes(type, key, value);
        }

        internal static string? StringFrom(
            JsonNode node,
            out Reporting.Error? error)
        {
            error = null;
            JsonValue? value = node as JsonValue;
            if (value == null)
            {
                error = new Reporting.Error(
                                            $"Expected a JsonValue, but got {node.GetType()}");
                return null;
            }

            bool ok = value.TryGetValue<string>(out string? result);
            if (!ok)
            {
                error = new Reporting.Error(
                                            "Expected a string, but the conversion failed " +
                                            $"from {value.ToJsonString()}");
                return null;
            }

            if (result == null)
            {
                error = new Reporting.Error(
                                            "Expected a string, but got a null");
                return null;
            }

            return result;
        }
    }
}