using System.Text.Json.Nodes;
using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <summary>
/// Interface for deserializing JSON nodes representing submodel elements and values.
/// </summary>
public interface IValueOnlyJsonDeserializer
{
    /// <summary>
    /// Deserialize a JSON node representing a submodel element value.
    /// </summary>
    /// <param name="node">The JSON node to deserialize.</param>
    /// <param name="encodedSubmodelIdentifier">The encoded identifier of the submodel, if applicable.</param>
    /// <param name="idShortPath">The path of the submodel element within the submodel, if applicable.</param>
    /// <returns>The deserialized submodel element value.</returns>
    IValueDTO DeserializeSubmodelElementValue(JsonNode node, string encodedSubmodelIdentifier = null, string idShortPath = null);

    /// <summary>
    /// Deserialize a JSON node representing a submodel value.
    /// </summary>
    /// <param name="node">The JSON node to deserialize.</param>
    /// <param name="encodedSubmodelIdentifier">The encoded identifier of the submodel.</param>
    /// <returns>The deserialized submodel value.</returns>
    SubmodelValue DeserializeSubmodelValue(JsonNode node, string encodedSubmodelIdentifier);
}