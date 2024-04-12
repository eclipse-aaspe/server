using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

public interface IResponseValueTransformer
{
    IDTO Transform(IClass source);
}