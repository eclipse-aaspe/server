using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    public static class ValueOnlyJsonSerializer
    {
        internal static JsonNode ToJsonObject(IValueDTO that)
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
            if (entityValue.statements != null)
            {
                foreach (var element in entityValue.statements)
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
            valueObject["entityType"] = Jsonization.Serialize.EntityTypeToJsonValue(entityValue.entityType);
            valueObject["globalAssetId"] = entityValue.globalAssetId;

            result[entityValue.idShort] = valueObject;

            return result;
        }

        private static JsonNode Transform(AnnotatedRelationshipElementValue annotatedRelEleValue)
        {
            var result = new JsonObject();

            var annotations = new JsonArray();
            if (annotatedRelEleValue.annotations != null)
            {
                foreach (var element in annotatedRelEleValue.annotations)
                {
                    var elementObject = ToJsonObject(element);
                    annotations.Add(elementObject);
                }
            }

            var valueObject = new JsonObject();
            valueObject["first"] = Transform(annotatedRelEleValue.first);
            valueObject["second"] = Transform(annotatedRelEleValue.second);
            valueObject["annotations"] = annotations;

            result[annotatedRelEleValue.idShort] = valueObject;

            return result;
        }

        private static JsonNode Transform(SubmodelValue submodelValue)
        {
            var result = new JsonObject();
            if (submodelValue.submodelElements != null)
            {
                foreach (var element in submodelValue.submodelElements)
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
            if (submodelElementListValue.value != null)
            {
                foreach (var element in submodelElementListValue.value)
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

            result[submodelElementListValue.idShort] = valueArray;

            return result;
        }

        private static JsonObject Transform(SubmodelElementCollectionValue submodelElementCollectionValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            if (submodelElementCollectionValue.value != null)
            {
                foreach (var element in submodelElementCollectionValue.value)
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

            result[submodelElementCollectionValue.idShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(RelationshipElementValue relationshipElementValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["first"] = Transform(relationshipElementValue.first);
            valueObject["second"] = Transform(relationshipElementValue.second);

            result[relationshipElementValue.idShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(ReferenceElementValue referenceElementValue)
        {
            var result = new JsonObject();

            result[referenceElementValue.idShort] = Transform(referenceElementValue.value);

            return result;
        }

        private static JsonObject Transform(RangeValue rangeValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["min"] = rangeValue.min;
            valueObject["max"] = rangeValue.max;

            result[rangeValue.idShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(FileValue fileValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["contentType"] = fileValue.contentType;
            valueObject["value"] = fileValue.value;

            result[fileValue.idShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(BlobValue blobValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["contentType"] = blobValue.contentType;
            if (blobValue.value != null)
            {
                valueObject["value"] = JsonValue.Create(System.Convert.ToBase64String(blobValue.value));
            }

            result[blobValue.idShort] = valueObject;

            return result;
        }

        private static JsonObject Transform(BasicEventElementValue basicEventElementValue)
        {
            var result = new JsonObject();

            var valueObject = new JsonObject();
            valueObject["observed"] = Transform(basicEventElementValue.observed);

            result[basicEventElementValue.idShort] = valueObject;

            return result;
        }

        private static JsonNode Transform(ReferenceDTO that)
        {
            var result = new JsonObject();

            result["type"] = Jsonization.Serialize.ReferenceTypesToJsonValue(
                that.type);

            if (that.referredSemanticId != null)
            {
                result["referredSemanticId"] = Transform(
                    that.referredSemanticId);
            }

            var arrayKeys = new JsonArray();
            foreach (KeyDTO item in that.keys)
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
                that.type);

            result["value"] = JsonValue.Create(
                that.value);

            return result;
        }

        private static JsonObject Transform(MultiLanguagePropertyValue multiLanguagePropertyValue)
        {
            var result = new JsonObject();

            var arrayLangStrings = new JsonArray();
            foreach (var item in multiLanguagePropertyValue.langStrings)
            {
                var langNode = new JsonObject();
                langNode[item.Key] = item.Value;
                arrayLangStrings.Add(langNode);
            }

            result[multiLanguagePropertyValue.idShort] = arrayLangStrings;
            return result;
        }

        private static JsonObject Transform(PropertyValue propertyValue)
        {
            var result = new JsonObject();
            result[propertyValue.idShort] = propertyValue.value;
            return result;
        }
    }
}
