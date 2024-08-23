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

using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    public static class ValueOnlyJsonSerializer
    {
        internal static JsonNode? ToJsonObject(IValueDTO that)
        {
            if (that == null)
                throw new ArgumentNullException(nameof(that));
            if (that is PropertyValue propertyValue)
            {
                return Transform(propertyValue);
            }
            else if (that is MultiLanguagePropertyValue multiLanguagePropertyValue)
            {
                return Transform(multiLanguagePropertyValue);
            }
            else if (that is BasicEventElementValue basicEventElementValue)
            {
                return Transform(basicEventElementValue);
            }
            else if (that is BlobValue blobValue)
            {
                return Transform(blobValue);
            }
            else if (that is FileValue fileValue)
            {
                return Transform(fileValue);
            }
            else if (that is RangeValue rangeValue)
            {
                return Transform(rangeValue);
            }
            else if (that is ReferenceElementValue referenceElementValue)
            {
                return Transform(referenceElementValue);
            }
            else if (that is RelationshipElementValue relationshipElementValue)
            {
                return Transform(relationshipElementValue);
            }
            else if (that is SubmodelElementCollectionValue submodelElementCollectionValue)
            {
                return Transform(submodelElementCollectionValue);
            }
            else if (that is SubmodelElementListValue submodelElementListValue)
            {
                return Transform(submodelElementListValue);
            }
            else if (that is AnnotatedRelationshipElementValue annotatedRelEleValue)
            {
                return Transform(annotatedRelEleValue);
            }
            else if (that is EntityValue EntityValue)
            {
                return Transform(EntityValue);
            }
            else if (that is SubmodelValue submodelValue)
            {
                return Transform(submodelValue);
            }


            return null;
        }

        private static JsonNode Transform(EntityValue entityValue)
        {
            var result = new JsonObject();

            var statements = new JsonObject();
            if (entityValue.Statements != null)
            {
                foreach (var element in entityValue.Statements)
                {
                    var elementObject = ToJsonObject(element) as JsonObject;
                    foreach (var keyValue in elementObject)
                    {
                        var valueString = keyValue.Value.ToJsonString();
                        var valueNode = JsonSerializer.Deserialize<JsonNode>(valueString);
                        statements[keyValue.Key] = valueNode;
                    }
                }
            }

            var valueObject = new JsonObject();
            valueObject["statements"] = statements;
            valueObject["entityType"] = Jsonization.Serialize.EntityTypeToJsonValue(entityValue.EntityType);
            valueObject["globalAssetId"] = entityValue.GlobalAssetId;

            result[entityValue.IdShort] = valueObject;

            return result;
        }

        private static JsonNode Transform(AnnotatedRelationshipElementValue annotatedRelEleValue)
        {
            var result = new JsonObject();

            var annotations = new JsonArray();
            if (annotatedRelEleValue.Annotations != null)
            {
                foreach (var element in annotatedRelEleValue.Annotations)
                {
                    var elementObject = ToJsonObject(element);
                    annotations.Add(elementObject);
                }
            }

            var valueObject = new JsonObject();
            valueObject["first"] = Transform(annotatedRelEleValue.First);
            valueObject["second"] = Transform(annotatedRelEleValue.Second);
            valueObject["annotations"] = annotations;

            result[annotatedRelEleValue.IdShort] = valueObject;

            return result;
        }

        private static JsonNode Transform(SubmodelValue submodelValue)
        {
            var result = new JsonObject();
            if (submodelValue.SubmodelElements != null)
            {
                foreach (var element in submodelValue.SubmodelElements)
                {
                    var elementObject = ToJsonObject(element) as JsonObject;
                    foreach (var keyValue in elementObject)
                    {
                        var valueString = keyValue.Value.ToJsonString();
                        var valueNode = JsonSerializer.Deserialize<JsonNode>(valueString);
                        result[keyValue.Key] = valueNode;
                    }
                }
            }

            return result;
        }

        private static JsonObject Transform(SubmodelElementListValue submodelElementListValue)
        {
            var result = new JsonObject();

            var valueArray = new JsonArray();
            if (submodelElementListValue.Value != null)
            {
                foreach (var element in submodelElementListValue.Value)
                {
                    var elementObject = ToJsonObject(element) as JsonObject;
                    foreach (var keyValue in elementObject)
                    {
                        var valueString = keyValue.Value.ToJsonString();
                        var valueNode = JsonSerializer.Deserialize<JsonNode>(valueString);
                        valueArray.Add(valueNode);
                    }
                }
            }

            result[submodelElementListValue.IdShort] = valueArray;

            return result;
        }

        private static JsonObject Transform(SubmodelElementCollectionValue submodelElementCollectionValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            if (submodelElementCollectionValue.Value != null)
            {
                foreach (var element in submodelElementCollectionValue.Value)
                {
                    var elementObject = ToJsonObject(element) as JsonObject;
                    foreach (var keyValue in elementObject)
                    {
                        var valueString = keyValue.Value.ToJsonString();
                        var valueNode = JsonSerializer.Deserialize<JsonNode>(valueString);
                        valueObject[keyValue.Key] = valueNode;
                    }
                }
            }

            result[submodelElementCollectionValue.IdShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(RelationshipElementValue relationshipElementValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["first"] = Transform(relationshipElementValue.First);
            valueObject["second"] = Transform(relationshipElementValue.Second);

            result[relationshipElementValue.IdShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(ReferenceElementValue referenceElementValue)
        {
            var result = new JsonObject();

            result[referenceElementValue.IdShort] = Transform(referenceElementValue.Value);

            return result;
        }

        private static JsonObject Transform(RangeValue rangeValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["min"] = rangeValue.Min;
            valueObject["max"] = rangeValue.Max;

            result[rangeValue.IdShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(FileValue fileValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["contentType"] = fileValue.ContentType;
            valueObject["value"] = fileValue.Value;

            result[fileValue.IdShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(BlobValue blobValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["contentType"] = blobValue.ContentType;
            if (blobValue.Value != null)
            {
                valueObject["value"] = JsonValue.Create(System.Convert.ToBase64String(blobValue.Value));
            }

            result[blobValue.IdShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(BasicEventElementValue basicEventElementValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["observed"] = Transform(basicEventElementValue.Observed);

            result[basicEventElementValue.IdShort] = valueObject;

            return result;
        }

        private static JsonNode Transform(ReferenceDTO that)
        {
            var result = new JsonObject();

            result["type"] = Jsonization.Serialize.ReferenceTypesToJsonValue(
                that.Type);

            if (that.ReferredSemanticId != null)
            {
                result["referredSemanticId"] = Transform(
                    that.ReferredSemanticId);
            }

            var arrayKeys = new JsonArray();
            foreach (KeyDTO item in that.Keys)
            {
                arrayKeys.Add(Transform(item));
            }
            result["keys"] = arrayKeys;

            return result;
        }

        private static JsonNode Transform(KeyDTO that)
        {
            var result = new JsonObject();

            result["type"] = Jsonization.Serialize.KeyTypesToJsonValue(
                that.Type);

            result["value"] = JsonValue.Create(
                that.Value);

            return result;
        }

        private static JsonObject Transform(MultiLanguagePropertyValue multiLanguagePropertyValue)
        {
            var result = new JsonObject();

            var arrayLangStrings = new JsonArray();
            foreach (var item in multiLanguagePropertyValue.LangStrings)
            {
                var langNode = new JsonObject();
                langNode[item.Key] = item.Value;
                arrayLangStrings.Add(langNode);
            }

            result[multiLanguagePropertyValue.IdShort] = arrayLangStrings;
            return result;
        }

        private static JsonObject Transform(PropertyValue propertyValue)
        {
            var result = new JsonObject();
            result[propertyValue.IdShort] = propertyValue.Value;
            return result;
        }
    }
}
