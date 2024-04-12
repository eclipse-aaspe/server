using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AasxServerStandardBib.Interfaces;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using Newtonsoft.Json;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers.JsonObjectParser;

/// <inheritdoc cref="IValueObjectParser"/>
public class ValueObjectParser : IValueObjectParser
{
    private readonly ISubmodelService _submodelService;
    private readonly IBase64UrlDecoderService _decoderService;

    /// <inheritdoc cref="IValueObjectParser"/>
    public ValueObjectParser(ISubmodelService submodelService, IBase64UrlDecoderService decoderService)
    {
        _submodelService = submodelService ?? throw new ArgumentNullException(nameof(submodelService));
        _decoderService = decoderService ?? throw new ArgumentNullException(nameof(submodelService));
    }

    /// <inheritdoc/>
    public IValueDTO Parse(string idShort, JsonObject valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        if (valueObject == null)
            throw new ArgumentNullException(nameof(valueObject));

        if (valueObject.ContainsKey("min") && valueObject.ContainsKey("max"))
            return CreateRangeValue(idShort, valueObject);

        if (valueObject.ContainsKey("contentType"))
            return CreateBlobValue(idShort, valueObject);

        if (valueObject.ContainsKey("annotations") && valueObject.ContainsKey("first") && valueObject.ContainsKey("second"))
            return CreateAnnotatedRelationshipElementValue(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);

        if (valueObject.ContainsKey("first") && valueObject.ContainsKey("second"))
            return CreateRelationshipElementValue(idShort, valueObject);

        if (valueObject.ContainsKey("type") && valueObject.ContainsKey("keys"))
            return CreateReferenceElementValue(idShort, valueObject);

        if (valueObject.ContainsKey("entityType"))
            return CreateEntityValue(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);

        return valueObject.ContainsKey("observed") 
            ? CreateBasicEventElement(idShort, valueObject)
            : CreateSubmodelElementCollectionValue(idShort, valueObject, encodedSubmodelIdentifier, idShortPath);
    }

    private static IValueDTO CreateRangeValue(string idShort, JsonNode valueObject)
    {
        var min = valueObject["min"]?.ToString();
        var max = valueObject["max"]?.ToString();
        return new RangeValue(idShort, min, max);
    }

    private static IValueDTO CreateBlobValue(string idShort, JsonNode valueObject)
    {
        if (valueObject["contentType"] is not JsonValue contentTypeNode)
            throw new JsonDeserializationException(idShort, "ContentType missing");

        var contentType = contentTypeNode.ToJsonString();
        if (valueObject["value"] is not JsonValue valueNode)
            throw new JsonDeserializationException(idShort, "Value missing");

        var value = Convert.FromBase64String(valueNode.ToString());
        return new BlobValue(idShort, contentType, value);
    }

    private IValueDTO CreateAnnotatedRelationshipElementValue(string idShort, JsonNode valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        var firstDto = GetReferenceDto(valueObject["first"]);
        var secondDto = GetReferenceDto(valueObject["second"]);

        var annotations = new List<ISubmodelElementValue>();
        if (valueObject["annotations"] is JsonArray annotationsNode)
        {
            annotations.AddRange(annotationsNode.Select(annotationNode => DeserializeSubmodelElementValue(annotationNode, encodedSubmodelIdentifier, idShortPath))
                .Select(annotation => annotation as ISubmodelElementValue));
        }

        return new AnnotatedRelationshipElementValue(idShort, firstDto, secondDto, annotations);
    }

    private static IValueDTO CreateRelationshipElementValue(string idShort, JsonNode valueObject)
    {
        var firstDto = GetReferenceDto(valueObject["first"]);
        var secondDto = GetReferenceDto(valueObject["second"]);
        return new RelationshipElementValue(idShort, firstDto, secondDto);
    }

    private static IValueDTO CreateReferenceElementValue(string idShort, JsonNode valueObject)
    {
        var referenceDto = JsonConvert.DeserializeObject<ReferenceDTO>(valueObject.ToString());
        return new ReferenceElementValue(idShort, referenceDto);
    }

    private IValueDTO CreateEntityValue(string idShort, JsonNode valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        var entityType = valueObject["entityType"]?.ToString();
        var globalAssetId = valueObject["globalAssetId"]?.ToString();

        var statements = new List<ISubmodelElementValue>();
        if (valueObject["statements"] is JsonObject statementsNode)
        {
            statements.AddRange(from item in statementsNode
                let newNode = new JsonObject(new[] {KeyValuePair.Create(item.Key, JsonNode.Parse(item.Value.ToJsonString()))})
                let newIdShortPath = $"{idShortPath}.{item.Key}"
                select DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier, newIdShortPath)
                into statement
                select statement as ISubmodelElementValue);
        }

        return new EntityValue(idShort, (EntityType) Stringification.EntityTypeFromString(entityType), statements, globalAssetId);
    }

    private static IValueDTO CreateBasicEventElement(string idShort, JsonNode valueObject)
    {
        var observed = GetReferenceDto(valueObject["observed"]);
        return new BasicEventElementValue(idShort, observed);
    }

    private IValueDTO CreateSubmodelElementCollectionValue(string idShort, JsonObject valueObject, string encodedSubmodelIdentifier, string idShortPath)
    {
        var submodelElements = new List<ISubmodelElementValue>();

        foreach (var item in valueObject)
        {
            var newNode = new JsonObject(new[] {KeyValuePair.Create(item.Key, JsonNode.Parse(item.Value.ToJsonString()))});
            idShortPath ??= idShort;
            var newIdShortPath = $"{idShortPath}.{item.Key}";
            var submodelElement = DeserializeSubmodelElementValue(newNode, encodedSubmodelIdentifier, newIdShortPath);
            submodelElements.Add(submodelElement as ISubmodelElementValue);
        }

        return new SubmodelElementCollectionValue(idShort, submodelElements);
    }

    private static ReferenceDTO GetReferenceDto(JsonNode node)
    {
        return node != null ? JsonConvert.DeserializeObject<ReferenceDTO>(node.ToString()) : null;
    }

    private IValueDTO DeserializeSubmodelElementValue(JsonNode node, string encodedSubmodelIdentifier, string idShortPath = null)
    {
        var parser = new ValueObjectParser(_submodelService, _decoderService);
        return parser.Parse(idShortPath, node as JsonObject, encodedSubmodelIdentifier, idShortPath);
    }
}