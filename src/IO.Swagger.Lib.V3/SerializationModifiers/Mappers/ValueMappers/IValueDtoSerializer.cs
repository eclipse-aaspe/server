using DataTransferObjects.CommonDTOs;
using System.Text.Json.Nodes;
using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <summary>
/// Defines methods to serialize <see cref="IValueDTO"/> instances into JSON objects.
/// </summary>
public interface IValueOnlyJsonSerializer
{
    /// <summary>
    /// Serializes the provided <paramref name="valueDto"/> into a <see cref="JsonNode"/>.
    /// </summary>
    /// <param name="valueDto">The <see cref="IValueDTO"/> to serialize.</param>
    /// <returns>A <see cref="JsonNode"/> representing the serialized <paramref name="valueDto"/>.</returns>
    JsonNode ToJsonObject(IValueDTO valueDto);
}