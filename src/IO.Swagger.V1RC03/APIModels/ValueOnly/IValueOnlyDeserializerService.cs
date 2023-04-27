
using System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    public interface IValueOnlyDeserializerService
    {
        ISubmodelElement DeserializeISubmodelElement(JsonNode jsonNode, string encodedSubmodelIdentifier = null, string idShortPath = null);
        object DeserializeSubmodel(JsonNode node);
    }
}