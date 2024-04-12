using System.Text.Json.Nodes;
using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers.JsonObjectParser;

/// <summary>
/// Interface for parsing JSON objects into value data transfer objects (DTOs).
/// </summary>
public interface IValueObjectParser
{
    /// <summary>
    /// Parses a JSON object and returns the corresponding value DTO.
    /// </summary>
    /// <param name="idShort">The ID short of the value DTO.</param>
    /// <param name="valueObject">The JSON object to parse.</param>
    /// <param name="encodedSubmodelIdentifier">The encoded submodel identifier.</param>
    /// <param name="idShortPath">The ID short path.</param>
    /// <returns>The parsed value DTO.</returns>
    IValueDTO Parse(string idShort, JsonObject valueObject, string encodedSubmodelIdentifier, string idShortPath);
}
