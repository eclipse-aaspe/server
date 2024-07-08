using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;

public class ResponseMetadataMapper
{
    private static ResponseMetadataTransformer Transformer = new();
    public static IDTO Map(IClass? source) => Transformer.Transform(source);
}