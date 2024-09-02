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

namespace Extensions
{
    public static class ExtendDataElement
    {
        public static DataTypeDefXsd[] ValueTypes_Number =
            new[] { DataTypeDefXsd.Decimal, DataTypeDefXsd.Double, DataTypeDefXsd.Float,
                DataTypeDefXsd.Integer, DataTypeDefXsd.Long, DataTypeDefXsd.Int, DataTypeDefXsd.Short,
                DataTypeDefXsd.Byte, DataTypeDefXsd.NonNegativeInteger, DataTypeDefXsd.NonPositiveInteger,
                DataTypeDefXsd.UnsignedInt, DataTypeDefXsd.Integer, DataTypeDefXsd.UnsignedByte,
                DataTypeDefXsd.UnsignedLong, DataTypeDefXsd.UnsignedShort, DataTypeDefXsd.NegativeInteger };
    }
}
