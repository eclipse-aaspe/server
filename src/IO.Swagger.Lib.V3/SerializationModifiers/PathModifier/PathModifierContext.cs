using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.PathModifier
{
    public class PathModifierContext
    {
        private List<string>? idShortPaths;

        public string? ParentPath { get; internal set; }

        public List<string>? IdShortPaths { get => idShortPaths; set => idShortPaths = value; }

        public bool IsRoot { get; set; }

        public bool IsGetAllSmes { get; set; }

        public PathModifierContext(bool isGetAllSmes = false)
        {
            idShortPaths = new List<string>();
            IsRoot = true;
            IsGetAllSmes = isGetAllSmes;
        }
    }
}
