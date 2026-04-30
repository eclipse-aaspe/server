/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace Contracts;

using System.Security.Claims;
using System.Text;
using Newtonsoft.Json;

/// <summary>
/// Structured SQL conditions produced by <see cref="QueryGrammarJSON.CreateSqlConditions"/>.
/// Replaces the LINQ-string <c>conditionsExpression</c> dictionary fed into <c>CombineTablesLEFT</c>.
/// All strings are ready-to-embed SQLite SQL fragments.
/// </summary>
public class SqlConditions
{
    private static readonly string[] ConditionKeys = ["all", "aas", "sm", "sme", "value"];

    /// <summary>
    /// Prefix of the inline placeholder emitted by <see cref="QueryGrammarJSON.CreateSqlConditions"/>
    /// for a <c>$attribute</c>(<c>CLAIM</c>) operand — substituted with the real token-claim value
    /// per request via <see cref="SubstituteTokenClaims"/>. Lives inside a SQL string literal,
    /// e.g. <c>'$$claim:token:sub$$'</c>.
    /// </summary>
    public const string ClaimSentinelPrefix = "$$claim:";

    /// <summary>Suffix of the claim sentinel; see <see cref="ClaimSentinelPrefix"/>.</summary>
    public const string ClaimSentinelSuffix = "$$";

    /// <summary>
    /// SQL string literal carrying a deferred token-claim reference, e.g.
    /// <c>BuildClaimSentinelSqlLiteral("token:sub") == "'$$claim:token:sub$$'"</c>.
    /// <see cref="SubstituteTokenClaims"/> resolves the sentinel against a request's
    /// <see cref="System.Security.Claims.Claim"/> list at execution time.
    /// </summary>
    public static string BuildClaimSentinelSqlLiteral(string claimType)
        => $"'{ClaimSentinelPrefix}{(claimType ?? string.Empty).Replace("'", "''")}{ClaimSentinelSuffix}'";

    public SqlConditions()
    {
        foreach (var key in ConditionKeys)
        {
            FormulaConditions[key] = "";
            FilterConditions[key] = "";
        }
    }

    /// <summary>
    /// Per-scope WHERE predicates (no table prefix, used to filter individual DbSets before the main JOIN).
    /// Keys: "all", "aas", "sm", "sme", "value".
    /// Empty string means no restriction for that scope.
    /// </summary>
    public Dictionary<string, string> FormulaConditions { get; } = new();

    /// <summary>
    /// Per-scope SQL predicates from access-rule FILTER blocks.
    /// Keys: "all", "aas", "sm", "sme", "value".
    /// </summary>
    public Dictionary<string, string> FilterConditions { get; } = new();

    /// <summary>
    /// Query projection hint from the parsed request, e.g. <c>"id"</c> or <c>"match"</c>.
    /// Kept with the SQL conditions so callers do not need a parallel metadata dictionary.
    /// </summary>
    public string Select { get; set; } = "";

    /// <summary>
    /// Per-scope C# Dynamic LINQ expressions for in-memory filtering of AAS model objects.
    /// Keys: "sm." (for <c>Submodel</c>), "sme." (for <c>ISubmodelElement</c>).
    /// Derived from <see cref="FormulaConditions"/> <c>sm</c>/<c>sme</c> SQL via <see cref="SqlToLinqConverter"/> whenever scopes are finalized (create/merge).
    /// </summary>
    public Dictionary<string, string> FormulaConditionsCSharp { get; } = new();

    /// <summary>
    /// Fills <see cref="FormulaConditionsCSharp"/> from the current <c>sm</c>/<c>sme</c> entries in <see cref="FormulaConditions"/>.
    /// </summary>
    public static void RefreshFormulaConditionsCSharpFromFormulaSql(SqlConditions sc)
    {
        sc.FormulaConditionsCSharp["sm."] = SqlToLinqConverter.Convert(sc.FormulaConditions.GetValueOrDefault("sm", ""), "sm.");
        sc.FormulaConditionsCSharp["sme."] = SqlToLinqConverter.Convert(sc.FormulaConditions.GetValueOrDefault("sme", ""), "sme.");
    }

