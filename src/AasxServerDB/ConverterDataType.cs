using AasCore.Aas3_0;

namespace AasxServerDB;
using System;
using System.Collections.Generic;
using AasCore.Aas3_0;
using Microsoft.IdentityModel.Tokens;

public static class ConverterDataType
{
    public static Dictionary<DataTypeDefXsd, string> DataTypeToTable = new Dictionary<DataTypeDefXsd, string>() {
        { DataTypeDefXsd.AnyUri, "S" },
        { DataTypeDefXsd.Base64Binary, "S" },
        { DataTypeDefXsd.Boolean, "S" },
        { DataTypeDefXsd.Byte, "I" },
        { DataTypeDefXsd.Date, "S" },
        { DataTypeDefXsd.DateTime, "S" },
        { DataTypeDefXsd.Decimal, "S" },
        { DataTypeDefXsd.Double, "D" },
        { DataTypeDefXsd.Duration, "S" },
        { DataTypeDefXsd.Float, "D" },
        { DataTypeDefXsd.GDay, "S" },
        { DataTypeDefXsd.GMonth, "S" },
        { DataTypeDefXsd.GMonthDay, "S" },
        { DataTypeDefXsd.GYear, "S" },
        { DataTypeDefXsd.GYearMonth, "S" },
        { DataTypeDefXsd.HexBinary, "S" },
        { DataTypeDefXsd.Int, "I" },
        { DataTypeDefXsd.Integer, "I" },
        { DataTypeDefXsd.Long, "I" },
        { DataTypeDefXsd.NegativeInteger, "I" },
        { DataTypeDefXsd.NonNegativeInteger, "I" },
        { DataTypeDefXsd.NonPositiveInteger, "I" },
        { DataTypeDefXsd.PositiveInteger, "I" },
        { DataTypeDefXsd.Short, "I" },
        { DataTypeDefXsd.String, "S" },
        { DataTypeDefXsd.Time, "S" },
        { DataTypeDefXsd.UnsignedByte, "I" },
        { DataTypeDefXsd.UnsignedInt, "I" },
        { DataTypeDefXsd.UnsignedLong, "I" },
        { DataTypeDefXsd.UnsignedShort, "I" }
    };

    public static DataTypeDefXsd? StringToDataType(string? tableDataType)
    {
        if (tableDataType.IsNullOrEmpty())
            return null;

        foreach (var item in Enum.GetValues<DataTypeDefXsd>())
            if (item.ToString().Equals(tableDataType))
                return item;

        return null;
    }
}