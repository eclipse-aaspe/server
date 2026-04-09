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
