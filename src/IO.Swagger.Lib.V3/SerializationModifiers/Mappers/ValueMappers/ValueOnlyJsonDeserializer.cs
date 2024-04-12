using AasxServerStandardBib.Interfaces;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

public class ValueOnlyJsonDeserializer : IValueOnlyJsonDeserializer
{
    private readonly ISubmodelService _submodelService;
    private readonly IBase64UrlDecoderService _decoderService;

    public ValueOnlyJsonDeserializer(ISubmodelService submodelService, IBase64UrlDecoderService decoderService)
    {
        _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
        _decoderService = decoderService ?? throw new ArgumentNullException(nameof(submodelService));
    }

    public IValueDTO DeserializeSubmodelElementValue(JsonNode node, string encodedSubmodelIdentifier = null, string idShortPath = null)
    {
        if (node == null) throw new ArgumentNullException(nameof(node));

        IValueDTO output = null;

        if (node is not JsonObject jsonObject)
        {
            return output;
        }

        foreach (var (idShort, value) in jsonObject)
        {
            output = Deserialize(idShort, value, encodedSubmodelIdentifier, idShortPath);
        }

        return output;
    }

    private IValueDTO Deserialize(string idShort, JsonNode value, string encodedSubmodelIdentifier, string idShortPath)
    {
        IValueDTO output = null;
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

    private IValueDTO ParseJsonValueArray(string idShort, JsonArray valueArray, string encodedSubmodelIdentifier, string idShortPath)
    {
        //This is Multilingual Property or SMEList
        var decodedSubmodelId = _decoderService.Decode("submodelId", encodedSubmodelIdentifier);
        var element = _submodelService.GetSubmodelElementByPath(decodedSubmodelId, idShortPath);
        if (element != null)
        {
            return element switch
            {
                MultiLanguageProperty => CreateMultilanguagePropertyValue(idShort, valueArray),
                SubmodelElementList smeList => CreateSubmodelElementList(idShort, valueArray, smeList, encodedSubmodelIdentifier, idShortPath),
                _ => throw new JsonDeserializationException(idShort, "Element is neither MutlilanguageProperty nor SubmodelElementList")
            };
        }

        return null;
    }

    private IValueDTO CreateSubmodelElementList(string idShort, JsonArray valueArray, SubmodelElementList smeList, string encodedSubmodelIdentifier, string idShortPath)
    {
        var value = valueArray.Select(element => Deserialize(null, element, encodedSubmodelIdentifier, idShortPath))
            .Select(elementValueDTO => (ISubmodelElementValue) elementValueDTO).ToList();

        return new SubmodelElementListValue(idShort, value);
    }

    private IValueDTO CreateMultilanguagePropertyValue(string idShort, JsonArray valueArray)
    {
        var langStrings = new List<KeyValuePair<string, string>>();

        foreach (var item in valueArray)
        {
            if (item is not JsonObject jsonObject) continue;
            GetPropertyFromJsonObject(jsonObject, out string propertyName, out string propertyValue);
            langStrings.Add(new KeyValuePair<string, string>(propertyName, propertyValue));
        }

        return new MultiLanguagePropertyValue(idShort, langStrings);
    }

    private static void GetPropertyFromJsonObject(JsonObject jsonObject, out string propertyName, out string propertyValue)
    {
        propertyName = null;
        propertyValue = null;
        foreach (var item in jsonObject)
        {
            propertyName = item.Key;
            var jsonValue = item.Value as JsonValue;
            jsonValue.TryGetValue(out propertyValue);
        }
    }

    private IValueDTO ParseJsonValueObject(string idShort, JsonObject valueObject, string encodedSubmodelIdentifier = null, string idShortPath = null)
    {
        if (valueObject == null) throw new ArgumentNullException($"{nameof(valueObject)}");
        if (valueObject.ContainsKey("min") && valueObject.ContainsKey("max"))
        {
            //Range
            return CreateRangeValue(idShort, valueObject);
        }
        else if (valueObject.ContainsKey("contentType"))
        {
            //If it contains both contentType and Value, both File and Blob are possible. Hence, we need to retrieve actual elements from the server
            var decodedSubmodelIdentifier = _decoderService.Decode("submodelIdentifier", encodedSubmodelIdentifier);
            idShortPath ??= idShort;
            var submodelElement = _submodelService.GetSubmodelElementByPath(decodedSubmodelIdentifier, idShortPath);
            if (submodelElement != null)
            {
                return submodelElement switch
                {
                    File => CreateFileValue(idShort, valueObject),
                    Blob => CreateBlobValue(idShort, valueObject),
                    _ => throw new JsonDeserializationException(idShort, "Element is neither File nor Blob.")
                };
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

    private IValueDTO CreateSubmodelElementCollectionValue(string idShort, JsonObject valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        var submodelElements = new List<ISubmodelElementValue>();

        foreach (var item in valueObject)
        {
            var newNode = new JsonObject(new[] {KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString()))});
            if (idShortPath == null)
                idShortPath = idShort;
            var newIdShortPath = idShortPath + "." + item.Key;
            var submodelElement = DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier, newIdShortPath);
            submodelElements.Add((ISubmodelElementValue) submodelElement);
        }

        return new SubmodelElementCollectionValue(idShort, submodelElements);
    }

    private static IValueDTO CreateBasicEventElement(string idShort, JsonNode valueObject)
    {
        ReferenceDTO observed = null;
        if (valueObject["observed"] != null)
        {
            observed = JsonConvert.DeserializeObject<ReferenceDTO>(valueObject.ToString());
        }

        return new BasicEventElementValue(idShort, observed);
    }

    private IValueDTO CreateEntityValue(string idShort, JsonNode valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        string entityType = null;
        string globalAssetId = null;
        List<ISubmodelElementValue> statements = null;

        var entityTypeNode = valueObject["entityType"] as JsonValue;
        entityTypeNode?.TryGetValue(out entityType);

        var globalAssetIdNode = valueObject["globalAssetId"] as JsonValue;
        globalAssetIdNode?.TryGetValue(out globalAssetId);

        var statementsNode = valueObject["statements"] as JsonObject;
        if (statementsNode != null)
        {
            statements = (from item in statementsNode
                let newNode = new JsonObject(new[] {KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString()))})
                let newIdShortPath = idShortPath + "." + item.Key
                select DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier, newIdShortPath)
                into statement
                select (ISubmodelElementValue) statement).ToList();
        }

        return new EntityValue(idShort, (EntityType) Stringification.EntityTypeFromString(entityType), statements, globalAssetId);
    }

    private static IValueDTO CreateReferenceElementValue(string idShort, JsonObject valueObject)
    {
        var referenceDTO = JsonConvert.DeserializeObject<ReferenceDTO>(valueObject.ToString());
        return new ReferenceElementValue(idShort, referenceDTO);
    }

    private static IValueDTO CreateRelationshipElementValue(string idShort, JsonObject valueObject)
    {
        ReferenceDTO firstDTO = null, secondDTO = null;
        var firstNode = valueObject["first"];
        if (firstNode != null)
        {
            firstDTO = JsonConvert.DeserializeObject<ReferenceDTO>(firstNode.ToString());
        }

        var secondNode = valueObject["second"];
        if (secondNode != null)
        {
            secondDTO = JsonConvert.DeserializeObject<ReferenceDTO>(firstNode.ToString());
        }

        return new RelationshipElementValue(idShort, firstDTO, secondDTO);
    }

    private IValueDTO CreateAnnotedRelationshipElementValue(string idShort, JsonNode valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        ReferenceDTO firstDTO = null, secondDTO = null;
        var firstNode = valueObject["first"];
        if (firstNode != null)
        {
            firstDTO = JsonConvert.DeserializeObject<ReferenceDTO>(firstNode.ToString());
        }

        var secondNode = valueObject["second"];
        if (secondNode != null)
        {
            secondDTO = JsonConvert.DeserializeObject<ReferenceDTO>(firstNode.ToString());
        }

        var annotationsNode = valueObject["annotations"] as JsonArray;
        if (annotationsNode == null) return new AnnotatedRelationshipElementValue(idShort, firstDTO, secondDTO);
        var annotations = annotationsNode.Select(annotationNode => DeserializeSubmodelElementValue(annotationNode, encodedSubmodelIdentifier, idShortPath))
            .Select(annotation => (ISubmodelElementValue) annotation).ToList();
        return new AnnotatedRelationshipElementValue(idShort, firstDTO, secondDTO, annotations);
    }

    private static IValueDTO CreateBlobValue(string idShort, JsonObject valueObject)
    {
        string contentType = null;
        var contentTypeNode = valueObject["contentType"] as JsonValue;
        contentTypeNode?.TryGetValue(out contentType);

        if (valueObject["value"] is not JsonValue valueNode) return new BlobValue(idShort, contentType);
        valueNode.TryGetValue(out string value);
        return new BlobValue(idShort, contentType, System.Convert.FromBase64String(value));
    }

    private IValueDTO CreateFileValue(string idShort, JsonNode valueObject)
    {
        string contentType = null, value = null;
        var contentTypeNode = valueObject["contentType"] as JsonValue;
        contentTypeNode?.TryGetValue(out contentType);
        var valueNode = valueObject["value"] as JsonValue;
        valueNode?.TryGetValue(out value);
        return new FileValue(idShort, contentType, value);
    }

    private static IValueDTO CreateRangeValue(string idShort, JsonNode valueObject)
    {
        string min = null, max = null;
        var minNode = valueObject["min"] as JsonValue;
        minNode?.TryGetValue(out min);
        var maxNode = valueObject["max"] as JsonValue;
        maxNode?.TryGetValue(out max);

        return new RangeValue(idShort, min, max);
    }

    public SubmodelValue DeserializeSubmodelValue(JsonNode node, string encodedSubmodelIdentifier)
    {
        var jsonObject = node as JsonObject;
        var submodelElements = (from item in jsonObject
            select new JsonObject(new[] {KeyValuePair.Create<string, JsonNode>(item.Key, JsonNode.Parse(item.Value.ToJsonString()))})
            into newNode
            select DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier)
            into submodelElement
            select (ISubmodelElementValue) submodelElement).ToList();

        return new SubmodelValue(submodelElements);
    }
}