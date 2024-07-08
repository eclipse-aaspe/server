using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces;

/// <summary>
/// Service responsible for generating serialized representations of Asset Administration Shells (AAS) and Submodels.
/// </summary>
public interface IGenerateSerializationService
{
    /// <summary>
    /// Generates a serialized environment containing the requested Asset Administration Shells (AAS) and Submodels.
    /// </summary>
    /// <param name="aasIds">Optional list of AAS IDs to include in the serialized output.</param>
    /// <param name="submodelIds">Optional list of Submodel IDs to include in the serialized output.</param>
    /// <returns>An <see cref="Environment"/> object containing the specified AAS and Submodels.</returns>
    Environment GenerateSerializationByIds(List<string?>? aasIds = null, List<string?>? submodelIds = null);
}