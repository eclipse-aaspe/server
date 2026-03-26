/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

using System.Text.RegularExpressions;
using AasCore.Aas3_0;

namespace AasxServerDB;

/// <summary>
/// Maps query field <c>sme.language</c> to predicates on <see cref="Entities.SMESet.TValue"/> and
/// <see cref="Entities.ValueSet.Annotation"/> (BCP 47 language tag for MLP rows), matching import in <see cref="VisitorAASX"/>.
/// </summary>
public static class QueryLanguageExpression
{
    /// <summary>Returns true if <paramref name="literal"/> is a non-empty BCP 47 tag per AAS verification.</summary>
    public static bool TryValidateLanguageLiteral(string literal, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(literal))
            return false;
        var t = literal.Trim();
        if (Verification.VerifyBcp47LanguageTag(t).Any())
            return false;
        normalized = t;
        return true;
    }

    /// <summary>
    /// Builds a Dynamic LINQ fragment (aliases <c>sme</c>, <c>valueAnnotation</c>).
    /// <paramref name="exp"/> is the comparison tail, e.g. <c> == "en"</c> or <c>.Contains("en")</c>.
    /// </summary>
    public static bool TryBuildPathLanguageExpression(string exp, out string predicate)
    {
        predicate = "";
        var trimmed = exp.Trim();

        if (TryParseComparison(trimmed, out var cmpOp, out var literal))
        {
            if (!TryValidateLanguageLiteral(literal, out var lang))
                return false;
            var esc = EscapeDynamicString(lang);
            predicate = cmpOp switch
            {
                "==" => $"(sme.TValue == \"S\" && valueAnnotation == \"{esc}\")",
                "!=" => $"(sme.TValue != \"S\" || valueAnnotation != \"{esc}\")",
                _ => ""
            };
            return predicate != "";
        }

        if (!TryParseStringMethod(trimmed, out var method, out var needle))
            return false;

        var escNeedle = EscapeDynamicString(needle);
        // MLP: TValue S and Annotation = language tag string.
        predicate = method switch
        {
            "Contains" => $"(sme.TValue == \"S\" && valueAnnotation.Contains(\"{escNeedle}\"))",
            "StartsWith" => $"(sme.TValue == \"S\" && valueAnnotation.StartsWith(\"{escNeedle}\"))",
            "EndsWith" => $"(sme.TValue == \"S\" && valueAnnotation.EndsWith(\"{escNeedle}\"))",
            _ => ""
        };
        return predicate != "";
    }

    /// <summary>
    /// Replaces <c>sme.language</c> comparisons in expressions evaluated on <see cref="Entities.SMESet"/>
    /// (e.g. <c>condition["sme"]</c> after stripping <c>sme.</c>) with predicates on <c>TValue</c> and
    /// <see cref="Entities.ValueSet.Annotation"/> via navigation <c>ValueSets</c>.
    /// </summary>
    public static string RewriteSmeLanguageForSmeEntityExpression(string expr)
    {
        if (string.IsNullOrEmpty(expr) || !expr.Contains("sme.language", StringComparison.Ordinal))
            return expr;

        return Regex.Replace(
            expr,
            @"sme\.language\s*((?:==|!=)\s*""(?:[^""\\]|\\.)*""|\.(?:Contains|StartsWith|EndsWith)\(\s*""(?:[^""\\]|\\.)*""\s*\))",
            m =>
            {
                var tail = m.Groups[1].Value.TrimStart();
                if (!TryBuildPredicateOnSmeEntity(tail, out var pred))
                    return m.Value;
                return pred;
            });
    }

    /// <summary>
    /// Replaces <c>sme.language</c> with <c>sme.TValue</c> / <c>valueAnnotation</c> fragments for joined queries
    /// (same semantics as <see cref="TryBuildPathLanguageExpression"/>).
    /// </summary>
    public static string RewriteSmeLanguageForJoinExpression(string expr)
    {
        if (string.IsNullOrEmpty(expr) || !expr.Contains("sme.language", StringComparison.Ordinal))
            return expr;

        return Regex.Replace(
            expr,
            @"sme\.language\s*((?:==|!=)\s*""(?:[^""\\]|\\.)*""|\.(?:Contains|StartsWith|EndsWith)\(\s*""(?:[^""\\]|\\.)*""\s*\))",
            m =>
            {
                var tail = m.Groups[1].Value.TrimStart();
                if (!TryBuildPathLanguageExpression(tail, out var pred))
                    return m.Value;
                return pred;
            });
    }

    /// <summary>
    /// Expands <c>sme.language</c> in GetSMs dynamic projection (SME_TValue / ValueAnnotation).
    /// </summary>
    public static string ExpandLanguageComparisonsForSmeValueProjection(string expr)
    {
        if (string.IsNullOrEmpty(expr) || !expr.Contains("sme.language", StringComparison.Ordinal))
            return expr;

        expr = Regex.Replace(expr, @"sme\.language\s*(==|!=)\s*""((?:[^""\\]|\\.)*)""", m =>
        {
            var op = m.Groups[1].Value;
            var rawLiteral = Regex.Unescape(m.Groups[2].Value);
            if (!TryValidateLanguageLiteral(rawLiteral, out var lang))
                return m.Value;
            var esc = EscapeDynamicString(lang);
            return op switch
            {
                "==" => $"(SME_TValue == \"S\" && ValueAnnotation == \"{esc}\")",
                "!=" => $"(SME_TValue != \"S\" || ValueAnnotation != \"{esc}\")",
                _ => m.Value
            };
        });

        expr = Regex.Replace(expr, @"sme\.language\.Contains\(\s*""((?:[^""\\]|\\.)*)""\s*\)", m =>
        {
            var rawLiteral = Regex.Unescape(m.Groups[1].Value);
            var esc = EscapeDynamicString(rawLiteral);
            return $"(SME_TValue == \"S\" && ValueAnnotation.Contains(\"{esc}\"))";
        });

        expr = Regex.Replace(expr, @"sme\.language\.StartsWith\(\s*""((?:[^""\\]|\\.)*)""\s*\)", m =>
        {
            var rawLiteral = Regex.Unescape(m.Groups[1].Value);
            var esc = EscapeDynamicString(rawLiteral);
            return $"(SME_TValue == \"S\" && ValueAnnotation.StartsWith(\"{esc}\"))";
        });

        expr = Regex.Replace(expr, @"sme\.language\.EndsWith\(\s*""((?:[^""\\]|\\.)*)""\s*\)", m =>
        {
            var rawLiteral = Regex.Unescape(m.Groups[1].Value);
            var esc = EscapeDynamicString(rawLiteral);
            return $"(SME_TValue == \"S\" && ValueAnnotation.EndsWith(\"{esc}\"))";
        });

        return expr;
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

    /// <summary>Parses tails like <c>.Contains("x")</c> produced for path-tagged SME fields.</summary>
    private static bool TryParseStringMethod(string exp, out string method, out string literal)
    {
        method = "";
        literal = "";
        var m = Regex.Match(exp, @"^\.(Contains|StartsWith|EndsWith)\(\s*""((?:[^""\\]|\\.)*)""\s*\)\s*$");
        if (!m.Success)
            return false;
        method = m.Groups[1].Value;
        literal = Regex.Unescape(m.Groups[2].Value);
        return true;
    }

    private static string EscapeDynamicString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// SMESet-only prefilter for language (MLP uses <c>TValue == \"S\"</c>); full language match uses join + <c>valueAnnotation</c> in <c>condition[\"all\"]</c>.
    /// </summary>
    private static bool TryBuildPredicateOnSmeEntity(string expTail, out string predicate)
    {
        predicate = "";
        var trimmed = expTail.Trim();

        if (TryParseComparison(trimmed, out var cmpOp, out _))
        {
            predicate = cmpOp switch
            {
                "==" => "(TValue == \"S\")",
                "!=" => "(TValue != \"S\")",
                _ => ""
            };
            return predicate != "";
        }

        if (!TryParseStringMethod(trimmed, out _, out _))
            return false;

        predicate = "(TValue == \"S\")";
        return true;
    }
}

