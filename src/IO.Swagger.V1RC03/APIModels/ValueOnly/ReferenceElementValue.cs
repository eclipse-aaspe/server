using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal class ReferenceElementValue : IValue
    {
        public ReferenceElementValue(string idShort, Reference value)
        {
            IdShort = idShort;
            Value = value;
        }

        public string IdShort { get; }
        public Reference Value { get; }

        public void ToJsonObject(Stream body)
        {
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var writer = new Utf8JsonWriter(body, writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName(IdShort);
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            writer.WriteStringValue(Value.Type.ToString());
            writer.WritePropertyName("keys");
            writer.WriteStartArray();
            foreach (var key in Value.Keys)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteStringValue(key.Type.ToString());
                writer.WritePropertyName("value");
                writer.WriteStringValue(key.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
