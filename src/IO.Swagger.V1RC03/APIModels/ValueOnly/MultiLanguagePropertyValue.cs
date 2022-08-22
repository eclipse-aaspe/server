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
    internal class MultiLanguagePropertyValue : IValue
    {
        public MultiLanguagePropertyValue(string idShort, LangStringSet value)
        {
            IdShort = idShort;
            Value = value;
        }

        public string IdShort { get; }
        public LangStringSet Value { get; }

        public void ToJsonObject(Stream body)
        {
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var writer = new Utf8JsonWriter(body, writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName(IdShort);
            writer.WriteStartArray();
            foreach (var langString in Value.LangStrings)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(langString.Language);
                writer.WriteStringValue(langString.Text);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
