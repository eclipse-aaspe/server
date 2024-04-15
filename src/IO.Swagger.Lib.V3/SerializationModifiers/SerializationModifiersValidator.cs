using DataTransferObjects.MetadataDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Models;
using System;

namespace IO.Swagger.Lib.V3.SerializationModifiers;

/// <summary>
/// Provides methods for validating serialization modifiers of resources.
/// </summary>
public class SerializationModifiersValidator
{
    /// <summary>
    /// Validates the serialization modifiers of the specified resource.
    /// </summary>
    /// <param name="resource">The resource to validate.</param>
    /// <param name="level">The level of the resource.</param>
    /// <param name="extent">The extent of the resource.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="resource"/> is null.</exception>
    public static void Validate(object resource, LevelEnum level, ExtentEnum extent)
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }

        if (level == LevelEnum.Core || extent == ExtentEnum.WithBlobValue)
        {
            ValidateForException(resource, level, extent);
        }
    }

    private static void ValidateForException(object resource, LevelEnum level, ExtentEnum extent)
    {
        var resourceType = resource.GetType();

        if (IsUnsupportedType(resourceType))
        {
            throw new InvalidSerializationModifierException(GetModifierString(level, extent), resourceType.Name);
        }
    }

    private static bool IsUnsupportedType(Type resourceType)
    {
        return resourceType switch
        {
            not null when IsBasicType(resourceType) => true,
            not null when IsBlobType(resourceType) => true,
            not null when IsSubmodelType(resourceType) => true,
            _ => false
        };
    }

    private static bool IsBasicType(Type resourceType)
    {
        return resourceType == typeof(BasicEventElementMetadata) ||
               resourceType == typeof(BasicEventElementValue) ||
               resourceType == typeof(BasicEventElement) ||
               resourceType == typeof(Capability) ||
               resourceType == typeof(Operation);
    }

    private static bool IsBlobType(Type resourceType)
    {
        return resourceType == typeof(BlobMetadata) ||
               resourceType == typeof(BlobValue) ||
               resourceType == typeof(Blob);
    }

    private static bool IsSubmodelType(Type resourceType)
    {
        return resourceType == typeof(ISubmodelElementMetadata) ||
               resourceType == typeof(ISubmodelElementValue) ||
               resourceType == typeof(IDataElement) ||
               resourceType == typeof(SubmodelElementListMetadata) ||
               resourceType == typeof(SubmodelElementListValue) ||
               resourceType == typeof(SubmodelElementCollectionMetadata) ||
               resourceType == typeof(SubmodelElementCollectionValue) ||
               resourceType == typeof(AnnotatedRelationshipElementMetadata) ||
               resourceType == typeof(AnnotatedRelationshipElementValue) ||
               resourceType == typeof(EntityMetadata) ||
               resourceType == typeof(EntityValue) ||
               resourceType == typeof(OperationMetadata) ||
               resourceType == typeof(OperationValue);
    }

    private static string GetModifierString(LevelEnum level, ExtentEnum extent)
    {
        return level == LevelEnum.Core ? level.ToString() : extent.ToString();
    }
}