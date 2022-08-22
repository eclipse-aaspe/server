using AasCore.Aas3_0_RC02;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal class RelationshipElementValue : IValue
    {
        public RelationshipElementValue(string idShort, Reference first, Reference second)
        {
            IdShort = idShort;
            First = first;
            Second = second;
        }

        public string IdShort { get; }
        public Reference First { get; }
        public Reference Second { get; }


        public void ToJsonObject(Stream body)
        {
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };
            var writer = new Utf8JsonWriter(body, writerOptions);
            writer.WriteStartObject();
            writer.WritePropertyName(IdShort);
            //first
            writer.WriteStartObject();
            writer.WritePropertyName("first");
            writer.WriteStartObject();
            writer.WritePropertyName("modelType");
            writer.WriteStringValue(First.Type.ToString());
            writer.WritePropertyName("keys");
            writer.WriteStartArray();
            foreach (var key in First.Keys)
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
            //Second
            writer.WriteStartObject();
            writer.WritePropertyName("second");
            writer.WriteStartObject();
            writer.WritePropertyName("modelType");
            writer.WriteStringValue(First.Type.ToString());
            writer.WritePropertyName("keys");
            writer.WriteStartArray();
            foreach (var key in First.Keys)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("type");
                writer.WriteStringValue(key.Type.ToString());
                writer.WritePropertyName("value");
                writer.WriteStringValue(key.Value);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            //end
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