    /// <summary>
    /// Standalone path conditions (from <c>$sme.idShortPath#field</c> outside a <c>$match</c>).
    /// Paths[i] corresponds to placeholder <c>$$path{i}$$</c> in <c>FormulaConditions["all"]</c>.
    /// </summary>
    public List<PathJoin> Paths { get; } = new();

    /// <summary>
    /// Match groups (from <c>$match</c>).  Each group owns its paths explicitly.
    /// Matches[i] corresponds to placeholder <c>$$match{i}$$</c> in <c>FormulaConditions["all"]</c>.
    /// </summary>
    public List<MatchJoin> Matches { get; } = new();

    /// <summary>
    /// Correlated EXISTS predicates produced from direct <c>$sme#value</c> overall subtrees.
    /// ExistsConditions[i] corresponds to placeholder <c>$$exists{i}$$</c> in <c>FormulaConditions["all"]</c>.
    /// </summary>
    public List<ExistsCondition> ExistsConditions { get; } = new();

    /// <summary>
    /// Returns a deep copy that owns its own dictionaries and join lists so per-request mutations
    /// (e.g. <see cref="SubstituteTokenClaims"/>, single-row Allow-checks) cannot leak into cached
    /// <c>AccessPermissionRule._formula_sqlConditions</c> instances shared across requests.
    /// </summary>
    public SqlConditions Clone()
    {
        var dst = new SqlConditions { Select = Select };
        foreach (var kv in FormulaConditions)
            dst.FormulaConditions[kv.Key] = kv.Value;
        foreach (var kv in FilterConditions)
            dst.FilterConditions[kv.Key] = kv.Value;
        foreach (var kv in FormulaConditionsCSharp)
            dst.FormulaConditionsCSharp[kv.Key] = kv.Value;
        foreach (var p in Paths)
            dst.Paths.Add(new PathJoin { Placeholder = p.Placeholder, SubquerySql = p.SubquerySql, IdShortPath = p.IdShortPath });
        foreach (var m in Matches)
        {
            var nm = new MatchJoin { Placeholder = m.Placeholder, JoinConditionSql = m.JoinConditionSql };
            foreach (var p in m.Paths)
                nm.Paths.Add(new PathJoin { Placeholder = p.Placeholder, SubquerySql = p.SubquerySql, IdShortPath = p.IdShortPath });
            dst.Matches.Add(nm);
        }
        foreach (var e in ExistsConditions)
            dst.ExistsConditions.Add(new ExistsCondition { Placeholder = e.Placeholder, PredicateSql = e.PredicateSql });
        return dst;
    }

    /// <summary>
    /// Replaces every <see cref="ClaimSentinelPrefix"/>…<see cref="ClaimSentinelSuffix"/> placeholder
    /// produced by <c>$attribute(CLAIM(...))</c> with the matching token-claim value (SQL-escaped),
    /// in every SQL fragment carried by this instance. Mirrors the substitution loop the legacy
    /// <c>SecurityService.GetCondition</c> performed before commit <c>4bef43ac</c> dropped it.
    /// <para>
    /// Missing claims resolve to an empty string (the surrounding string literal becomes <c>''</c>),
    /// so a CLAIM-gated rule cannot accidentally widen access for users without that claim.
    /// JSON-formatted claim values (<c>{"key": [...]}</c>) are flattened with a single-space
    /// separator — matches <c>SecurityService.HandleJsonFormatedTokenClaim</c>.
    /// </para>
    /// <para>
    /// Caller is expected to <see cref="RefreshFormulaConditionsCSharpFromFormulaSql"/> afterwards
    /// so the C# mirror reflects the substituted SQL.
    /// </para>
    /// </summary>
    public void SubstituteTokenClaims(IEnumerable<Claim>? tokenClaims)
    {
        var claimList = tokenClaims as IList<Claim> ?? tokenClaims?.ToList();
        foreach (var key in FormulaConditions.Keys.ToList())
            FormulaConditions[key] = ReplaceClaimSentinels(FormulaConditions[key], claimList);
        foreach (var key in FilterConditions.Keys.ToList())
            FilterConditions[key] = ReplaceClaimSentinels(FilterConditions[key], claimList);
        foreach (var p in Paths)
            p.SubquerySql = ReplaceClaimSentinels(p.SubquerySql, claimList);
        foreach (var m in Matches)
        {
            m.JoinConditionSql = ReplaceClaimSentinels(m.JoinConditionSql, claimList);
            foreach (var p in m.Paths)
                p.SubquerySql = ReplaceClaimSentinels(p.SubquerySql, claimList);
        }
        foreach (var e in ExistsConditions)
            e.PredicateSql = ReplaceClaimSentinels(e.PredicateSql, claimList);
    }

