/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace Contracts;

/// <summary>
/// Structured SQL conditions produced by <see cref="QueryGrammarJSON.CreateSqlConditions"/>.
/// Replaces the LINQ-string <c>conditionsExpression</c> dictionary fed into <c>CombineTablesLEFT</c>.
/// All strings are ready-to-embed SQLite SQL fragments.
/// </summary>
public class SqlConditions
{
    /// <summary>
    /// Per-scope WHERE predicates (no table prefix, used to filter individual DbSets before the main JOIN).
    /// Keys: "aas", "sm", "sme", "value".
    /// Empty string means no restriction for that scope.
    /// </summary>
    public Dictionary<string, string> ScopeFilters { get; } = new();

    /// <summary>
    /// Per-scope C# Dynamic LINQ expressions for in-memory filtering of AAS model objects.
    /// Keys: "sm." (for <c>Submodel</c>), "sme." (for <c>ISubmodelElement</c>).
    /// Populated in parallel with <see cref="ScopeFilters"/> during <c>ParseAccessRules</c>.
    /// </summary>
    public Dictionary<string, string> ScopeFiltersCSharp { get; } = new();

    /// <summary>
    /// The combined boolean condition over all scopes, with <c>$$path0$$</c>, <c>$$match0$$</c> … as
    /// placeholders for LEFT-JOIN existence checks.  References outer aliases "a" (AAS), "t" (SM).
    /// </summary>
    public string OverallCondition { get; set; } = "";

    /// <summary>
    /// Standalone path conditions (from <c>$sme.idShortPath#field</c> outside a <c>$match</c>).
    /// Paths[i] corresponds to placeholder <c>$$path{i}$$</c> in <see cref="OverallCondition"/>.
    /// </summary>
    public List<PathJoin> Paths { get; } = new();

    /// <summary>
    /// Match groups (from <c>$match</c>).  Each group owns its paths explicitly.
    /// Matches[i] corresponds to placeholder <c>$$match{i}$$</c> in <see cref="OverallCondition"/>.
    /// </summary>
    public List<MatchJoin> Matches { get; } = new();
}

/// <summary>
/// A single LEFT-JOIN subquery for one <c>$sme.idShortPath#field op value</c> path condition.
/// The subquery uses internal alias <c>sme</c> for SMESets and <c>v</c> for ValueSets.
/// Callers may rename <c>sme</c> → <c>smePath{n}</c> via string replacement.
/// </summary>
public class PathJoin
{
    /// <summary>Placeholder token used in <see cref="SqlConditions.OverallCondition"/>, e.g. <c>path0</c>.</summary>
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
/// ScopeFilters are AND-combined per key.  OverallCondition is AND-combined.
/// Security Paths/Matches are renumbered so their placeholder indices follow after the query ones
/// before being appended — this keeps every <c>$$path{n}$$</c>/<c>$$match{n}$$</c> reference
/// in the merged OverallCondition unambiguous.
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

        // --- ScopeFilters: AND per key ---
        var allKeys = query.ScopeFilters.Keys.Union(security.ScopeFilters.Keys);
        foreach (var key in allKeys)
        {
            var qVal = query.ScopeFilters.GetValueOrDefault(key, "");
            var sVal = security.ScopeFilters.GetValueOrDefault(key, "");
            merged.ScopeFilters[key] =
                !string.IsNullOrWhiteSpace(qVal) && !string.IsNullOrWhiteSpace(sVal)
                    ? $"({qVal}) AND ({sVal})"
                    : string.IsNullOrWhiteSpace(qVal) ? sVal : qVal;
        }

        // --- Renumber security placeholders ---
        int pathOffset  = query.Paths.Count;
        int matchOffset = query.Matches.Count;

        // Rewrite security OverallCondition with shifted indices
        var secOverall = security.OverallCondition;
        // Process in reverse order so "path10" isn't clobbered before "path1"
        for (int i = security.Paths.Count - 1; i >= 0; i--)
            secOverall = secOverall.Replace($"$$path{i}$$", $"$$path{i + pathOffset}$$");
        for (int i = security.Matches.Count - 1; i >= 0; i--)
            secOverall = secOverall.Replace($"$$match{i}$$", $"$$match{i + matchOffset}$$");

        // --- OverallCondition: AND-combine ---
        var qOver = query.OverallCondition;
        merged.OverallCondition =
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

        return merged;
    }

    /// <summary>
    /// OR-combines two <see cref="SqlConditions"/> — used when accumulating rules where any rule may match.
    /// ScopeFilters are OR-combined per key. OverallCondition is OR-combined.
    /// Paths/Matches are renumbered and appended (same logic as <see cref="Merge"/>).
    /// </summary>
    public static SqlConditions? OrMerge(SqlConditions? left, SqlConditions? right)
    {
        if (left == null && right == null) return null;
        if (left == null) return right;
        if (right == null) return left;

        var merged = new SqlConditions();

        // --- ScopeFilters: OR per key ---
        var allKeys = left.ScopeFilters.Keys.Union(right.ScopeFilters.Keys);
        foreach (var key in allKeys)
        {
            var lVal = left.ScopeFilters.GetValueOrDefault(key, "");
            var rVal = right.ScopeFilters.GetValueOrDefault(key, "");
            merged.ScopeFilters[key] =
                !string.IsNullOrWhiteSpace(lVal) && !string.IsNullOrWhiteSpace(rVal)
                    ? $"({lVal}) OR ({rVal})"
                    : string.IsNullOrWhiteSpace(lVal) ? rVal : lVal;
        }

        // --- Renumber right placeholders ---
        int pathOffset  = left.Paths.Count;
        int matchOffset = left.Matches.Count;

        var rightOverall = right.OverallCondition;
        for (int i = right.Paths.Count - 1; i >= 0; i--)
            rightOverall = rightOverall.Replace($"$$path{i}$$", $"$$path{i + pathOffset}$$");
        for (int i = right.Matches.Count - 1; i >= 0; i--)
            rightOverall = rightOverall.Replace($"$$match{i}$$", $"$$match{i + matchOffset}$$");

        // --- OverallCondition: OR-combine ---
        var lOver = left.OverallCondition;
        merged.OverallCondition =
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

        return merged;
    }
}

/// <summary>
/// A LEFT-JOIN subquery group for one <c>$match</c> block that checks multiple paths match in the same
/// array element.  The sub-paths are listed explicitly to make the index relationship visible.
/// </summary>
public class MatchJoin
{
    /// <summary>Placeholder token used in <see cref="SqlConditions.OverallCondition"/>, e.g. <c>match0</c>.</summary>
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
