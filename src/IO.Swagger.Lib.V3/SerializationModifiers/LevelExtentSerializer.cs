/**
 * This class applies only Level and Extent modifiers. As per new specification, there are seperate APIs for the content modifiers.
 * 
 */
using AasCore.Aas3_0_RC02;
using System.Text.Json.Nodes;

namespace IO.Swagger.Lib.V3.SerializationModifiers
{
    public class LevelExtentSerializer
    {
        private static readonly LevelExtentTransformer _transformer = new LevelExtentTransformer();

        public static JsonObject ToJsonObject(IClass that, SerializationModifierContext context)
        {
            return _transformer.Transform(that, context);
        }
    }
}
