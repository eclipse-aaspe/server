
using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.V1RC03.Exceptions;
using IO.Swagger.V1RC03.Services;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.Services
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
                    _logger.LogDebug("Successfully decoded and deserialized the reference.");
                }
            }
            catch (JsonException ex)
            {
                throw new JsonDeserializationException(fieldName, ex.Message);
            }
            catch (Exception)
            {
                throw;
            }

            return output;
        }
    }
}
