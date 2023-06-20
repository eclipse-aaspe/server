using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.PathModifier
{
    public class PathModifierContext
    {
        private List<string> idShortPaths;

        public string ParentPath { get; internal set; }

        public List<string> IdShortPaths { get => idShortPaths; set => idShortPaths = value; }

        public PathModifierContext()
        {
            idShortPaths = new List<string>();
        }
    }
}