    private static string ReplaceClaimSentinels(string? sql, IList<Claim>? claims)
    {
        if (string.IsNullOrEmpty(sql) || sql!.IndexOf(ClaimSentinelPrefix, StringComparison.Ordinal) < 0)
            return sql ?? string.Empty;

        var sb = new StringBuilder(sql.Length);
        var idx = 0;
        while (idx < sql.Length)
        {
            var start = sql.IndexOf(ClaimSentinelPrefix, idx, StringComparison.Ordinal);
            if (start < 0)
            {
                sb.Append(sql, idx, sql.Length - idx);
                break;
            }
            // Sentinel is always wrapped in a SQL string literal (single quotes) — the surrounding
            // quotes stay; we only replace the placeholder body with the escaped claim value.
            var typeStart = start + ClaimSentinelPrefix.Length;
            var end = sql.IndexOf(ClaimSentinelSuffix, typeStart, StringComparison.Ordinal);
            if (end < 0)
            {
                sb.Append(sql, idx, sql.Length - idx);
                break;
            }
            sb.Append(sql, idx, start - idx);
            var claimType = sql.Substring(typeStart, end - typeStart);
            var resolved = ResolveClaimValue(claims, claimType);
            sb.Append(resolved.Replace("'", "''"));
            idx = end + ClaimSentinelSuffix.Length;
        }
        return sb.ToString();
    }

    private static string ResolveClaimValue(IList<Claim>? claims, string claimType)
    {
        if (claims == null || claims.Count == 0 || string.IsNullOrEmpty(claimType))
            return string.Empty;
        var raw = claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        if (string.IsNullOrEmpty(raw))
            return string.Empty;
        if (raw[0] == '{')
            return FlattenJsonClaimValue(raw);
        return raw;
    }

    /// <summary>
    /// Flattens JSON-formatted token-claim values like <c>{"sub": ["a", "b"]}</c> into a
    /// single space-separated string — matches the legacy <c>HandleJsonFormatedTokenClaim</c>.
    /// </summary>
    private static string FlattenJsonClaimValue(string value)
    {
        try
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(value);
            if (dict == null || dict.Count == 0)
                return value;
            var first = dict.First();
            return first.Value == null || first.Value.Count == 0
                ? value
                : string.Join(" ", first.Value);
        }
        catch
        {
            return value;
        }
    }
}

/// <summary>
/// A correlated EXISTS predicate for an overall condition. The predicate SQL uses <c>v</c> for ValueSets.
/// </summary>
public class ExistsCondition
{
    /// <summary>Placeholder token used in <c>SqlConditions.FormulaConditions["all"]</c>, e.g. <c>exists0</c>.</summary>
    public string Placeholder { get; set; } = "";

    /// <summary>Predicate SQL inside the correlated EXISTS query.</summary>
    public string PredicateSql { get; set; } = "";
}

/// <summary>
/// A single LEFT-JOIN subquery for one <c>$sme.idShortPath#field op value</c> path condition.
/// The subquery uses internal alias <c>sme</c> for SMESets and <c>v</c> for ValueSets.
/// Callers may rename <c>sme</c> → <c>smePath{n}</c> via string replacement.
/// </summary>
public class PathJoin
{
    /// <summary>Placeholder token used in <c>SqlConditions.FormulaConditions["all"]</c>, e.g. <c>path0</c>.</summary>
    public string Placeholder { get; set; } = "";

