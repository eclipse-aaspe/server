/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.PathModifier;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services;

using System.Linq;

/// <inheritdoc />
public class PathModifierService : IPathModifierService
{
    private static readonly PathTransformer PathTransformer = new();

    /// <inheritdoc />
    public List<string> ToIdShortPath(IClass that)
    {
        var context = new PathModifierContext();
        return PathTransformer.Transform(that, context);
    }

    /// <inheritdoc />
    public List<List<string>> ToIdShortPath(List<ISubmodel> submodelList) =>
        (from submodel in submodelList let context = new PathModifierContext() select PathTransformer.Transform(submodel, context)).ToList();

    /// <inheritdoc />
    public List<List<string>> ToIdShortPath(List<ISubmodelElement> submodelElementList) =>
        (from submodelElement in submodelElementList let context = new PathModifierContext() select PathTransformer.Transform(submodelElement, context)).ToList();
}