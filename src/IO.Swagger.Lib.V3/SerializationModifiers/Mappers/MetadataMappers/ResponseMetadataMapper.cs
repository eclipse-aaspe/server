using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers
{
    /// <inheritdoc cref="IResponseMetadataMapper"/>
    public class ResponseMetadataMapper : IResponseMetadataMapper
    {
        private static ResponseMetadataTransformer _transformer = new();

        public IDTO Map(IClass source)
        {
            return _transformer.Transform(source);
        }
    }
}