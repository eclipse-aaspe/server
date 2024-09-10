/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
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
            ArgumentNullException.ThrowIfNull(sourceList);

            var output = new List<IClass>();
            var context = new LevelExtentModifierContext(level, extent, isGetAllSme: true);
            foreach (var source in sourceList)
            {
                output.Add(_transformer.Transform(source, context));
            }

            return output;
        }
    }
}
