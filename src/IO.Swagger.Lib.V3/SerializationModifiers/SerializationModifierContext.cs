using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.SerializationModifiers
{
    public class SerializationModifierContext
    {
        public LevelEnum Level { get; set; }

        public ExtentEnum Extent { get; set; }

        public SerializationModifierContext(LevelEnum level, ExtentEnum extent)
        {
            Level = level;
            Extent = extent;
        }
    }
}
