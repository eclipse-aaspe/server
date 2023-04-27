
using IO.Swagger.V1RC03.APIModels.ValueOnly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Metadata
{
    internal class MetadataSerializer
    {
        private static readonly MetadataTransfomer Transformer = new MetadataTransfomer();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public static JsonObject ToJsonObject(IClass that)
        {
            return Transformer.Transform(that);
        }
    }
}
