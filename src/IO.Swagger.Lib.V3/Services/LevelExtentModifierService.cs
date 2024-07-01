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
using IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IO.Swagger.Lib.V3.Services;

/// <inheritdoc />
public class LevelExtentModifierService : ILevelExtentModifierService
{
    private readonly LevelExtentTransformer _transformer = new();

    /// <inheritdoc />
    public IClass ApplyLevelExtent(IClass that, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
    {
        ArgumentNullException.ThrowIfNull(that);

        var context = new LevelExtentModifierContext(level, extent);
        return _transformer.Transform(that, context);
    }
    
    /// <inheritdoc />
    public List<IClass?> ApplyLevelExtent(List<IClass?> that, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
    {
        ArgumentNullException.ThrowIfNull(that);

        var context = new LevelExtentModifierContext(level, extent);

        return that.Select(source => source != null ? _transformer.Transform(source, context) : null).ToList();
    }
}