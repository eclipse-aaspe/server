using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <inheritdoc cref="IResponseValueMapper"/>
public class ResponseValueMapper : IResponseValueMapper
{
    private static readonly IResponseValueTransformer Transformer = new ResponseValueTransformer();

    public IValueDTO Map(IClass source)
    {
        return (IValueDTO) Transformer.Transform(source);
    }
}