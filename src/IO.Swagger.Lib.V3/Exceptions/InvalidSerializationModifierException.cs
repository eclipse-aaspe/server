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

using System;

namespace IO.Swagger.Lib.V3.Exceptions
{
    public class InvalidSerializationModifierException : Exception
    {
        public InvalidSerializationModifierException(string modifier, string type) : base($"Invalid serialization modifier {modifier} for the requested element of type {type}.")
        {

        }

        public InvalidSerializationModifierException(string modifier) : base($"Invalid serialization modifier {modifier}.")
        {

        }

        public InvalidSerializationModifierException() : base($"Invalid serialization modifier combination Level = Deeep & Content = Reference.")
        {

        }
    }
}
