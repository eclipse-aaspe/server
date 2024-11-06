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

using System.Collections.Generic;

namespace IO.Swagger.Lib.V3.SerializationModifiers.PathModifier
{
    public class PathModifierContext
    {
        private List<string>? idShortPaths;

        public string? ParentPath { get; internal set; }

        public List<string>? IdShortPaths { get => idShortPaths; set => idShortPaths = value; }

        public bool IsRoot { get; set; }

        public bool IsGetAllSmes { get; set; }

        public PathModifierContext(bool isGetAllSmes = false)
        {
            idShortPaths = new List<string>();
            IsRoot = true;
            IsGetAllSmes = isGetAllSmes;
        }
    }
}
