using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal class FileValue : IValue
    {
        public FileValue(string idShort, string contentType, string value)
        {
            IdShort = idShort;
            ContentType = contentType;
            Value = value;
        }

        public string IdShort { get; }
        public string ContentType { get; }
        public string Value { get; }

        public void ToJsonObject(Stream body)
        {
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var writer = new Utf8JsonWriter(body, writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName(IdShort);
            writer.WriteStringValue(Value);
            writer.WriteStartObject();
            writer.WritePropertyName("contentType");
            writer.WriteStringValue(ContentType);
            writer.WritePropertyName("value");
            writer.WriteStringValue(Value);
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
