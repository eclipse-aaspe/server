/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System.Text.RegularExpressions;
using AasCore.Aas3_0;
using Contracts;

namespace AasxServerDB;

/// <summary>
/// Maps query field <c>sme.valueType</c> to predicates on <see cref="Entities.SMESet.TValue"/>
/// and <see cref="Entities.ValueSet.Annotation"/> (serialized <see cref="DataTypeDefXsd"/>), matching import in <see cref="VisitorAASX"/>.
/// </summary>
public static class QueryValueTypeExpression
{
    /// <summary>
    /// Serialize a user literal (e.g. <c>xs:string</c>) to the same string stored in <c>ValueSet.Annotation</c>.
    /// </summary>
    public static bool TrySerializeDataTypeLiteral(string literal, out string serialized)
    {
        serialized = "";
        if (string.IsNullOrWhiteSpace(literal))
            return false;

        var key = literal.Trim();
        var dt = Stringification.DataTypeDefXsdFromString(key);
        if (dt == null)
            return false;

        serialized = Serializer.SerializeElement(dt) ?? "";
        return !string.IsNullOrEmpty(serialized);
    }

    /// <summary>
    /// Builds a Dynamic LINQ fragment for <see cref="Entities.SMESet"/> + value row join (aliases <c>sme</c>, <c>valueAnnotation</c>).
    /// <paramref name="exp"/> is the comparison tail, e.g. <c> == "xs:string"</c>.
    /// Uses the same <c>TValue</c> discriminator as <see cref="SmeQueryPrefilter"/> (S / N / DT) plus serialized annotation.
    /// </summary>
    public static bool TryBuildPathValueTypeExpression(string exp, out string predicate)
    {
        predicate = "";
        if (!TryParseComparison(exp.Trim(), out var op, out var literal))
            return false;
        if (!TrySerializeDataTypeLiteral(literal, out var ann))
            return false;
        if (!SmeQueryPrefilter.TryGetTValueForValueTypeLiteral(literal, out var disc))
            return false;
        var esc = EscapeDynamicString(ann);
        predicate = op switch
        {
            "==" => $"(sme.TValue == \"{disc}\" && valueAnnotation == \"{esc}\")",
            "!=" => $"(sme.TValue != \"{disc}\" || valueAnnotation != \"{esc}\")",
            _ => ""
        };
        return predicate != "";
    }

    /// <summary>
    /// Replaces <c>sme.valueType</c> in expressions evaluated on <see cref="Entities.SMESet"/> only (e.g. <c>condition["sme"]</c>
    /// after stripping <c>sme.</c>), using <c>TValue</c> (string <c>S</c> or numeric <c>N</c>) and <c>ValueSets.Annotation</c>.
    /// </summary>
    public static string RewriteSmeValueTypeForSmeEntityExpression(string expr)
    {
        if (string.IsNullOrEmpty(expr) || !expr.Contains("sme.valueType", StringComparison.Ordinal))
            return expr;

        return Regex.Replace(
            expr,
            @"sme\.valueType\s*((?:==|!=)\s*""(?:[^""\\]|\\.)*"")",
            m =>
            {
                var tail = m.Groups[1].Value.TrimStart();
                if (!TryBuildPredicateOnSmeEntityValueType(tail, out var pred))
                    return m.Value;
                return pred;
            });
    }

    /// <summary>
    /// Replaces <c>sme.valueType</c> with <c>sme.TValue</c> / <c>valueAnnotation</c> fragments for joined queries
    /// (same as <see cref="TryBuildPathValueTypeExpression"/>).
    /// </summary>
    public static string RewriteSmeValueTypeForJoinExpression(string expr)
    {
        if (string.IsNullOrEmpty(expr) || !expr.Contains("sme.valueType", StringComparison.Ordinal))
            return expr;

        return Regex.Replace(
            expr,
            @"sme\.valueType\s*((?:==|!=)\s*""(?:[^""\\]|\\.)*"")",
            m =>
            {
                var tail = m.Groups[1].Value.TrimStart();
                if (!TryBuildPathValueTypeExpression(tail, out var pred))
                    return m.Value;
                return pred;
            });
    }

    /// <summary>
    /// Expands <c>sme.valueType</c> comparisons in expressions that use SME_/Value column aliases (GetSMs dynamic projection).
    /// </summary>
    public static string ExpandValueTypeComparisonsForSmeValueProjection(string expr)
    {
        if (string.IsNullOrEmpty(expr) || !expr.Contains("sme.valueType", StringComparison.Ordinal))
            return expr;

        return Regex.Replace(expr, @"sme\.valueType\s*(==|!=)\s*""((?:[^""\\]|\\.)*)""", m =>
        {
            var op = m.Groups[1].Value;
            var rawLiteral = Regex.Unescape(m.Groups[2].Value);
            if (!TrySerializeDataTypeLiteral(rawLiteral, out var ann))
                return m.Value;
            if (!SmeQueryPrefilter.TryGetTValueForValueTypeLiteral(rawLiteral, out var disc))
                return m.Value;
            var esc = EscapeDynamicString(ann);
            return op switch
            {
                "==" => $"(SME_TValue == \"{disc}\" && ValueAnnotation == \"{esc}\")",
                "!=" => $"(SME_TValue != \"{disc}\" || ValueAnnotation != \"{esc}\")",
                _ => m.Value
            };
        });
    }

    private static bool TryParseComparison(string exp, out string op, out string literal)
    {
        op = "";
        literal = "";
        var m = Regex.Match(exp, @"^(==|!=)\s*""((?:[^""\\]|\\.)*)""\s*$");
        if (!m.Success)
            return false;
        op = m.Groups[1].Value;
        literal = Regex.Unescape(m.Groups[2].Value);
        return true;
    }

    private static string EscapeDynamicString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static bool TryBuildPredicateOnSmeEntityValueType(string expTail, out string predicate)
    {
        predicate = "";
        if (!TryParseComparison(expTail.Trim(), out var op, out var literal))
            return false;
        var dt = Stringification.DataTypeDefXsdFromString(literal.Trim());
        if (!dt.HasValue || !VisitorAASX.DataTypeToTable.TryGetValue(dt.Value, out var disc))
            return false;
        predicate = op switch
        {
            "==" => $"(TValue == \"{disc}\")",
            "!=" => $"(TValue != \"{disc}\")",
            _ => ""
        };
        return predicate != "";
    }
}
