using AasCore.Aas3_0_RC02;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.Lib.V3.Formatters
{
    public class AasRequestFormatter : InputFormatter
    {
        public AasRequestFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        }

        public override bool CanRead(InputFormatterContext context)
        {
            if (typeof(IClass).IsAssignableFrom(context.ModelType))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            Type type = context.ModelType;
            var request = context.HttpContext.Request;
            object result = null;

            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(request.Body).Result;

            if (type == typeof(Submodel))
            {
                result = Jsonization.Deserialize.SubmodelFrom(node);
            }
            else if (type == typeof(AssetAdministrationShell))
            {
                result = Jsonization.Deserialize.AssetAdministrationShellFrom(node);
            }
            else if (type == typeof(SpecificAssetId))
            {
                result = Jsonization.Deserialize.SpecificAssetIdFrom(node);
            }
            else if (type == typeof(ISubmodelElement))
            {
                result = Jsonization.Deserialize.ISubmodelElementFrom(node);
            }
            else if (type == typeof(Reference))
            {
                result = Jsonization.Deserialize.ReferenceFrom(node);
            }
            else if (type == typeof(ConceptDescription))
            {
                result = Jsonization.Deserialize.ConceptDescriptionFrom(node);
            }
            else if (type == typeof(AssetInformation))
            {
                result = Jsonization.Deserialize.AssetInformationFrom(node);
            }
            return InputFormatterResult.SuccessAsync(result);

        }
    }
}
