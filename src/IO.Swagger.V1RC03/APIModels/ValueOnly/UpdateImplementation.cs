
using IO.Swagger.V1RC03.APIModels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal static class UpdateImplementation
    {
        private static readonly UpdateTransformer _updateTransformer = new();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public static void Update(IClass that, IClass source, OutputModifierContext context)
        {
            _updateTransformer.Transform(that, new UpdateContext(context, source));
        }
    }

    public class UpdateContext
    {
        OutputModifierContext _outputModifierContext;

        IClass _source;

        public UpdateContext(OutputModifierContext outputModifierContext, IClass source)
        {
            _outputModifierContext = outputModifierContext;
            _source = source;
        }

        public OutputModifierContext OutputModifierContext { get => _outputModifierContext; set => _outputModifierContext = value; }
        public IClass Source { get => _source; set => _source = value; }
    }
}
