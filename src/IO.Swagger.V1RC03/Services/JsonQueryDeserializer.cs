using AasCore.Aas3_0_RC02;
using AasxServerStandardBib.Exceptions;
using IO.Swagger.V1RC03.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.Services
{
    public class JsonQueryDeserializer : IJsonQueryDeserializer
    {
        private readonly IAppLogger<JsonQueryDeserializer> _logger;
        private readonly IBase64UrlDecoderService _decoderService;

        public JsonQueryDeserializer(IAppLogger<JsonQueryDeserializer> logger, IBase64UrlDecoderService decoderService)
        {
            _logger = logger;
            _decoderService = decoderService;
        }

        public Reference DeserializeReference(string fieldName, string referenceString)
        {
            Reference output = null;
            try
            {
                if (!string.IsNullOrEmpty(referenceString))
                {
                    var decodedString = _decoderService.Decode(fieldName, referenceString);
                    MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(decodedString));
                    JsonNode node = JsonSerializer.DeserializeAsync<JsonNode>(mStrm).Result;
                    output = Jsonization.Deserialize.ReferenceFrom(node);
                }
            }
            catch (JsonException ex)
            {
                throw new JsonDeserializationException(fieldName, ex.Message);
            }
            catch (Exception ex)
            {
                throw;
            }

            return output;
        }
    }
}
