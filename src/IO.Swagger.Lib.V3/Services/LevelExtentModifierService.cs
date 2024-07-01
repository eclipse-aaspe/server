using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services;

using System.Linq;

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