    /// <summary>
    /// SQL body of the LEFT JOIN subquery (FROM SMESets sme LEFT JOIN ValueSets v … WHERE …).
    /// No SELECT header — callers add that with the right alias.
    /// </summary>
    public string SubquerySql { get; set; } = "";

    /// <summary>Original idShortPath expression (e.g. "Records[].DateOfRecord") for substr() generation in $match.</summary>
    public string IdShortPath { get; set; } = "";
}

/// <summary>
/// Merges two <see cref="SqlConditions"/> instances into one.
/// <para>
/// FormulaConditions are AND-combined per key.
/// Security Paths/Matches are renumbered so their placeholder indices follow after the query ones
/// before being appended — this keeps every <c>$$path{n}$$</c>/<c>$$match{n}$$</c> reference
/// in the merged <c>FormulaConditions["all"]</c> unambiguous.
/// </para>
/// </summary>
public static class SqlConditionsMerger
{
    public static SqlConditions? Merge(SqlConditions? query, SqlConditions? security)
    {
        if (query == null && security == null) return null;
        if (query == null) return security;
        if (security == null) return query;

        var merged = new SqlConditions();

        // --- FormulaConditions: AND per key ---
        MergeConditions(query.FormulaConditions, security.FormulaConditions, merged.FormulaConditions, "AND", skipAll: true);
        MergeConditions(query.FilterConditions, security.FilterConditions, merged.FilterConditions, "AND");

        // --- Renumber security placeholders ---
        int pathOffset  = query.Paths.Count;
        int matchOffset = query.Matches.Count;
        int existsOffset = query.ExistsConditions.Count;

        // Rewrite security FormulaConditions["all"] with shifted indices
        var secOverall = security.FormulaConditions.GetValueOrDefault("all", "");
        // Process in reverse order so "path10" isn't clobbered before "path1"
        for (int i = security.Paths.Count - 1; i >= 0; i--)
            secOverall = secOverall.Replace($"$$path{i}$$", $"$$path{i + pathOffset}$$");
        for (int i = security.Matches.Count - 1; i >= 0; i--)
            secOverall = secOverall.Replace($"$$match{i}$$", $"$$match{i + matchOffset}$$");
        for (int i = security.ExistsConditions.Count - 1; i >= 0; i--)
            secOverall = secOverall.Replace($"$$exists{i}$$", $"$$exists{i + existsOffset}$$");

        // --- FormulaConditions["all"]: AND-combine ---
        var qOver = NormalizeNeutralCondition(query.FormulaConditions.GetValueOrDefault("all", ""));
        secOverall = NormalizeNeutralCondition(secOverall);
        merged.FormulaConditions["all"] =
            !string.IsNullOrWhiteSpace(qOver) && !string.IsNullOrWhiteSpace(secOverall)
                ? $"({qOver}) AND ({secOverall})"
                : string.IsNullOrWhiteSpace(qOver) ? secOverall : qOver;

        // --- Paths: query first, then renumbered security ---
        foreach (var p in query.Paths)
            merged.Paths.Add(p);
        for (int i = 0; i < security.Paths.Count; i++)
        {
            var sp = security.Paths[i];
            merged.Paths.Add(new PathJoin
            {
                Placeholder  = $"path{i + pathOffset}",
                SubquerySql  = sp.SubquerySql,
                IdShortPath  = sp.IdShortPath
            });
        }

        // --- Matches: query first, then renumbered security ---
        foreach (var m in query.Matches)
            merged.Matches.Add(m);
        for (int i = 0; i < security.Matches.Count; i++)
        {
            var sm = security.Matches[i];
            var newMatch = new MatchJoin
            {
                Placeholder      = $"match{i + matchOffset}",
                JoinConditionSql = sm.JoinConditionSql
            };
            foreach (var p in sm.Paths) newMatch.Paths.Add(p);
            merged.Matches.Add(newMatch);
        }

        // --- EXISTS: query first, then renumbered security ---
        foreach (var e in query.ExistsConditions)
            merged.ExistsConditions.Add(e);
        for (int i = 0; i < security.ExistsConditions.Count; i++)
        {
            var se = security.ExistsConditions[i];
            merged.ExistsConditions.Add(new ExistsCondition
            {
                Placeholder = $"exists{i + existsOffset}",
                PredicateSql = se.PredicateSql
            });
        }

        SqlConditions.RefreshFormulaConditionsCSharpFromFormulaSql(merged);
        // Projection ($select: id | match) lives on the user query; security fragments usually omit it.
        merged.Select = !string.IsNullOrWhiteSpace(query.Select)
            ? query.Select
            : security.Select ?? "";

        return merged;
    }

