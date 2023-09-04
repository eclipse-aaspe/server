using IO.Swagger.Registry.Lib.V3.Models;
using System.Text.Json.Nodes;

namespace IO.Swagger.Registry.Lib.V3.Serializers
{
    public static class DescriptorDeserializer
    {
        public static AssetAdministrationShellDescriptor AssetAdministrationShellDescriptorFrom(JsonNode node)
        {
            AssetAdministrationShellDescriptor? result = DescriptorDeserializeImplementation.AssetAdministrationShellDescriptorFrom(
                    node,
                    out Reporting.Error? error);
            if (error != null)
            {
                throw new Jsonization.Exception(
                    Reporting.GenerateJsonPath(error.PathSegments),
                    error.Cause);
            }
            return result
                ?? throw new System.InvalidOperationException(
                    "Unexpected output null when error is null");
        }
    }
}
