using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers
{
    public class ResponseMetadataMapper
    {
        private static ResponseMetadataTransformer Transformer = new ResponseMetadataTransformer();

        public static IDTO Map(IClass source)
        {
            return Transformer.Transform(source);
        }
    }
}
