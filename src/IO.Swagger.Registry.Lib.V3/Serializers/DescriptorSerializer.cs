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
using System.Text.Json.Nodes;

namespace IO.Swagger.Registry.Lib.V3.Serializers
{
    public static class DescriptorSerializer
    {
        public static JsonObject? ToJsonObject(object that)
        {
            if (that == null)
                return null;
            if (that is AssetAdministrationShellDescriptor aasDesc)
            {
                return Transform(aasDesc);
            }
            else if (that is SubmodelDescriptor smDesc)
            {
                return Transform(smDesc);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static JsonObject? Transform(AssetAdministrationShellDescriptor that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();
            if (that.Administration != null)
            {
                result[ "administration" ] = Jsonization.Serialize.ToJsonObject(that.Administration);
            }

            if (that.AssetKind != null)
            {
                result[ "assetKind" ] = Jsonization.Serialize.AssetKindToJsonValue(
                    that.AssetKind);
            }

            if (that.AssetType != null)
            {
                result[ "assetType" ] = that.AssetType;
            }

            if (that.Endpoints != null)
            {
                var arrayEndpoints = new JsonArray();
                foreach (var endpoint in that.Endpoints)
                {
                    arrayEndpoints.Add(Transform(endpoint));
                }

                result[ "endpoints" ] = arrayEndpoints;
            }

            if (that.GlobalAssetId != null)
            {
                result[ "globalAssetId" ] = that.GlobalAssetId;
            }

            if (that.IdShort != null)
            {
                result[ "idShort" ] = that.IdShort;
            }

            if (that.Id != null)
            {
                result[ "id" ] = that.Id;
            }

            if (that.SpecificAssetIds != null)
            {
                var arraySpecificAssetIds = new JsonArray();
                foreach (var specificAssetId in that.SpecificAssetIds)
                {
                    arraySpecificAssetIds.Add(Jsonization.Serialize.ToJsonObject(specificAssetId));
                }

                result[ "specificAssetIds" ] = arraySpecificAssetIds;
            }

            if (that.SubmodelDescriptors != null)
            {
                var arraySubmodelDescriptors = new JsonArray();
                foreach (var submodelDescriptor in that.SubmodelDescriptors)
                {
                    arraySubmodelDescriptors.Add(Transform(submodelDescriptor));
                }

                result[ "submodelDescriptors" ] = arraySubmodelDescriptors;
            }

            return result;
        }

        private static JsonObject? Transform(SubmodelDescriptor that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();
            if (that.Administration != null)
            {
                result[ "administration" ] = Jsonization.Serialize.ToJsonObject(that.Administration);
            }

            if (that.Endpoints != null)
            {
                var arrayEndpoints = new JsonArray();
                foreach (var endpoint in that.Endpoints)
                {
                    arrayEndpoints.Add(Transform(endpoint));
                }

                result[ "endpoints" ] = arrayEndpoints;
            }

            if (that.IdShort != null)
            {
                result[ "idShort" ] = that.IdShort;
            }

            if (that.Id != null)
            {
                result[ "id" ] = that.Id;
            }

            if (that.SemanticId != null)
            {
                result[ "semanticId" ] = Jsonization.Serialize.ToJsonObject(that.SemanticId);
            }

            if (that.SupplementalSemanticId != null)
            {
                var arraySupplementalSemanticId = new JsonArray();
                foreach (var suppSemId in that.SupplementalSemanticId)
                {
                    arraySupplementalSemanticId.Add(Jsonization.Serialize.ToJsonObject(suppSemId));
                }

                result[ "supplementalSemanticId" ] = arraySupplementalSemanticId;
            }

            if (that.FederatedElements != null)
            {
                var arrayFederatedElements = new JsonArray();
                foreach (var fedElement in that.FederatedElements)
                {
                    arrayFederatedElements.Add(fedElement);
                }

                result[ "federatedElements" ] = arrayFederatedElements;
            }

            return result;
        }

        private static JsonObject Transform(Endpoint that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();

            if (that.Interface != null)
            {
                result[ "interface" ] = that.Interface;
            }

            if (that.ProtocolInformation != null)
            {
                result[ "protocolInformation" ] = Transform(that.ProtocolInformation);
            }

            return result;
        }

        private static JsonObject Transform(ProtocolInformation that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();

            if (that.Href != null)
            {
                result[ "href" ] = that.Href;
            }

            if (that.EndpointProtocol != null)
            {
                result[ "endpointProtocol" ] = that.EndpointProtocol;
            }

            if (that.EndpointProtocolVersion != null)
            {
                var arrayProtocolVersion = new JsonArray();
                foreach (var protocol in that.EndpointProtocolVersion)
                {
                    arrayProtocolVersion.Add(protocol);
                }

                result[ "endpointProtocolVersion" ] = arrayProtocolVersion;
            }

            if (that.Subprotocol != null)
            {
                result[ "subprotocol" ] = that.Subprotocol;
            }

            if (that.SubprotocolBody != null)
            {
                result[ "subprotocolBody" ] = that.SubprotocolBody;
            }

            if (that.SubprotocolBodyEncoding != null)
            {
                result[ "subprotocolBodyEncoding" ] = that.SubprotocolBodyEncoding;
            }

            if (that.SecurityAttributes != null)
            {
                var arraySecurityAttributes = new JsonArray();
                foreach (var securityAttribute in that.SecurityAttributes)
                {
                    arraySecurityAttributes.Add(Transform(securityAttribute));
                }

                result[ "securityAttributes" ] = arraySecurityAttributes;
            }

            return result;
        }

        private static JsonObject Transform(ProtocolInformationSecurityAttributes that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));

            var result = new JsonObject();
            if (that.Type != null)
            {
                result[ "type" ] = that.Type.ToString();
            }

            if (that.Key != null)
            {
                result[ "key" ] = that.Key;
            }

            if (that.Value != null)
            {
                result[ "value" ] = that.Value;
            }

            return result;
        }
    }
}