using DataTransferObjects;

namespace IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

/// <summary>
/// Represents a contract for transforming an <see cref="IClass"/> instance into an <see cref="IDTO"/> instance.
/// </summary>
public interface IResponseValueTransformer
{
    /// <summary>
    /// Transforms the provided <paramref name="source"/> object into an <see cref="IDTO"/> instance.
    /// </summary>
    /// <param name="source">The source object to be transformed.</param>
    /// <returns>An instance of <see cref="IDTO"/> representing the transformed data.</returns>
    IDTO Transform(IClass source);
}