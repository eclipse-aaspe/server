using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent
{
    public class LevelExtentModifierContext
    {
        public LevelEnum Level { get; set; }

        public ExtentEnum Extent { get; set; }

        public bool IncludeChildren { get; set; }

        public bool IsRoot { get; set; }

        public LevelExtentModifierContext(LevelEnum level, ExtentEnum extent)
        {
            Level = level;
            Extent = extent;
            IsRoot = true;
            IncludeChildren = true;
        }
    }
}
