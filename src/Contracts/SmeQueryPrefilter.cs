/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System.Collections.Generic;
using AasCore.Aas3_0;

namespace Contracts;

/// <summary>
/// Maps XSD value-type literals to <c>SMESet.TValue</c> discriminators for <c>createExpression(..., "sme.")</c>
/// pre-filters. Must stay aligned with VisitorAASX.DataTypeToTable in the DB layer.
/// </summary>
public static class SmeQueryPrefilter
{
    private static readonly Dictionary<DataTypeDefXsd, string> DataTypeToTable = new()
    {
        { DataTypeDefXsd.AnyUri, "S" },
        { DataTypeDefXsd.Base64Binary, "S" },
        { DataTypeDefXsd.Boolean, "N" },
        { DataTypeDefXsd.Byte, "N" },
        { DataTypeDefXsd.Date, "DT" },
        { DataTypeDefXsd.DateTime, "DT" },
        { DataTypeDefXsd.Decimal, "S" },
        { DataTypeDefXsd.Double, "N" },
        { DataTypeDefXsd.Duration, "S" },
        { DataTypeDefXsd.Float, "N" },
        { DataTypeDefXsd.GDay, "S" },
        { DataTypeDefXsd.GMonth, "S" },
        { DataTypeDefXsd.GMonthDay, "S" },
        { DataTypeDefXsd.GYear, "S" },
        { DataTypeDefXsd.GYearMonth, "S" },
        { DataTypeDefXsd.HexBinary, "N" },
        { DataTypeDefXsd.Int, "N" },
        { DataTypeDefXsd.Integer, "N" },
        { DataTypeDefXsd.Long, "N" },
        { DataTypeDefXsd.NegativeInteger, "N" },
        { DataTypeDefXsd.NonNegativeInteger, "N" },
        { DataTypeDefXsd.NonPositiveInteger, "N" },
        { DataTypeDefXsd.PositiveInteger, "N" },
        { DataTypeDefXsd.Short, "N" },
        { DataTypeDefXsd.String, "S" },
        { DataTypeDefXsd.Time, "DT" },
        { DataTypeDefXsd.UnsignedByte, "N" },
        { DataTypeDefXsd.UnsignedInt, "N" },
        { DataTypeDefXsd.UnsignedLong, "N" },
        { DataTypeDefXsd.UnsignedShort, "N" }
    };

    /// <summary>Returns the <c>TValue</c> column value (S / N / DT) for a value-type literal such as <c>xs:integer</c>.</summary>
    public static bool TryGetTValueForValueTypeLiteral(string literal, out string tvalue)
    {
        tvalue = "";
        if (string.IsNullOrWhiteSpace(literal))
            return false;
        var dt = Stringification.DataTypeDefXsdFromString(literal.Trim());
        if (!dt.HasValue)
            return false;
        return DataTypeToTable.TryGetValue(dt.Value, out tvalue);
    }

    /// <summary>
    /// Serializes a user literal (e.g. <c>xs:integer</c>) to the same string stored in <c>ValueSet.Annotation</c>.
    /// </summary>
    public static bool TrySerializeDataTypeAnnotation(string literal, out string serialized)
    {
        serialized = "";
        if (string.IsNullOrWhiteSpace(literal))
            return false;
        var dt = Stringification.DataTypeDefXsdFromString(literal.Trim());
        if (!dt.HasValue)
            return false;
        var json = Jsonization.Serialize.DataTypeDefXsdToJsonValue(dt.Value);
        serialized = json.ToString() ?? "";
        return !string.IsNullOrEmpty(serialized);
    }

    public static string EscapeForDynamicLinq(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
