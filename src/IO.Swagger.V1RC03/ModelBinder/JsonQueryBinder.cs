using AasxServerStandardBib.Logging;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.ModelBinder
{
    public class JsonQueryBinder : IModelBinder
    {
        private readonly IAppLogger<JsonQueryBinder> _logger;

        public JsonQueryBinder(IAppLogger<JsonQueryBinder> logger)
        {
            _logger = logger;
        }
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var value = bindingContext.ValueProvider.GetValue(bindingContext.FieldName).FirstValue;
            if (value == null)
            {
                return Task.CompletedTask;
            }

            try
            {
                var decodedString = Base64UrlEncoder.Decode(value);
                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(decodedString));
                JsonNode node = JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                var reqIsCaseOf = AasCore.Aas3_0_RC02.Jsonization.Deserialize.ReferenceFrom(node);
                bindingContext.Result = ModelBindingResult.Success(reqIsCaseOf);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to bind query parameter {bindingContext.FieldName}");
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
}