    /// <summary>
    /// OR-combines two <see cref="SqlConditions"/> — used when accumulating rules where any rule may match.
    /// FormulaConditions are OR-combined per key.
    /// Paths/Matches are renumbered and appended (same logic as <see cref="Merge"/>).
    /// </summary>
    public static SqlConditions? OrMerge(SqlConditions? left, SqlConditions? right)
    {
        if (left == null && right == null) return null;
        if (left == null) return right;
        if (right == null) return left;

        var merged = new SqlConditions();

        // --- FormulaConditions: OR per key ---
        MergeConditions(left.FormulaConditions, right.FormulaConditions, merged.FormulaConditions, "OR", skipAll: true);
        MergeConditions(left.FilterConditions, right.FilterConditions, merged.FilterConditions, "OR");

        // --- Renumber right placeholders ---
        int pathOffset  = left.Paths.Count;
        int matchOffset = left.Matches.Count;
        int existsOffset = left.ExistsConditions.Count;

        var rightOverall = right.FormulaConditions.GetValueOrDefault("all", "");
        for (int i = right.Paths.Count - 1; i >= 0; i--)
            rightOverall = rightOverall.Replace($"$$path{i}$$", $"$$path{i + pathOffset}$$");
        for (int i = right.Matches.Count - 1; i >= 0; i--)
            rightOverall = rightOverall.Replace($"$$match{i}$$", $"$$match{i + matchOffset}$$");
        for (int i = right.ExistsConditions.Count - 1; i >= 0; i--)
            rightOverall = rightOverall.Replace($"$$exists{i}$$", $"$$exists{i + existsOffset}$$");

        // --- FormulaConditions["all"]: OR-combine ---
        var lOver = NormalizeNeutralCondition(left.FormulaConditions.GetValueOrDefault("all", ""));
        rightOverall = NormalizeNeutralCondition(rightOverall);
        merged.FormulaConditions["all"] =
            !string.IsNullOrWhiteSpace(lOver) && !string.IsNullOrWhiteSpace(rightOverall)
                ? $"({lOver}) OR ({rightOverall})"
                : string.IsNullOrWhiteSpace(lOver) ? rightOverall : lOver;

        // --- Paths/Matches: left first, then renumbered right ---
        foreach (var p in left.Paths) merged.Paths.Add(p);
        for (int i = 0; i < right.Paths.Count; i++)
        {
            var rp = right.Paths[i];
            merged.Paths.Add(new PathJoin { Placeholder = $"path{i + pathOffset}", SubquerySql = rp.SubquerySql, IdShortPath = rp.IdShortPath });
        }

        foreach (var m in left.Matches) merged.Matches.Add(m);
        for (int i = 0; i < right.Matches.Count; i++)
        {
            var rm = right.Matches[i];
            var newMatch = new MatchJoin { Placeholder = $"match{i + matchOffset}", JoinConditionSql = rm.JoinConditionSql };
            foreach (var p in rm.Paths) newMatch.Paths.Add(p);
            merged.Matches.Add(newMatch);
        }

        foreach (var e in left.ExistsConditions)
            merged.ExistsConditions.Add(e);
        for (int i = 0; i < right.ExistsConditions.Count; i++)
        {
            var re = right.ExistsConditions[i];
            merged.ExistsConditions.Add(new ExistsCondition
            {
                Placeholder = $"exists{i + existsOffset}",
                PredicateSql = re.PredicateSql
            });
        }

        SqlConditions.RefreshFormulaConditionsCSharpFromFormulaSql(merged);
        merged.Select = !string.IsNullOrWhiteSpace(left.Select)
            ? left.Select
            : right.Select ?? "";

        return merged;
    }

