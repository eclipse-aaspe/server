using DataTransferObjects.ValueDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <summary>
/// Provides functionality to map response values.
/// </summary>
public interface IResponseValueMapper
{
    /// <summary>
    /// Maps a source class to a value DTO.
    /// </summary>
    /// <param name="source">The source class to map.</param>
    /// <returns>The mapped value DTO.</returns>
    public IValueDTO Map(IClass source);
}