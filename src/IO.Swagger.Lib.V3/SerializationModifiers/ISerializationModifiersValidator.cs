using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.SerializationModifiers;

/// <summary>
/// Provides methods for validating serialization modifiers of resources.
/// </summary>
public interface ISerializationModifiersValidator
{
    /// <summary>
    /// Validates the serialization modifiers of the specified resource.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="level">The level of the resource.</param>
    /// <param name="extent">The extent of the resource.</param>
    void Validate(object resource, LevelEnum level, ExtentEnum extent);
}