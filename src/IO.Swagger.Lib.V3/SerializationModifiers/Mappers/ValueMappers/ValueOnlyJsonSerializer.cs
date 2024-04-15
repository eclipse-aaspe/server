using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <inheritdoc cref="IValueOnlyJsonSerializer"/>
public class ValueOnlyJsonSerializer : IValueOnlyJsonSerializer
{
    public JsonNode ToJsonObject(IValueDTO valueDto)
    {
        return valueDto switch
        {
            null => throw new ArgumentNullException(nameof(valueDto)),
            PropertyValue propertyValue => Transform(propertyValue),
            MultiLanguagePropertyValue multiLanguagePropertyValue => Transform(multiLanguagePropertyValue),
            BasicEventElementValue basicEventElementValue => Transform(basicEventElementValue),
            BlobValue blobValue => Transform(blobValue),
            FileValue fileValue => Transform(fileValue),
            RangeValue rangeValue => Transform(rangeValue),
            ReferenceElementValue referenceElementValue => Transform(referenceElementValue),
            RelationshipElementValue relationshipElementValue => Transform(relationshipElementValue),
            SubmodelElementCollectionValue submodelElementCollectionValue => Transform(submodelElementCollectionValue),
            SubmodelElementListValue submodelElementListValue => Transform(submodelElementListValue),
            AnnotatedRelationshipElementValue annotatedRelEleValue => Transform(annotatedRelEleValue),
            EntityValue EntityValue => Transform(EntityValue),
            SubmodelValue submodelValue => Transform(submodelValue),
            _ => null
        };
    }

    private JsonNode Transform(EntityValue entityValue)
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

    private JsonNode Transform(AnnotatedRelationshipElementValue annotatedRelEleValue)
    {
        var result = new JsonObject();

        var annotations = new JsonArray();
        if (annotatedRelEleValue.annotations != null)
        {
            foreach (var elementObject in annotatedRelEleValue.annotations.Select(element => ToJsonObject(element)))
            {
                annotations.Add(elementObject);
            }
        }

        var valueObject = new JsonObject
        {
            ["first"] = Transform(annotatedRelEleValue.first),
            ["second"] = Transform(annotatedRelEleValue.second),
            ["annotations"] = annotations
        };

        result[annotatedRelEleValue.idShort] = valueObject;

        return result;
    }

    private JsonNode Transform(SubmodelValue submodelValue)
    {
        var result = new JsonObject();
        if (submodelValue.submodelElements == null)
        {
            return result;
        }

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

        return result;
    }

    private JsonObject Transform(SubmodelElementListValue submodelElementListValue)
    {
        var result = new JsonObject();

        var valueArray = new JsonArray();
        if (submodelElementListValue.value != null)
        {
            foreach (var valueNode in from element in submodelElementListValue.value
                     select ToJsonObject(element) as JsonObject
                     into elementObject
                     from keyValue in elementObject
                     select keyValue.Value.ToJsonString()
                     into valueString
                     select JsonSerializer.Deserialize<JsonNode>(valueString))
            {
                valueArray.Add(valueNode);
            }
        }

        result[submodelElementListValue.idShort] = valueArray;

        return result;
    }

    private JsonObject Transform(SubmodelElementCollectionValue submodelElementCollectionValue)
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

        var valueObject = new JsonObject
        {
            ["first"] = Transform(relationshipElementValue.first),
            ["second"] = Transform(relationshipElementValue.second)
        };

        result[relationshipElementValue.idShort] = valueObject;

        return result;
    }

    private static JsonObject Transform(ReferenceElementValue referenceElementValue)
    {
        var result = new JsonObject
        {
            [referenceElementValue.idShort] = Transform(referenceElementValue.value)
        };

        return result;
    }

    private static JsonObject Transform(RangeValue rangeValue)
    {
        var result = new JsonObject();

        var valueObject = new JsonObject
        {
            ["min"] = rangeValue.min,
            ["max"] = rangeValue.max
        };

        result[rangeValue.idShort] = valueObject;

        return result;
    }

    private static JsonObject Transform(FileValue fileValue)
    {
        var result = new JsonObject();

        var valueObject = new JsonObject
        {
            ["contentType"] = fileValue.contentType,
            ["value"] = fileValue.value
        };

        result[fileValue.idShort] = valueObject;

        return result;
    }

    private static JsonObject Transform(BlobValue blobValue)
    {
        var result = new JsonObject();

        var valueObject = new JsonObject
        {
            ["contentType"] = blobValue.contentType
        };
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

        var valueObject = new JsonObject
        {
            ["observed"] = Transform(basicEventElementValue.observed)
        };

        result[basicEventElementValue.idShort] = valueObject;

        return result;
    }

    private static JsonNode Transform(ReferenceDTO that)
    {
        var result = new JsonObject
        {
            ["type"] = Jsonization.Serialize.ReferenceTypesToJsonValue(
                that.type)
        };

        if (that.referredSemanticId != null)
        {
            result["referredSemanticId"] = Transform(
                that.referredSemanticId);
        }

        var arrayKeys = new JsonArray();
        foreach (var item in that.keys)
        {
            arrayKeys.Add(Transform(item));
        }

        result["keys"] = arrayKeys;

        return result;
    }

    private static JsonNode Transform(KeyDTO that)
    {
        var result = new JsonObject
        {
            ["type"] = Jsonization.Serialize.KeyTypesToJsonValue(
                that.type),
            ["value"] = JsonValue.Create(
                that.value)
        };

        return result;
    }

    private static JsonObject Transform(MultiLanguagePropertyValue multiLanguagePropertyValue)
    {
        var result = new JsonObject();

        var arrayLangStrings = new JsonArray();
        if (multiLanguagePropertyValue.langStrings != null)
            foreach (var langNode in multiLanguagePropertyValue.langStrings.Select(item => new JsonObject
                     {
                         [item.Key] = item.Value
                     }))
            {
                arrayLangStrings.Add(langNode);
            }

        result[multiLanguagePropertyValue.idShort] = arrayLangStrings;
        return result;
    }

    private static JsonObject Transform(PropertyValue propertyValue)
    {
        var result = new JsonObject
        {
            [propertyValue.idShort] = propertyValue.value
        };
        return result;
    }
}