using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers
{
    public class ResponseValueMapper
    {
        private static ResponseValueTransformer Transformer = new ResponseValueTransformer();

        public static IValueDTO Map(IClass source)
        {
            return (IValueDTO)Transformer.Transform(source);
        }
    }
}
