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
    internal class SubmodelValue : IValue
    {

        public SubmodelValue(List<IValue> submodelElements)
        {
            this.SubmodelElements = submodelElements;
        }

        public List<IValue> SubmodelElements { get; }

        public void ToJsonObject(Stream body)
        {
            var writerOptions = new JsonWriterOptions
            {
                Indented = true
            };

            var writer = new Utf8JsonWriter(body, writerOptions);
            writer.WriteStartObject();
            foreach (var submodelElement in SubmodelElements)
            {
                submodelElement.ToJsonObject(body);
            }
            writer.WriteEndObject();
            writer.FlushAsync().GetAwaiter().GetResult();
        }
    }
}
