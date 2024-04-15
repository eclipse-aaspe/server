using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using AdminShellNS.Lib.V3.Models;
using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.Formatters;

/// <inheritdoc cref="IJsonSerializerStrategy"/>
public class JsonSerializerStrategy : IJsonSerializerStrategy
{
    /// <inheritdoc/>
    public bool CanSerialize(Type objectType, object obj)
    {
        return typeof(IClass).IsAssignableFrom(objectType) ||
               typeof(ValueOnlyPagedResult).IsAssignableFrom(objectType) ||
               typeof(IValueDTO).IsAssignableFrom(objectType) ||
               IsGenericListOfIClass(obj) ||
               IsGenericListOfIValueDto(obj) ||
               typeof(PagedResult).IsAssignableFrom(objectType);
    }

    /// <inheritdoc/>
    public void Serialize(Utf8JsonWriter writer, object obj, LevelEnum level, ExtentEnum extent)
    {
        switch (obj)
        {
            case IClass classObj:
                SerializationModifiersValidator.Validate(classObj, level, extent);
                Jsonization.Serialize.ToJsonObject(classObj).WriteTo(writer);
                break;
            case IList<IClass> genericListOfClass:
                WriteJsonArray(writer, genericListOfClass.Select(Jsonization.Serialize.ToJsonObject));
                break;
            case ValueOnlyPagedResult valuePagedResult:
                WriteValueOnlyPagedResult(writer, valuePagedResult);
                break;
            case IValueDTO valueDto:
                new ValueOnlyJsonSerializer().ToJsonObject(valueDto).WriteTo(writer);
                break;
            case IList<IValueDTO> genericListOfValueDto:
                WriteJsonArray(writer, genericListOfValueDto.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)));
                break;
            case PagedResult pagedResult:
                WritePagedResult(writer, pagedResult);
                break;
            default:
                throw new ArgumentException($"Type {obj.GetType()} is not supported for serialization.");
        }
    }

    private static bool IsGenericListOfIClass(object @object)
    {
        var oType = @object?.GetType();
        return oType?.IsGenericType == true &&
               oType.GetGenericTypeDefinition() == typeof(List<>) &&
               typeof(IClass).IsAssignableFrom(oType.GetGenericArguments()[0]);
    }

    private static bool IsGenericListOfIValueDto(object @object)
    {
        if (@object is not List<IValueDTO> list)
            return false;

        return list.Count > 0 && list[0] != null;
    }


    private static void WriteJsonArray(Utf8JsonWriter writer, IEnumerable<JsonNode> nodes)
    {
        var jsonArray = new JsonArray();
        foreach (var node in nodes)
        {
            jsonArray.Add(node);
        }

        jsonArray.WriteTo(writer);
    }

    private static void WriteValueOnlyPagedResult(Utf8JsonWriter writer, ValueOnlyPagedResult valuePagedResult)
    {
        var jsonArray = new JsonArray();
        var cursor = valuePagedResult.paging_metadata?.cursor;

        foreach (var json in valuePagedResult.result.Select(item => new ValueOnlyJsonSerializer().ToJsonObject(item)))
        {
            jsonArray.Add(json);
        }

        var jsonNode = new JsonObject
        {
            ["result"] = jsonArray
        };

        if (cursor != null)
        {
            jsonNode["paging_metadata"] = new JsonObject {["cursor"] = cursor};
        }

        jsonNode.WriteTo(writer);
    }

    private static void WritePagedResult(Utf8JsonWriter writer, PagedResult pagedResult)
    {
        var jsonArray = new JsonArray();
        var cursor = pagedResult.paging_metadata?.cursor;

        foreach (var json in pagedResult.result.Select(Jsonization.Serialize.ToJsonObject))
        {
            jsonArray.Add(json);
        }

        var jsonNode = new JsonObject
        {
            ["result"] = jsonArray
        };

        if (cursor != null)
        {
            jsonNode["paging_metadata"] = new JsonObject {["cursor"] = cursor};
        }

        jsonNode.WriteTo(writer);
    }
}