    private static void MergeConditions(
        Dictionary<string, string> left,
        Dictionary<string, string> right,
        Dictionary<string, string> target,
        string op,
        bool skipAll = false)
    {
        var allKeys = left.Keys.Union(right.Keys);
        foreach (var key in allKeys)
        {
            if (skipAll && key == "all")
                continue;

            var lVal = NormalizeNeutralCondition(left.GetValueOrDefault(key, ""));
            var rVal = NormalizeNeutralCondition(right.GetValueOrDefault(key, ""));
            if (!string.IsNullOrWhiteSpace(lVal) && !string.IsNullOrWhiteSpace(rVal))
            {
                // Avoid (X) OR (X) / (X) AND (X) when merging duplicate rule fragments (same string).
                target[key] = string.Equals(lVal, rVal, StringComparison.Ordinal)
                    ? lVal
                    : $"({lVal}) {op} ({rVal})";
            }
            else
            {
                target[key] = string.IsNullOrWhiteSpace(lVal) ? rVal : lVal;
            }
        }
    }

    /// <summary>
    /// Empty, <c>1=1</c>, or parenthesized tautologies only (e.g. <c>(1=1)</c>, <c>((1=1))</c>) — for AND/OR merges so query <c>$boolean: true</c> does not add noise.
    /// </summary>
    public static string NormalizeNeutralCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
            return "";
        if (IsPureTautologySql(condition))
            return "";
        return condition.Trim();
    }

    /// <summary>True if the SQL fragment is only <c>1=1</c> with optional balanced wrapping parentheses.</summary>
    private static bool IsPureTautologySql(string sql)
    {
        var t = sql.Trim();
        while (true)
        {
            if (t == "1=1")
                return true;
            if (t.Length >= 2 && t[0] == '(' && t[^1] == ')')
            {
                if (!IsSingleBalancedParenthesisWrap(t))
                    return false;
                t = t.Substring(1, t.Length - 2).Trim();
                continue;
            }

            return false;
        }
    }

    private static bool IsSingleBalancedParenthesisWrap(string t)
    {
        if (t.Length < 2 || t[0] != '(')
            return false;
        var depth = 0;
        for (var i = 0; i < t.Length; i++)
        {
            if (t[i] == '(') depth++;
            else if (t[i] == ')')
            {
                depth--;
                if (depth == 0)
                    return i == t.Length - 1;
            }
        }

        return false;
    }
}

/// <summary>
/// A LEFT-JOIN subquery group for one <c>$match</c> block that checks multiple paths match in the same
/// array element.  The sub-paths are listed explicitly to make the index relationship visible.
/// </summary>
public class MatchJoin
{
    /// <summary>Placeholder token used in <c>SqlConditions.FormulaConditions["all"]</c>, e.g. <c>match0</c>.</summary>
    public string Placeholder { get; set; } = "";

    /// <summary>
    /// The individual path conditions that this match group combines.
    /// Each PathJoin here uses alias <c>smePath{k+1}</c> as its internal SME alias.
    /// </summary>
    public List<PathJoin> Paths { get; } = new();

    /// <summary>
    /// SQL expression emitted into the match subquery's WHERE clause to enforce that all paths
    /// match within the same array element, e.g. <c>Path1.SMId = Path2.SMId AND …</c>.
    /// </summary>
    public string JoinConditionSql { get; set; } = "";
}

