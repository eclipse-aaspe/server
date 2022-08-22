using IO.Swagger.V1RC03.APIModels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aas = AasCore.Aas3_0_RC02;
using Nodes = System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal static class ValueOnlySerializer
    {
        //private static readonly ValueOnlyTransformer Transformer = new ValueOnlyTransformer();
        private static readonly ValueTransformer Transformer = new ValueTransformer();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public static Nodes.JsonObject ToJsonObject(Aas.IClass that, OutputModifierContext context)
        {
            return Transformer.Transform(that, context);
        }


    }
}
