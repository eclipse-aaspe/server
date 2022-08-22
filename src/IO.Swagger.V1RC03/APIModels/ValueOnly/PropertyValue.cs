using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal class PropertyValue : IValue
    {
        public string IdShort { get; set; }
        public string Value { get; set; }

        public PropertyValue(string idShort, string value)
        {
            IdShort = idShort;
            Value = value;
        }

        public void ToJsonObject(System.IO.Stream body)
        {
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var writer = new Utf8JsonWriter(body, writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName(IdShort);
            writer.WriteStringValue(Value);
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
