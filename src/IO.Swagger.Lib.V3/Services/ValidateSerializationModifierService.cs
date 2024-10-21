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

namespace IO.Swagger.Lib.V3.Services;

using System;
using System.Reflection.Emit;
using System.Xml.Linq;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Models;

public class ValidateSerializationModifierService : IValidateSerializationModifierService
{
    /// <inheritdoc/>
    public ExtentEnum ValidateExtent(string extent)
    {
        ExtentEnum result = ExtentEnum.WithoutBlobValue;
        if (!string.IsNullOrEmpty(extent))
        {
            try
            {
                result = Enum.Parse<ExtentEnum>(extent, true);
            }
            catch (Exception e)
            {
                throw new InvalidSerializationModifierException("extent");
            }
        }

        return result;
    }
    /// <inheritdoc/>
    public LevelEnum ValidateLevel(string level)
    {
        LevelEnum result = LevelEnum.Deep;
        if (!string.IsNullOrEmpty(level))
        {
            try
            {
                result = Enum.Parse<LevelEnum>(level, true);
            }
            catch(Exception e)
            {
                throw new InvalidSerializationModifierException("level");
            }
        }

        return result;
    }
}
