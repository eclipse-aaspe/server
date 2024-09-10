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

using IO.Swagger.Models;

namespace IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent
{
    public class LevelExtentModifierContext
    {
        public LevelEnum Level { get; set; }

        public ExtentEnum Extent { get; set; }

        public bool IncludeChildren { get; set; }

        public bool IsRoot { get; set; }

        public bool IsGetAllSmes { get; set; }

        public LevelExtentModifierContext(LevelEnum level, ExtentEnum extent, bool isGetAllSme = false)
        {
            Level = level;
            Extent = extent;
            IsRoot = true;
            IncludeChildren = true;
            IsGetAllSmes = isGetAllSme;
        }
    }
}
