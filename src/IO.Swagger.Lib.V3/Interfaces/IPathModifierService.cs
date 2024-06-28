﻿using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Interfaces;

/// <summary>
/// Service for transforming paths of metamodel instances.
/// </summary>
public interface IPathModifierService
{
    /// <summary>
    /// Transforms the path of a single class instance into a short ID path.
    /// </summary>
    /// <param name="that">The class instance to transform.</param>
    /// <returns>A list of strings representing the transformed path.</returns>
    List<string> ToIdShortPath(IClass that);

    /// <summary>
    /// Transforms the paths of a list of submodels into short ID paths.
    /// </summary>
    /// <param name="submodelList">The list of submodels to transform.</param>
    /// <returns>A list of lists of strings representing the transformed paths.</returns>
    List<List<string>> ToIdShortPath(List<ISubmodel> submodelList);

    /// <summary>
    /// Transforms the paths of a list of submodel elements into short ID paths.
    /// </summary>
    /// <param name="submodelElementList">The list of submodel elements to transform.</param>
    /// <returns>A list of lists of strings representing the transformed paths.</returns>
    List<List<string>> ToIdShortPath(List<ISubmodelElement> submodelElementList);
}