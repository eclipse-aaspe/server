using System.Text.Json.Nodes;
using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

public interface IValueOnlyJsonDeserializer
{
    IValueDTO DeserializeSubmodelElementValue(JsonNode node, string encodedSubmodelIdentifier = null, string idShortPath = null);
    SubmodelValue DeserializeSubmodelValue(JsonNode node, string encodedSubmodelIdentifier);
}