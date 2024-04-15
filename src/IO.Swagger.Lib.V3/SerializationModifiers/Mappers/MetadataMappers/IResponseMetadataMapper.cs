using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;

/// <summary>
/// Represents a transformer for response metadata.
/// </summary>
public interface IResponseMetadataMapper
{
    /// <summary>
    /// Transforms a source class to a DTO representing metadata.
    /// </summary>
    /// <param name="source">The source class to transform.</param>
    /// <returns>The transformed DTO representing metadata.</returns>
    public IDTO Map(IClass source);
}