using AasCore.Aas3_0;

namespace AasxServerDB;
using System;
using System.Collections.Generic;
using AasCore.Aas3_0;
using Microsoft.IdentityModel.Tokens;

public static class ConverterDataType
{
    public static Dictionary<DataTypeDefXsd, DataTypeDefXsd> DataTypeToTable = new Dictionary<DataTypeDefXsd, DataTypeDefXsd>() {
        { DataTypeDefXsd.AnyUri, DataTypeDefXsd.String },
        { DataTypeDefXsd.Base64Binary, DataTypeDefXsd.String },
        { DataTypeDefXsd.Boolean, DataTypeDefXsd.String },
        { DataTypeDefXsd.Byte, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.Date, DataTypeDefXsd.String },
        { DataTypeDefXsd.DateTime, DataTypeDefXsd.String },
        { DataTypeDefXsd.Decimal, DataTypeDefXsd.String },
        { DataTypeDefXsd.Double, DataTypeDefXsd.Double },
        { DataTypeDefXsd.Duration, DataTypeDefXsd.String },
        { DataTypeDefXsd.Float, DataTypeDefXsd.Double },
        { DataTypeDefXsd.GDay, DataTypeDefXsd.String },
        { DataTypeDefXsd.GMonth, DataTypeDefXsd.String },
        { DataTypeDefXsd.GMonthDay, DataTypeDefXsd.String },
        { DataTypeDefXsd.GYear, DataTypeDefXsd.String },
        { DataTypeDefXsd.GYearMonth, DataTypeDefXsd.String },
        { DataTypeDefXsd.HexBinary, DataTypeDefXsd.String },
        { DataTypeDefXsd.Int, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.Integer, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.Long, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.NegativeInteger, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.NonNegativeInteger, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.NonPositiveInteger, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.PositiveInteger, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.Short, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.String, DataTypeDefXsd.String },
        { DataTypeDefXsd.Time, DataTypeDefXsd.String },
        { DataTypeDefXsd.UnsignedByte, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.UnsignedInt, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.UnsignedLong, DataTypeDefXsd.Integer },
        { DataTypeDefXsd.UnsignedShort, DataTypeDefXsd.Integer }
    };

    private static Dictionary<DataTypeDefXsd, List<string>> TableToDataType = new Dictionary<DataTypeDefXsd, List<string>>();

    public static List<string> FromTableToDataType(DataTypeDefXsd? tableDataTypeD = null, string? tableDataTypeS = null)
    {
        if (tableDataTypeD == null && tableDataTypeS == null)
            return new List<string>();

        var key = (DataTypeDefXsd) (tableDataTypeD ?? StringToDataType(tableDataTypeS));

        if (!TableToDataType.ContainsKey(key))
        {
            TableToDataType[key] = new List<string>();

            foreach (var item in Enum.GetValues<DataTypeDefXsd>())
                if (DataTypeToTable[item] == key)
                    TableToDataType[key].Add(item.ToString());
        }

        return TableToDataType[key];
    }

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