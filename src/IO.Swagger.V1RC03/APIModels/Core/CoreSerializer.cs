
using Org.BouncyCastle.Crypto.Tls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class CoreSerializer
    {
        private static readonly CoreTransformer _coreTransformer = new Core.CoreTransformer();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public static JsonObject ToJsonObject(IClass that, OutputModifierContext context)
        {
            return _coreTransformer.Transform(that, context);
        }
    }
}
