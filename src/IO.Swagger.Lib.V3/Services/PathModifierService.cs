using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.PathModifier;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services
{
    public class PathModifierService : IPathModifierService
    {
        private static readonly PathTransformer _pathTransformer = new PathTransformer();

        /// <summary>
        /// Serialize an instance of the meta-model into a JSON object.
        /// </summary>
        public List<string> ToIdShortPath(IClass that)
        {
            var context = new PathModifierContext();
            return _pathTransformer.Transform(that, context);
        }

        public List<List<string>> ToIdShortPath(List<ISubmodel> submodelList)
        {
            var output = new List<List<string>>();

            foreach (var submodel in submodelList)
            {
                var context = new PathModifierContext();
                var path = _pathTransformer.Transform(submodel, context);
                output.Add(path);
            }

            return output;
        }

        public List<List<string>> ToIdShortPath(List<ISubmodelElement> submodelElementList)
        {
            var output = new List<List<string>>();

            foreach (var submodelElement in submodelElementList)
            {
                var context = new PathModifierContext();
                var path = _pathTransformer.Transform(submodelElement, context);
                output.Add(path);
            }

            return output;
        }
    }
}
