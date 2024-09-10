using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.PathModifier
{
    public class PathModifierContext
    {
        private List<string>? idShortPaths;

        public string? ParentPath { get; internal set; }

        public List<string>? IdShortPaths { get => idShortPaths; set => idShortPaths = value; }

        public bool IsRoot { get; set; }

        public PathModifierContext()
        {
            idShortPaths = new List<string>();
            IsRoot = true;
        }
    }
}
