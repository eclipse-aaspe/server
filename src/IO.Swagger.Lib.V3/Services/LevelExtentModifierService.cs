using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent;
using IO.Swagger.Models;
using System;
using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.Services
{
    public class LevelExtentModifierService : ILevelExtentModifierService
    {
        private readonly IAppLogger<LevelExtentModifierService> _logger;
        LevelExtentTransformer _transformer;

        public LevelExtentModifierService(IAppLogger<LevelExtentModifierService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transformer = new LevelExtentTransformer();
        }

        public IClass ApplyLevelExtent(IClass that, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            if (that == null) { throw new ArgumentNullException(nameof(that)); }

            var context = new LevelExtentModifierContext(level, extent);
            return _transformer.Transform(that, context);
        }

        public List<IClass> ApplyLevelExtent(List<IClass> sourceList, LevelEnum level = LevelEnum.Deep, ExtentEnum extent = ExtentEnum.WithoutBlobValue)
        {
            if (sourceList == null) { throw new ArgumentNullException(nameof(sourceList)); }
            var output = new List<IClass>();
            var context = new LevelExtentModifierContext(level, extent);
            foreach (var source in sourceList)
            {
                output.Add(_transformer.Transform(source, context));
            }

            return output;
        }
    }
}
