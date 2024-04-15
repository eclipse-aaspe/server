using System;
using System.Text.Json;
using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.Formatters;

/// <summary>
/// Interface for a strategy for serializing objects into JSON format based on their type.
/// </summary>
public interface IJsonSerializerStrategy
{
    /// <summary>
    /// Determines whether the specified object type can be serialized.
    /// </summary>
    /// <param name="objectType">The type of the object.</param>
    /// <param name="obj">The object to be serialized.</param>
    /// <returns><c>true</c> if the object can be serialized; otherwise, <c>false</c>.</returns>
    bool CanSerialize(Type objectType, object obj);

    /// <summary>
    /// Serializes the specified object into JSON format.
    /// </summary>
    /// <param name="writer">The JSON writer to write to.</param>
    /// <param name="obj">The object to be serialized.</param>
    /// <param name="level">The serialization level.</param>
    /// <param name="extent">The serialization extent.</param>
    void Serialize(Utf8JsonWriter writer, object obj, LevelEnum level, ExtentEnum extent);
}