/// <summary>
/// Converts SQL WHERE fragments (as produced by <c>BuildScopeSql</c> for sm/sme scopes)
/// to equivalent C# Dynamic LINQ expressions.  Supports the subset of SQL our grammar generates:
/// <c>=</c>, <c>&lt;&gt;</c>, <c>&gt;</c>, <c>&gt;=</c>, <c>&lt;</c>, <c>&lt;=</c>,
/// <c>GLOB</c> (prefix/suffix/contains patterns), <c>IS NULL</c>, <c>IS NOT NULL</c>,
/// <c>AND</c>, <c>OR</c>, <c>NOT</c>, and parenthesized groups.
/// </summary>
public static class SqlToLinqConverter
{
    /// <summary>
    /// Convert a SQL scope-filter string to C# Dynamic LINQ.
    /// <paramref name="scope"/> selects the column→property mapping: <c>"sm."</c> or <c>"sme."</c>.
    /// Returns <c>""</c> for empty/unconvertible input.
    /// </summary>
    public static string Convert(string sql, string scope)
    {
        if (string.IsNullOrWhiteSpace(sql)) return "";
        try
        {
            var parser = new SqlLinqParser(sql.Trim(), scope);
            var result = parser.Parse();
            return result is "$SKIP" or "$ERROR" ? "" : result;
        }
        catch
        {
            return "";
        }
    }

    // ---------------------------------------------------------------
    // Recursive-descent parser for the SQL subset we generate
    // ---------------------------------------------------------------
    private sealed class SqlLinqParser(string input, string scope)
    {
        private int _pos;

        public string Parse()
        {
            var result = ParseExpr();
            return result;
        }

        /// <summary>
        /// expr = 'NOT' '(' expr ')'
        ///      | '(' paren_body ')'
        /// </summary>
        private string ParseExpr()
        {
            SkipWs();
            if (MatchKeyword("NOT"))
            {
                SkipWs();
                if (!TryConsume('(')) return "$SKIP";
                var inner = ParseExpr();
                SkipWs();
                TryConsume(')');
                return inner is "$SKIP" or "$ERROR" ? "$SKIP" : $"!({inner})";
            }
            if (TryConsume('('))
            {
                SkipWs();
                string result;
                if (Peek() == '"')
                    result = ParseComparison();
                else
                    result = ParseBoolChain();
                SkipWs();
                TryConsume(')');
                return result;
            }
            return "$SKIP";
        }

        /// <summary>chain = expr (('AND'|'OR') expr)*</summary>
        private string ParseBoolChain()
        {
            var left = ParseExpr();
            SkipWs();
            while (PeekKeyword("AND") || PeekKeyword("OR"))
            {
                var isAnd = PeekKeyword("AND");
                MatchKeyword(isAnd ? "AND" : "OR");
                var right = ParseExpr();
                if (left is "$SKIP" or "$ERROR" || right is "$SKIP" or "$ERROR")
                    left = isAnd ? "$SKIP" : (left is "$SKIP" or "$ERROR" ? right : left);
                else
                    left = $"({left} {(isAnd ? "&&" : "||")} {right})";
                SkipWs();
            }
            return left;
        }

        /// <summary>comparison = "col" (IS [NOT] NULL | GLOB pattern | op literal)</summary>
        private string ParseComparison()
        {
            var col = ParseQuotedIdentifier();
            var prop = MapColumn(col);
            SkipWs();

            // IS NULL / IS NOT NULL
            if (PeekKeyword("IS"))
            {
                MatchKeyword("IS");
                SkipWs();
                var neg = MatchKeyword("NOT");
                SkipWs();
                MatchKeyword("NULL");
                return neg ? $"{prop} != null" : $"{prop} == null";
            }

            // GLOB 'pattern'
            if (PeekKeyword("GLOB"))
            {
                MatchKeyword("GLOB");
                SkipWs();
                var pattern = ParseSqlString();
                return ConvertGlob(prop, pattern);
            }

            // Standard comparison operator
            var sqlOp = ParseOperator();
            SkipWs();
            var literal = ParseLiteral();
            var csOp = sqlOp switch { "=" => "==", "<>" => "!=", _ => sqlOp };
            return $"({prop} {csOp} {literal})";
        }

