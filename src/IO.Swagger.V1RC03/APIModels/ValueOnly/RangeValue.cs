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
    internal class RangeValue : IValue
    {
        public RangeValue(string idShort, string min, string max)
        {
            IdShort = idShort;
            Min = min;
            Max = max;
        }

        public string IdShort { get; }
        public string Min { get; }
        public string Max { get; }

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
            writer.WritePropertyName("min");
            writer.WriteStringValue(Min);
            writer.WritePropertyName("max");
            writer.WriteStringValue(Max);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
