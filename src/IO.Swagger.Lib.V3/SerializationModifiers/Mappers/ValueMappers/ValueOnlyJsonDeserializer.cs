using AasxServerStandardBib.Interfaces;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers.JsonObjectParser;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <inheritdoc cref="IValueOnlyJsonDeserializer"/>
public class ValueOnlyJsonDeserializer : IValueOnlyJsonDeserializer
{
    private readonly ISubmodelService _submodelService;
    private readonly IBase64UrlDecoderService _decoderService;
    private readonly IValueObjectParser _valueObjectParser;

    /// <inheritdoc cref="IValueOnlyJsonDeserializer" />
    public ValueOnlyJsonDeserializer(ISubmodelService submodelService, IBase64UrlDecoderService decoderService, IValueObjectParser valueObjectParser)
    {
        _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
        _decoderService = decoderService ?? throw new ArgumentNullException(nameof(submodelService));
        _valueObjectParser = valueObjectParser ?? throw new ArgumentNullException(nameof(valueObjectParser));
    }

    /// <inheritdoc/>
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
        return value switch
        {
            JsonValue jsonValue => new PropertyValue(idShort, jsonValue.ToJsonString()),
            JsonObject valueObject => ParseJsonValueObject(idShort, valueObject, encodedSubmodelIdentifier, idShortPath),
            JsonArray valueArray => ParseJsonValueArray(idShort, valueArray, encodedSubmodelIdentifier, idShortPath),
            _ => throw new InvalidOperationException()
        };
    }

    private IValueDTO ParseJsonValueArray(string idShort, JsonArray valueArray, string encodedSubmodelIdentifier, string idShortPath)
    {
        var decodedSubmodelId = _decoderService.Decode("submodelId", encodedSubmodelIdentifier);
        var element = _submodelService.GetSubmodelElementByPath(decodedSubmodelId, idShortPath);

        if (element == null)
        {
            return null;
        }

        return element switch
        {
            MultiLanguageProperty => CreateMultilanguagePropertyValue(idShort, valueArray),
            SubmodelElementList smeList => CreateSubmodelElementList(idShort, valueArray, smeList, encodedSubmodelIdentifier, idShortPath),
            _ => throw new JsonDeserializationException(idShort, "Element is neither MultilanguageProperty nor SubmodelElementList")
        };
    }

    private IValueDTO CreateSubmodelElementList(string idShort, JsonArray valueArray, SubmodelElementList smeList, string encodedSubmodelIdentifier, string idShortPath)
    {
        var value = valueArray.Select(element => Deserialize(null, element, encodedSubmodelIdentifier, idShortPath))
            .Select(elementValueDTO => (ISubmodelElementValue) elementValueDTO)
            .ToList();

        return new SubmodelElementListValue(idShort, value);
    }

    private static IValueDTO CreateMultilanguagePropertyValue(string idShort, JsonArray valueArray)
    {
        var langStrings = valueArray.Select(item =>
        {
            if (item is not JsonObject jsonObject)
            {
                return default;
            }

            GetPropertyFromJsonObject(jsonObject, out var propertyName, out var propertyValue);
            return new KeyValuePair<string, string>(propertyName, propertyValue);
        }).ToList();

        return new MultiLanguagePropertyValue(idShort, langStrings);
    }

    private static void GetPropertyFromJsonObject(JsonObject jsonObject, out string propertyName, out string propertyValue)
    {
        propertyName = null;
        propertyValue = null;

        foreach (var item in jsonObject)
        {
            propertyName = item.Key;
            if (item.Value is JsonValue jsonValue)
            {
                propertyValue = jsonValue.ToJsonString();
            }
        }
    }

    private IValueDTO ParseJsonValueObject(string idShort, JsonObject valueObject, string encodedSubmodelIdentifier = null, string idShortPath = null)
    {
        if (valueObject == null) throw new ArgumentNullException($"{nameof(valueObject)}");

        return _valueObjectParser.Parse(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);
    }

    /// <inheritdoc/>
    public SubmodelValue DeserializeSubmodelValue(JsonNode node, string encodedSubmodelIdentifier)
    {
        if (node is not JsonObject jsonObject) throw new ArgumentException("Node must be a JsonObject", nameof(node));

        var submodelElements = jsonObject.Select(item =>
        {
            var newNode = new JsonObject(new[] {KeyValuePair.Create(item.Key, JsonNode.Parse(item.Value.ToJsonString()))});
            return DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier);
        }).OfType<ISubmodelElementValue>().ToList();

        return new SubmodelValue(submodelElements);
    }
}