
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class PathSerializer
    {
        private static readonly PathTransformer _pathTransformer = new PathTransformer();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public static List<string> ToIdShortPath(IClass that, OutputModifierContext context)
        {
            return _pathTransformer.Transform(that, context);
        }
    }
}
