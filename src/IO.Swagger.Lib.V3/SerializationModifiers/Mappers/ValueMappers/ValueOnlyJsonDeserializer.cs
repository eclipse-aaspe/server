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

using AasxServerStandardBib.Interfaces;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    using System.Text.Json;

    public interface IValueOnlyJsonDeserializer
    {
        IValueDTO?     DeserializeSubmodelElementValue(JsonNode node, string? encodedSubmodelIdentifier = null, string? idShortPath = null);
        SubmodelValue? DeserializeSubmodelValue(JsonNode node, string? encodedSubmodelIdentifier);
    }

    public class ValueOnlyJsonDeserializer : IValueOnlyJsonDeserializer
    {
        private readonly ISubmodelService _submodelService;
        private readonly IBase64UrlDecoderService _decoderService;

        public ValueOnlyJsonDeserializer(ISubmodelService submodelService, IBase64UrlDecoderService decoderService)
        {
            _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
            _decoderService  = decoderService ?? throw new ArgumentNullException(nameof(decoderService));
        }

        public IValueDTO? DeserializeSubmodelElementValue(JsonNode node, string? encodedSubmodelIdentifier = null, string idShortPath = null)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            IValueDTO? output = null;

            if (node is JsonObject jsonObject)
            {
                foreach (var keyValue in jsonObject)
                {
                    string idShort = keyValue.Key;
                    var    value   = keyValue.Value;
                    output = Deserialize(idShort, value, encodedSubmodelIdentifier, idShortPath);
                }
            }

            return output;
        }

        private IValueDTO? Deserialize(string idShort, JsonNode value, string? encodedSubmodelIdentifier, string idShortPath)
        {
            IValueDTO? output = null;
            switch (value)
            {
                case JsonValue jsonValue:
                {
                    //Property
                    jsonValue.TryGetValue(out string propertyValue);
                    output = new PropertyValue(idShort, propertyValue);
                    break;
                }
                case JsonObject valueObject:
                {
                    output = ParseJsonValueObject(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);
                    break;
                }
                case JsonArray valueArray:
                {
                    output = ParseJsonValueArray(idShort, valueArray, encodedSubmodelIdentifier, idShortPath);
                    break;
                }
                default:
                {
                    throw new InvalidOperationException();
                }
            }

            return output;
        }

        private IValueDTO? ParseJsonValueArray(string idShort, JsonArray valueArray, string? encodedSubmodelIdentifier, string idShortPath)
        {
            //This is Multilingual Property or SMEList
            var decodedSubmodelId = _decoderService.Decode("submodelId", encodedSubmodelIdentifier);
            var element           = _submodelService.GetSubmodelElementByPath(decodedSubmodelId, idShortPath);
            if (element != null)
            {
                if (element is MultiLanguageProperty)
                {
                    return CreateMultilanguagePropertyValue(idShort, valueArray);
                }
                else if (element is SubmodelElementList smeList)
                {
                    return CreateSubmodelElementList(idShort, valueArray, smeList, encodedSubmodelIdentifier, idShortPath);
                }
                else
                {
                    throw new JsonDeserializationException(idShort, "Element is neither MutlilanguageProperty nor SubmodelElementList");
                }
            }

            return null;
        }

        private IValueDTO CreateSubmodelElementList(string idShort, JsonArray valueArray, SubmodelElementList smeList, string? encodedSubmodelIdentifier, string idShortPath)
        {
            var value = new List<ISubmodelElementValue>();
            foreach (var element in valueArray)
            {
                var elementValueDTO = Deserialize(null, element, encodedSubmodelIdentifier, idShortPath);
                value.Add((ISubmodelElementValue)elementValueDTO);
            }

            return new SubmodelElementListValue(idShort, value);
        }

        private IValueDTO CreateMultilanguagePropertyValue(string idShort, JsonArray valueArray)
        {
            var langStrings = new List<KeyValuePair<string?, string>>();

            foreach (var item in valueArray)
            {
                if (item is JsonObject jsonObject)
                {
                    GetPropertyFromJsonObject(jsonObject, out var propertyName, out var propertyValue);
                    langStrings.Add(new KeyValuePair<string?, string>(propertyName, propertyValue ?? string.Empty));
                }
            }

            return new MultiLanguagePropertyValue(idShort, langStrings);
        }

        private void GetPropertyFromJsonObject(JsonObject jsonObject, out string? propertyName, out string? propertyValue)
        {
            propertyName  = null;
            propertyValue = null;
            foreach (var item in jsonObject)
            {
                propertyName = item.Key;
                var jsonValue = item.Value as JsonValue;
                jsonValue.TryGetValue(out propertyValue);
            }
        }

        private IValueDTO? ParseJsonValueObject(string idShort, JsonObject valueObject, string? encodedSubmodelIdentifier = null, string? idShortPath = null)
        {
            if (valueObject == null) throw new ArgumentNullException();
            if (valueObject.ContainsKey("min") && valueObject.ContainsKey("max"))
            {
                //Range
                return CreateRangeValue(idShort, valueObject);
            }
            else if (valueObject.ContainsKey("contentType"))
            {
                //If it contains both contentType and Value, both File and Blob are possible. Hence, we need to retrieve actual elements from the server
                var decodedSubmodelIdentifier = _decoderService.Decode("submodelIdentifier", encodedSubmodelIdentifier);
                if (idShortPath == null)
                    idShortPath = idShort;
                var submodelElement = _submodelService.GetSubmodelElementByPath(decodedSubmodelIdentifier, idShortPath);
                if (submodelElement != null)
                {
                    if (submodelElement is File)
                    {
                        return CreateFileValue(idShort, valueObject);
                    }
                    else if (submodelElement is Blob)
                    {
                        return CreateBlobValue(idShort, valueObject);
                    }
                    else
                    {
                        throw new JsonDeserializationException(idShort, "Element is neither File nor Blob.");
                    }
                }
            }
            else if (valueObject.ContainsKey("annotations") && valueObject.ContainsKey("first") && valueObject.ContainsKey("second"))
            {
                return CreateAnnotedRelationshipElementValue(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);
            }
            else if (valueObject.ContainsKey("first") && valueObject.ContainsKey("second"))
            {
                return CreateRelationshipElementValue(idShort, valueObject);
            }
            else if (valueObject.ContainsKey("type") && valueObject.ContainsKey("keys"))
            {
                return CreateReferenceElementValue(idShort, valueObject);
            }
            else if (valueObject.ContainsKey("entityType"))
            {
                return CreateEntityValue(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);
            }
            else if (valueObject.ContainsKey("observed"))
            {
                return CreateBasicEventElement(idShort, valueObject);
            }
            else
            {
                //This can be SubmodelElementCollection
                return CreateSubmodelElementCollectionValue(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);
            }

            return null;
        }

        private IValueDTO CreateSubmodelElementCollectionValue(string idShort, JsonObject valueObject, string? encodedSubmodelIdentifier, string idShortPath)
        {
            var submodelElements = new List<ISubmodelElementValue>();

            foreach (var item in valueObject)
            {
                var newNode = new JsonObject(new[] {KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString()))});
                if (idShortPath == null)
                    idShortPath = idShort;
                var newIdShortPath  = idShortPath + "." + item.Key;
                var submodelElement = DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier, newIdShortPath);
                submodelElements.Add((ISubmodelElementValue)submodelElement);
            }

            return new SubmodelElementCollectionValue(idShort, submodelElements);
        }

        private IValueDTO CreateBasicEventElement(string idShort, JsonObject valueObject)
        {
            ReferenceDTO? observed = null;
            if (valueObject["observed"] is not null)
            {
                observed = JsonSerializer.Deserialize<ReferenceDTO>(valueObject.ToString());
            }

            return new BasicEventElementValue(idShort, observed);
        }

        private IValueDTO CreateEntityValue(string idShort, JsonObject valueObject, string? encodedSubmodelIdentifier, string idShortPath)
        {
            string?                     entityType    = null;
            string                      globalAssetId = null;
            List<ISubmodelElementValue> statements    = null;

            JsonValue entityTypeNode = valueObject["entityType"] as JsonValue;
            entityTypeNode?.TryGetValue(out entityType);

            var globalAssetIdNode = valueObject["globalAssetId"] as JsonValue;
            globalAssetIdNode?.TryGetValue(out globalAssetId);

            var statementsNode = valueObject["statements"] as JsonObject;
            if (statementsNode != null)
            {
                statements = new List<ISubmodelElementValue>();
                foreach (var item in statementsNode)
                {
                    var newNode        = new JsonObject(new[] {KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString()))});
                    var newIdShortPath = idShortPath + "." + item.Key;
                    var statement      = DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier, newIdShortPath);
                    statements.Add((ISubmodelElementValue)statement);
                }
            }

            return new EntityValue(idShort, (EntityType)Stringification.EntityTypeFromString(entityType), statements, globalAssetId);
        }

        private IValueDTO CreateReferenceElementValue(string idShort, JsonObject valueObject)
        {
            ReferenceDTO referenceDTO = JsonSerializer.Deserialize<ReferenceDTO>(valueObject.ToString());
            return new ReferenceElementValue(idShort, referenceDTO);
        }

        private IValueDTO CreateRelationshipElementValue(string idShort, JsonObject valueObject)
        {
            ReferenceDTO firstDTO  = null, secondDTO = null;
            JsonNode     firstNode = valueObject["first"];
            if (firstNode != null)
            {
                firstDTO = JsonSerializer.Deserialize<ReferenceDTO>(firstNode.ToString());
            }

            JsonNode secondNode = valueObject["second"];
            if (secondNode != null)
            {
                secondDTO = JsonSerializer.Deserialize<ReferenceDTO>(firstNode.ToString());
            }

            return new RelationshipElementValue(idShort, firstDTO, secondDTO);
        }

        private IValueDTO CreateAnnotedRelationshipElementValue(string idShort, JsonObject valueObject, string? encodedSubmodelIdentifier, string idShortPath)
        {
            ReferenceDTO firstDTO  = null, secondDTO = null;
            JsonNode     firstNode = valueObject["first"];
            if (firstNode != null)
            {
                firstDTO = JsonSerializer.Deserialize<ReferenceDTO>(firstNode.ToString());
            }

            JsonNode secondNode = valueObject["second"];
            if (secondNode != null)
            {
                secondDTO = JsonSerializer.Deserialize<ReferenceDTO>(firstNode.ToString());
            }

            JsonArray annotationsNode = valueObject["annotations"] as JsonArray;
            if (annotationsNode != null)
            {
                var annotations = new List<ISubmodelElementValue>();
                foreach (var annotationNode in annotationsNode)
                {
                    var annotation = DeserializeSubmodelElementValue(annotationNode, encodedSubmodelIdentifier, idShortPath);
                    annotations.Add((ISubmodelElementValue)annotation);
                }

                return new AnnotatedRelationshipElementValue(idShort, firstDTO, secondDTO, annotations);
            }

            return new AnnotatedRelationshipElementValue(idShort, firstDTO, secondDTO);
        }

        private IValueDTO CreateBlobValue(string idShort, JsonObject valueObject)
        {
            string? contentType     = null;
            var     contentTypeNode = valueObject["contentType"] as JsonValue;
            contentTypeNode?.TryGetValue(out contentType);

            var valueNode = valueObject["value"] as JsonValue;
            if (valueNode != null)
            {
                valueNode.TryGetValue(out string value);
                return new BlobValue(idShort, contentType, System.Convert.FromBase64String(value));
            }

            return new BlobValue(idShort, contentType);
        }

        private IValueDTO CreateFileValue(string idShort, JsonObject valueObject)
        {
            string? contentType     = null;
            string  value           = null;
            var     contentTypeNode = valueObject["contentType"] as JsonValue;
            contentTypeNode?.TryGetValue(out contentType);
            var valueNode = valueObject["value"] as JsonValue;
            valueNode?.TryGetValue(out value);
            return new FileValue(idShort, contentType, value);
        }

        private static IValueDTO CreateRangeValue(string idShort, JsonObject valueObject)
        {
            string min     = null, max = null;
            var    minNode = valueObject["min"] as JsonValue;
            minNode?.TryGetValue(out min);
            var maxNode = valueObject["max"] as JsonValue;
            maxNode?.TryGetValue(out max);

            return new RangeValue(idShort, min, max);
        }

        public SubmodelValue DeserializeSubmodelValue(JsonNode node, string? encodedSubmodelIdentifier)
        {
            var submodelElements = new List<ISubmodelElementValue>();
            var jsonObject       = node as JsonObject;
            foreach (var item in jsonObject)
            {
                var newNode         = new JsonObject(new[] {KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString()))});
                var submodelElement = DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier);
                submodelElements.Add((ISubmodelElementValue)submodelElement);
            }

            return new SubmodelValue(submodelElements);
        }
    }
}