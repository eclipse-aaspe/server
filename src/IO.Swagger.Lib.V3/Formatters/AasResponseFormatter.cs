using IO.Swagger.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Formatters;

/// <summary>
/// A formatter for formatting responses in the Asset Administration shell format.
/// </summary>
public class AasResponseFormatter : OutputFormatter
{
    private readonly IJsonSerializerStrategy _jsonSerializerStrategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="AasResponseFormatter"/> class.
    /// </summary>
    public AasResponseFormatter(IJsonSerializerStrategy jsonSerializerStrategy)
    {
        _jsonSerializerStrategy = jsonSerializerStrategy;
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
    }

    /// <inheritdoc />
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        return _jsonSerializerStrategy.CanSerialize(context.ObjectType, context.Object);
    }

    /// <inheritdoc />
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        var response = context.HttpContext.Response;
        var (level, extent) = GetSerializationModifiersFromRequest(context.HttpContext.Request);

        await using var writer = new Utf8JsonWriter(response.Body);
        _jsonSerializerStrategy.Serialize(writer, context.Object, level, extent);
        await writer.FlushAsync();
    }

    private static (LevelEnum, ExtentEnum) GetSerializationModifiersFromRequest(HttpRequest request)
    {
        request.Query.TryGetValue("level", out var levelValues);
        var level = levelValues.Any() ? Enum.TryParse(levelValues.First(), out LevelEnum parsedLevel) ? parsedLevel : LevelEnum.Deep : LevelEnum.Deep;

        request.Query.TryGetValue("extent", out var extendValues);
        var extent = extendValues.Any()
            ? Enum.TryParse(extendValues.First(), out ExtentEnum parsedExtent) ? parsedExtent : ExtentEnum.WithoutBlobValue
            : ExtentEnum.WithoutBlobValue;

        return (level, extent);
    }
}