        // ---------------------------------------------------------------
        // GLOB → StartsWith / EndsWith / Contains
        // ---------------------------------------------------------------
        private static string ConvertGlob(string prop, string pattern)
        {
            var startsWithStar = pattern.StartsWith('*');
            var endsWithStar   = pattern.EndsWith('*');
            if (startsWithStar && endsWithStar && pattern.Length > 2)
                return $"{prop}.Contains(\"{EscCs(pattern[1..^1])}\")";
            if (endsWithStar)
                return $"{prop}.StartsWith(\"{EscCs(pattern[..^1])}\")";
            if (startsWithStar)
                return $"{prop}.EndsWith(\"{EscCs(pattern[1..])}\")";
            // exact GLOB without wildcards — treat as equality
            return $"({prop} == \"{EscCs(pattern)}\")";
        }

        // ---------------------------------------------------------------
        // SQL column → C# property name
        // ---------------------------------------------------------------
        private string MapColumn(string col) => scope switch
        {
            "sm." => col switch
            {
                "Identifier" => "identifier",
                "IdShort"    => "idShort",
                "Category"   => "category",
                "SemanticId" => "semanticId",
                _ => col
            },
            "sme." => col switch
            {
                "IdShort"     => "idShort",
                "IdShortPath" => "idShortPath",
                "Category"    => "category",
                "SemanticId"  => "semanticId",
                "TValue"      => "TValue",
                _ => col
            },
            _ => col
        };

        // ---------------------------------------------------------------
        // Token-level helpers
        // ---------------------------------------------------------------
        private string ParseQuotedIdentifier()
        {
            if (!TryConsume('"')) return "";
            var start = _pos;
            while (_pos < input.Length && input[_pos] != '"') _pos++;
            var name = input[start.._pos];
            TryConsume('"');
            return name;
        }

        private string ParseSqlString()
        {
            if (!TryConsume('\'')) return "";
            var sb = new System.Text.StringBuilder();
            while (_pos < input.Length)
            {
                if (input[_pos] == '\'' && _pos + 1 < input.Length && input[_pos + 1] == '\'')
                { sb.Append('\''); _pos += 2; }
                else if (input[_pos] == '\'')
                { _pos++; break; }
                else
                { sb.Append(input[_pos++]); }
            }
            return sb.ToString();
        }

        private string ParseOperator()
        {
            SkipWs();
            foreach (var op in new[] { ">=", "<=", "<>", ">", "<", "=" })
            {
                if (_pos + op.Length <= input.Length && input.AsSpan(_pos, op.Length).SequenceEqual(op))
                { _pos += op.Length; return op; }
            }
            return "";
        }

        private string ParseLiteral()
        {
            SkipWs();
            if (Peek() == '\'')
            {
                var s = ParseSqlString();
                return $"\"{EscCs(s)}\"";
            }
            // Numeric literal (int, double, negative, scientific)
            var start = _pos;
            if (_pos < input.Length && input[_pos] == '-') _pos++;
            while (_pos < input.Length && (char.IsDigit(input[_pos]) || input[_pos] is '.' or 'E' or 'e' or '+' or '-'))
            {
                // allow +/- only after E/e
                if (input[_pos] is '+' or '-' && _pos > 0 && input[_pos - 1] is not ('E' or 'e'))
                    break;
                _pos++;
            }
            return input[start.._pos];
        }

        // ---------------------------------------------------------------
        // Keyword matching
        // ---------------------------------------------------------------
        private bool PeekKeyword(string kw)
        {
            SkipWs();
            if (_pos + kw.Length > input.Length) return false;
            if (string.Compare(input, _pos, kw, 0, kw.Length, StringComparison.OrdinalIgnoreCase) != 0)
                return false;
            if (_pos + kw.Length < input.Length && char.IsLetterOrDigit(input[_pos + kw.Length]))
                return false;
            return true;
        }

        private bool MatchKeyword(string kw)
        {
            if (!PeekKeyword(kw)) return false;
            _pos += kw.Length;
            return true;
        }

        private bool TryConsume(char c)
        {
            SkipWs();
            if (_pos < input.Length && input[_pos] == c) { _pos++; return true; }
            return false;
        }

        private char Peek()
        {
            SkipWs();
            return _pos < input.Length ? input[_pos] : '\0';
        }

        private void SkipWs()
        {
            while (_pos < input.Length && char.IsWhiteSpace(input[_pos])) _pos++;
        }

        private static string EscCs(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
