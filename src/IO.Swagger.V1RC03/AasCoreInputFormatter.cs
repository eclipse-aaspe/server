using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;
using AasCore.Aas3_0_RC02;
using System.Collections.Generic;
using System;
using System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03
{
    public class AasCoreInputFormatter : InputFormatter
    {

        public AasCoreInputFormatter()
        {
            this.SupportedMediaTypes.Clear();
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));

        }

        public override bool CanRead(InputFormatterContext context)
        {
            if (typeof(IClass).IsAssignableFrom(context.ModelType)){
                return true;
            }
            else
            {
                return false;
            }
        }


        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            System.Type type = context.ModelType;
            var request = context.HttpContext.Request;
            MediaTypeHeaderValue requestContentType = null;
            MediaTypeHeaderValue.TryParse(request.ContentType, out requestContentType);

            object result = null;

            JsonNode node = System.Text.Json.JsonSerializer.DeserializeAsync<JsonNode>(request.Body).Result;

            if (type == typeof(Submodel))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.SubmodelFrom(node);
            }
            else if (type == typeof(AssetAdministrationShell))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.AssetAdministrationShellFrom(node);
            }
            else if (type == typeof(SpecificAssetId))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.SpecificAssetIdFrom(node);
            }
            else if (type == typeof(AasCore.Aas3_0_RC02.ISubmodelElement))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.ISubmodelElementFrom(node);
            }
            else if (type == typeof(AasCore.Aas3_0_RC02.Reference))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.ReferenceFrom(node);
            }
            else if (type == typeof(AasCore.Aas3_0_RC02.ConceptDescription))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.ConceptDescriptionFrom(node);
            }
            else if (type == typeof(AasCore.Aas3_0_RC02.AssetInformation))
            {
                result = AasCore.Aas3_0_RC02.Jsonization.Deserialize.AssetInformationFrom(node);
            }
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}