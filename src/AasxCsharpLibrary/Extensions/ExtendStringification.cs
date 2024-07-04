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

using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class ExtendStringification
    {
        public static IEnumerable<string> DataTypeXsdToStringArray() =>
            Enum.GetValues(typeof(DataTypeDefXsd)).OfType<DataTypeDefXsd>().Select((dt) => Stringification.ToString(dt));
    }
}
