using DataTransferObjects;
using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <summary>
/// Provides functionality to map request values.
/// </summary>
public interface IRequestValueMapper
{
    /// <summary>
    /// Maps a value DTO to an appropriate class.
    /// </summary>
    /// <param name="source">The value DTO to map.</param>
    /// <returns>The mapped class.</returns>
    IClass Map(IValueDTO source);

    IDTO Map(IClass source);
}