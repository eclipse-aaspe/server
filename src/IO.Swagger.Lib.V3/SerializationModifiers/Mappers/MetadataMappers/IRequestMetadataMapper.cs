using DataTransferObjects.MetadataDTOs;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;

/// <summary>
/// Provides functionality to map request metadata.
/// </summary>
public interface IRequestMetadataMapper
{
    /// <summary>
    /// Maps a metadata DTO to an appropriate class.
    /// </summary>
    /// <param name="source">The metadata DTO to map.</param>
    /// <returns>The mapped class.</returns>
    IClass Map(IMetadataDTO source);
}