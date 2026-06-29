namespace AasxServerDB.Tests;

using System.Security.Claims;
using Contracts.Pagination;
using Contracts.QueryResult;
using Contracts;
using FluentAssertions;
using Newtonsoft.Json;

/// <summary>
/// Query1–4: <c>noSecurity: true</c> (no rule merge / no in-memory security) — expected IDs match pure JSON query.
/// <see cref="ReadPagedSubmodels_NoAuthSecurityFilter_MatchesExpectedSubset"/>: loads merged access-rule <see cref="SqlConditions"/> (Formula + FILTER), like authenticated /submodels.
/// </summary>
[Collection(DatabaseFixture.Collection)]
public sealed class QueryTests
{
    private readonly DatabaseFixture _fixture;

    public QueryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static SqlConditions LoadSubmodelAccessRuleSqlConditions(string expression)
    {
        var blazorDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "AasxServerBlazor"));
        var grammar = new QueryGrammarJSON(new NoSecurityRules());
        var originalCurrentDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(blazorDir);
            grammar.ParseAccessRules(expression);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCurrentDirectory);
        }

        var matchingRules = QueryGrammarJSON._accessRules.Rules
            .Where(rule =>
                rule.Acl?.Access == "ALLOW" &&
                rule.Acl.Rights?.Contains("READ") == true &&
                rule.Acl.Attributes?.Any(a => a.ItemType == "CLAIM" && a.Value == "isNotAuthenticated") == true &&
                rule.Objects?.Any(o => o.ItemType == "ROUTE" && o.Value == "/submodels") == true)
            .ToList();

        matchingRules.Should().NotBeEmpty("the noauth /submodels rule from accessrules.txt should exist");

        SqlConditions? merged = null;
        foreach (var rule in matchingRules)
        {
            var ruleConditions = SqlConditionsMerger.Merge(rule._formula_sqlConditions, rule._filter_sqlConditions);
            if (ruleConditions == null)
                continue;

            merged = merged == null
                ? ruleConditions.Clone()
                : SqlConditionsMerger.OrMerge(merged, ruleConditions);
        }

        merged.Should().NotBeNull();
        return merged!;
    }

    // -------------------------------------------------------------------------
    // Query 1 — SMEs with any non-empty/non-zero value
    // -------------------------------------------------------------------------
    [Fact]
    public void Query1_NonEmptyValues_ReturnsExpectedIds()
    {
        const string expression = """
            {
              "Query": {
                "$select": "id",
                "$condition":
                  { "$or": [
                    { "$ne": [
                      { "$field": "$sme#value" },
                      { "$strVal": "" }
                    ] },
                    { "$ne": [
                      { "$field": "$sme#value" },
                      { "$numVal": 0 }
                    ] }
                  ] }
              }
            }
            """;

        var expected = new[]
        {
            "https://zvei.org/demo/sm/2580_0250_2022_9646",
            "https://zvei.org/demo/sm/99920200206160529000060678",
            "https://zvei.org/demo/sm/6593_0111_0112_7014",
            "https://zvei.org/demo/sm/605E831AA35645D6A194E64312AB599B",
            "https://zvei.org/demo/sm/CC46DCB43AB54ED0881CA8727928DA59",
            "http://smart.festo.com/id/instance/99920220506120448000013695",
            "http://smart.festo.com/id/instance/99920220506120451000016016",
            "http://smart.festo.com/id/instance/99920220506120519000017256",
            "http://smart.festo.com/id/instance/99920220506120552000022239",
            "https://example.com/ids/sm/dc-qr/3220_4132_1032_1386",
            "https://example.com/ids/sm/dc-qr/5560_1110_5022_5423",
            "https://example.com/ids/sm/dc-qr/7521_1110_5022_5254",
            "www.example.com/ids/sm/dc-qr/6210_3113_3022_2805",
            "www.example.com/ids/sm/dc-qr/5410_3113_3022_4726",
            "https://example.com/ids/sm/dc-qr/4565_3132_1032_2050",
            "https://i4d.de/T/2900542/submodel/TechnicalData",
            "https://i4d.de/T/2900542/submodel/Nameplate",
            "https://i4d.de/T/2900542/submodel/HandoverDocumentation",
            "https://i4d.de/T/2900542/submodel/CarbonFootprint/1B",
            "https://i.hilscher.com/00000000wln",
            "https://i.hilscher.com/00000000wlh",
            "https://i.hilscher.com/00000000w03",
            "https://i.hilscher.com/00000000wlc",
            "https://i.hilscher.com/00000000w04",
            "https://i.hilscher.com/00000000wli",
            "https://i4d.de/T/2966265/submodel/TechnicalData",
            "https://i4d.de/T/2966265/submodel/Nameplate",
            "https://i4d.de/T/2966265/submodel/HandoverDocumentation",
            "https://i4d.de/T/2966265/submodel/CarbonFootprint",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(
                noSecurity: true,
                db,
                pageFrom: 0,
                pageSize: int.MaxValue,
                ResultType.Submodel,
                expression,
                includeDebugSql: true,
                securitySqlConditions: null);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);
        result.Sql.Should().ContainSingle(sql => sql.Contains("EXISTS ("));
        result.Sql.Should().NotContain(sql => sql.Contains("LEFT JOIN(\r\n  SELECT DISTINCT\r\n    sme.SMId AS \"SMId\""));
    }

    // -------------------------------------------------------------------------
    // Query 2 — starts-with, eq (num), eq (hex) across various SMEs
    // -------------------------------------------------------------------------
    [Fact]
    public void Query2_MixedConditions_ReturnsExpectedIds()
    {
        const string expression = """
            {
              "Query": {
                  "$select": "id",
                  "$condition":
                  { "$or": [
                    { "$starts-with": [
                      { "$field": "$sme.ManufacturerName#value" },
                      { "$strVal": "ZVEI" }
                    ] },
                    { "$and": [
                      { "$eq": [
                        { "$field": "$sme.FootprintInformationModule2.CO2eq#value" },
                        { "$numVal": 103 }
                      ] },
                      { "$eq": [
                        { "$field": "$sme#value" },
                        { "$strVal": "2500" }
                      ] }
                    ] },
                    { "$and": [
                      { "$eq": [
                        { "$field": "$sme#value" },
                        { "$hexVal": "16#3DFFAF" }
                      ] }
                    ] }
                  ] }
              }
            }
            """;

        var expected = new[]
        {
            "https://zvei.org/demo/sm/2580_0250_2022_9646",
            "https://zvei.org/demo/sm/605E831AA35645D6A194E64312AB599B",
            "https://i4d.de/T/2900542/submodel/TechnicalData",
            "https://i4d.de/T/2966265/submodel/TechnicalData",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(
                noSecurity: true,
                db,
                pageFrom: 0,
                pageSize: int.MaxValue,
                ResultType.Submodel,
                expression,
                includeDebugSql: true,
                securitySqlConditions: null);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);

        // SQL shape: path conditions ($sme.ManufacturerName, $sme.FootprintInformationModule2.CO2eq)
        // must produce LEFT JOIN path subqueries, not a full-table SME scan.
        // Direct $sme#value equality conditions ('2500', hex) are selective, so they must be realized
        // as value-index-driven subqueries (sm.Id IN (...)), not a correlated EXISTS over the SM list.
        result.Sql.Should().ContainSingle();
        var sql = result.Sql[0];
        sql.Should().Contain("LEFT JOIN(", because: "path conditions must use path subquery LEFT JOINs");
        sql.Should().Contain("sm.Id IN (", because: "selective direct $sme#value (=) conditions must be value-driven, not correlated EXISTS");
        sql.Should().Contain("GLOB", because: "LIKE must be converted to GLOB for SQLite index performance");
        sql.Should().NotContain("LIKE", because: "LIKE must be converted to GLOB for SQLite index performance");
        sql.Should().NotContain("INNER JOIN SMRefSets", because: "no $aas condition in Query2 — AAS table join must be skipped");
    }

    // -------------------------------------------------------------------------
    // Query 3 — multi-branch: HandoverDocumentation (ru/Released),
    //           ProductCarbonFootprint, TechnicalData (hex)
    // -------------------------------------------------------------------------
    [Fact]
    public void Query3_MultiBranch_ReturnsExpectedIds()
    {
        const string expression = """
            {
              "Query": {
                "$select": "id",
                "$condition":
                 { "$or": [
                   { "$and": [
                     { "$boolean": true },
                     { "$eq": [
                       { "$field": "$sm#idShort" },
                       { "$strVal": "HandoverDocumentation" }
                     ] },
                     { "$eq": [
                       { "$field": "$aas#id" },
                       { "$strVal": "https://phoenixcontact.com/qr/2966265/1/aas" }
                     ] },
                     { "$match": [
                       { "$eq": [
                          { "$field": "$sme.Document%.DocumentVersion.Language%#value" },
                          { "$strVal": "ru" }
                       ] },
                       { "$eq": [
                         { "$field": "$sme.Document%.DocumentVersion.StatusValue#value" },
                         { "$strVal": "Released" }
                       ] }
                     ] }
                   ] },
                   { "$and": [
                     { "$boolean": true },
                     { "$eq": [
                       { "$field": "$sm#idShort" },
                       { "$strVal": "ProductCarbonFootprint" }
                     ] },
                     { "$eq": [
                       { "$field": "$aas#id" },
                       { "$strVal": "https://zvei.org/demo/aas/ControlCabinet" }
                     ] },
                     { "$eq": [
                       { "$field": "$sme.FootprintInformationModule2.CO2eq#value" },
                       { "$numVal": 103 }
                     ] },
                     { "$eq": [
                       { "$field": "$sme.FootprintInformationCombination1.CO2eq#value" },
                       { "$strVal": "2500" }
                     ] }
                   ] },
                   { "$and": [
                     { "$boolean": true },
                     { "$eq": [
                       { "$field": "$sm#idShort" },
                       { "$strVal": "TechnicalData" }
                     ] },
                     { "$eq": [
                       { "$field": "$sme#value" },
                       { "$hexVal": "16#3DFFAF" }
                     ] }
                   ] }
                 ] }
              }
            }
            """;

        var expected = new[]
        {
            "https://zvei.org/demo/sm/2580_0250_2022_9646",
            "https://i4d.de/T/2900542/submodel/TechnicalData",
            "https://i4d.de/T/2966265/submodel/TechnicalData",
            "https://i4d.de/T/2966265/submodel/HandoverDocumentation",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(noSecurity: true, db,
                pageFrom: 0, pageSize: int.MaxValue,
                ResultType.Submodel, expression,
                includeDebugSql: true);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);

        // SQL shape: $aas#id conditions require AAS join; $match requires a match subquery
        // with Path{N} aliases and substr() for %-wildcard segment extraction;
        // path conditions ($sme.FootprintInformationModule2.CO2eq) use LEFT JOIN path subqueries;
        // direct $sme#value (hex equality) is selective, so it uses a value-driven subquery (sm.Id IN (...)).
        result.Sql.Should().ContainSingle();
        var sql = result.Sql[0];
        sql.Should().Contain("INNER JOIN SMRefSets", because: "$aas#id conditions require the AAS→SMRefSets join");
        sql.Should().Contain("SELECT Path1.SMId AS SMId", because: "$match must produce a match subquery joining Path aliases");
        sql.Should().Contain("substr(", because: "%-wildcard $match paths must extract segments via substr()");
        sql.Should().Contain("LEFT JOIN(", because: "path conditions must use path subquery LEFT JOINs");
        sql.Should().Contain("sm.Id IN (", because: "selective direct $sme#value (hex =) must be value-driven, not correlated EXISTS");
        sql.Should().Contain("GLOB", because: "LIKE must be converted to GLOB for SQLite index performance");
        sql.Should().NotContain("LIKE", because: "LIKE must be converted to GLOB for SQLite index performance");
    }

    // -------------------------------------------------------------------------
    // Query 4 — $select: "id" with nested SML Records[]
    // -------------------------------------------------------------------------
    [Fact]
    public void Query4_MatchRecords_ReturnsExpectedIds()
    {
        const string expression = """
            {
              "Query": {
                "$select": "id",
                "$condition":
                 { "$or": [
                   { "$and": [
                     { "$match": [
                       { "$eq": [
                          { "$field": "$sme.Records[].ItemOfChange.TechnicalData_Changes[].ReasonId#value" },
                          { "$strVal": "SOFTW" }
                       ] },
                       { "$eq": [
                         { "$field": "$sme.Records[].ItemOfChange.TechnicalData_Changes[].Version#value" },
                         { "$strVal": "1.4.0.3" }
                       ] },
                       { "$ge": [
                         { "$field": "$sme.Records[].DateOfRecord#value" },
                         { "$strVal": "2024-04-24T07:53Z" }
                       ] }
                     ] }
                   ] }
                 ] }
              }
            }
            """;

        var expected = new[]
        {
            "https://i.hilscher.com/00000000wlc",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(
                noSecurity: true,
                db,
                pageFrom: 0,
                pageSize: int.MaxValue,
                ResultType.Submodel,
                expression,
                includeDebugSql: true,
                securitySqlConditions: null);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);

        // SQL shape: $match with [] array syntax must produce a match subquery that uses
        // substr()/instr() to extract the array-index segment between fixed path components,
        // and Part1_* column aliases to correlate the same array element across all paths in the match.
        result.Sql.Should().ContainSingle();
        var sql = result.Sql[0];
        sql.Should().Contain("SELECT Path1.SMId AS SMId", because: "$match must produce a match subquery with Path aliases");
        sql.Should().Contain("substr(", because: "[] array match must extract segment positions with substr()");
        sql.Should().Contain("instr(", because: "[] array match must locate segment boundaries with instr()");
        sql.Should().Contain("Part1_", because: "[] array match must use Part1_* columns to correlate array indices across paths");
        sql.Should().Contain("GLOB", because: "LIKE must be converted to GLOB for SQLite index performance");
        sql.Should().NotContain("LIKE", because: "LIKE must be converted to GLOB for SQLite index performance");
    }

    /// <summary>With security: OR-merged access rules + FILTER fragments (sme idShort), not a pure query-only condition.</summary>
    [Fact]
    public void ReadPagedSubmodels_NoAuthSecurityFilter_MatchesExpectedSubset()
    {
        const string expression = """
            {
              "AllAccessPermissionRules": {
                "rules": [
                  {
                    "ACL": {
                      "ATTRIBUTES": [
                        {
                          "CLAIM": "isNotAuthenticated"
                        }
                      ],
                      "RIGHTS": [
                        "READ"
                      ],
                      "ACCESS": "ALLOW"
                    },
                    "OBJECTS": [
                      {
                        "ROUTE": "/submodels"
                      }
                    ],
                    "FORMULA": {
                      "$or": [
                        {
                          "$and": [
                            {
                              "$eq": [
                                {
                                  "$field": "$sm#idShort"
                                },
                                {
                                  "$strVal": "Nameplate"
                                }
                              ]
                            }
                          ]
                        },
                        {
                          "$and": [
                            {
                              "$eq": [
                                {
                                  "$field": "$sm#idShort"
                                },
                                {
                                  "$strVal": "TechnicalData"
                                }
                              ]
                            }
                          ]
                        }
                      ]
                    },
                    "FILTER": {
                      "FRAGMENT": "xxx",
                      "CONDITION": {
                        "$or": [
                          {
                            "$starts-with": [
                              {
                                "$field": "$sme#idShort"
                              },
                              {
                                "$strVal": "Generalxxx"
                              }
                            ]
                          },
                          {
                            "$starts-with": [
                              {
                                "$field": "$sme#idShort"
                              },
                              {
                                "$strVal": "Manufacturer"
                              }
                            ]
                          }
                        ]
                      }
                    }
                  }
                ]
              }
            }
            """;

        var expected = new[]
        {
            "http://smart.festo.com/id/instance/99920220506120448000013695",
            "http://smart.festo.com/id/instance/99920220506120451000016016",
            "https://example.com/ids/sm/dc-qr/3220_4132_1032_1386",
            "https://example.com/ids/sm/dc-qr/5560_1110_5022_5423",
            "https://i.hilscher.com/00000000wln",
            "https://i4d.de/T/2900542/submodel/Nameplate",
            "https://i4d.de/T/2900542/submodel/TechnicalData",
            "https://i4d.de/T/2966265/submodel/Nameplate",
            "https://i4d.de/T/2966265/submodel/TechnicalData",
            "https://zvei.org/demo/sm/605E831AA35645D6A194E64312AB599B",
            "https://zvei.org/demo/sm/CC46DCB43AB54ED0881CA8727928DA59",
        };

        using var db = _fixture.CreateDbContext();

        var securitySqlConditions = LoadSubmodelAccessRuleSqlConditions(expression);
        var pagination = new PaginationParameters("0", 500);

        var result = CrudOperator.ReadPagedSubmodels(
            db,
            new Query(_fixture.Grammar),
            pagination,
            reqSemanticId: null,
            idShort: null,
            securitySqlConditions: securitySqlConditions);

        var actualIdentifiers = result
            .Select(sm => sm.Id)
            .OrderBy(id => id)
            .ToList();

        actualIdentifiers.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        actualIdentifiers.Should().NotBeEmpty();
        result.Select(sm => sm.IdShort).Distinct().Should().OnlyContain(idShort => idShort == "Nameplate" || idShort == "TechnicalData");
    }

    /// <summary>
    /// Regression for the single-submodel GET path (<see cref="CrudOperator.ReadSubmodel"/>): the
    /// element-level access-rule FILTER (sme idShort) must be applied there too, not only by the paged
    /// list path. Guards the bug where <c>ApplySmeSqlFilterConditions</c> read the always-empty
    /// <c>FilterConditions</c> bucket instead of <c>FormulaConditions</c>, so GET /submodels/{id}
    /// returned all elements unfiltered while the tree view filtered correctly.
    /// </summary>
    [Fact]
    public void ReadSubmodelById_NoAuthSecurityFilter_FiltersSubmodelElements()
    {
        const string expression = """
            {
              "AllAccessPermissionRules": {
                "rules": [
                  {
                    "ACL": {
                      "ATTRIBUTES": [ { "CLAIM": "isNotAuthenticated" } ],
                      "RIGHTS": [ "READ" ],
                      "ACCESS": "ALLOW"
                    },
                    "OBJECTS": [ { "ROUTE": "/submodels" } ],
                    "FORMULA": {
                      "$or": [
                        { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "Nameplate" } ] },
                        { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "TechnicalData" } ] }
                      ]
                    },
                    "FILTER": {
                      "FRAGMENT": "xxx",
                      "CONDITION": {
                        "$or": [
                          { "$starts-with": [ { "$field": "$sme#idShort" }, { "$strVal": "General" } ] },
                          { "$starts-with": [ { "$field": "$sme#idShort" }, { "$strVal": "Manufacturer" } ] }
                        ]
                      }
                    }
                  }
                ]
              }
            }
            """;

        const string nameplateId = "https://i4d.de/T/2900542/submodel/Nameplate";

        using var db = _fixture.CreateDbContext();
        var securitySqlConditions = LoadSubmodelAccessRuleSqlConditions(expression);

        // Baseline: full submodel without security — must contain elements the FILTER is meant to drop.
        var full = CrudOperator.ReadSubmodel(db, submodelIdentifier: nameplateId, securitySqlConditions: null, skipAllowCheck: true);
        full.Should().NotBeNull("the Nameplate submodel exists in the test database");
        var fullIdShorts = full!.SubmodelElements!.Select(e => e.IdShort).ToList();
        fullIdShorts.Should().Contain(s => !(s!.StartsWith("General") || s.StartsWith("Manufacturer")),
            "the baseline must include elements the FILTER is expected to remove");

        // With security: only top-level elements allowed by the FILTER (idShort General*/Manufacturer*) remain.
        var filtered = CrudOperator.ReadSubmodel(db, submodelIdentifier: nameplateId, securitySqlConditions: securitySqlConditions);
        filtered.Should().NotBeNull("Nameplate is allowed by the FORMULA");
        var filteredIdShorts = filtered!.SubmodelElements!.Select(e => e.IdShort).ToList();

        filteredIdShorts.Should().NotBeEmpty();
        filteredIdShorts.Should().OnlyContain(s => s!.StartsWith("General") || s.StartsWith("Manufacturer"));
        filteredIdShorts.Count.Should().BeLessThan(fullIdShorts.Count,
            "the element-level FILTER must remove non-matching elements on the single-submodel GET path");
    }

    /// <summary>
    /// $UNION / $TEMPTABLE under an access-rule FILTER: the merged overall is "(userOR) AND (ACL)",
    /// whose top level is AND. The builder must split the *user* OR and AND the ACL onto each branch
    /// (distribution), so the flagged query (a) still returns exactly the same submodels as the default
    /// LEFT-JOIN query and (b) actually emits multiple branches.
    /// </summary>
    [Theory]
    [InlineData("$UNION")]
    [InlineData("$TEMPTABLE")]
    public void UnionOrTemp_WithSecurityFilter_DistributesUserOrAndMatchesDefault(string flag)
    {
        const string securityExpression = """
            {
              "AllAccessPermissionRules": {
                "rules": [
                  {
                    "ACL": {
                      "ATTRIBUTES": [ { "CLAIM": "isNotAuthenticated" } ],
                      "RIGHTS": [ "READ" ],
                      "ACCESS": "ALLOW"
                    },
                    "OBJECTS": [ { "ROUTE": "/submodels" } ],
                    "FORMULA": {
                      "$or": [
                        { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "Nameplate" } ] },
                        { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "TechnicalData" } ] }
                      ]
                    },
                    "FILTER": {
                      "FRAGMENT": "xxx",
                      "CONDITION": {
                        "$or": [
                          { "$starts-with": [ { "$field": "$sme#idShort" }, { "$strVal": "General" } ] },
                          { "$starts-with": [ { "$field": "$sme#idShort" }, { "$strVal": "Manufacturer" } ] }
                        ]
                      }
                    }
                  }
                ]
              }
            }
            """;

        // User query with a genuine top-level $or (sm idShort), which under the ACL becomes
        // "(idShort=Nameplate OR idShort=TechnicalData) AND (ACL FORMULA) AND (ACL FILTER)".
        const string userQuery = """
            {
              "Query": {
                "$select": "id",
                "$condition": {
                  "$or": [
                    { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "Nameplate" } ] },
                    { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "TechnicalData" } ] }
                  ]
                }
              }
            }
            """;

        using var db = _fixture.CreateDbContext();
        var sec = LoadSubmodelAccessRuleSqlConditions(securityExpression);

        var def = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: false, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery, includeDebugSql: true, securitySqlConditions: sec.Clone());
        var flagged = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: false, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery + " " + flag, includeDebugSql: true, securitySqlConditions: sec.Clone());

        def.Should().NotBeNull();
        flagged.Should().NotBeNull();
        def!.Ids.Should().NotBeEmpty("the anonymous ACL exposes Nameplate/TechnicalData submodels");
        flagged!.Ids.Should().BeEquivalentTo(def.Ids, "OR distribution must preserve query semantics");

        var sql = string.Join("\n", flagged.Sql);
        if (flag == "$UNION")
            sql.Should().Contain("UNION", "the user $or must split into separate UNION branches");
        else
            System.Text.RegularExpressions.Regex.Matches(sql, "INSERT OR IGNORE INTO union_ids").Count
                .Should().BeGreaterThan(1, "the user $or must split into separate temp-table INSERT branches");
    }

    /// <summary>
    /// Without security the OR-distribution fallback must still split a fully-paren-wrapped top-level
    /// OR ("(A OR B)") via StripEnclosingParens — otherwise $UNION/$TEMPTABLE would degenerate to one
    /// monolithic branch (full SMSets scan) instead of value-/index-driven branches.
    /// </summary>
    [Theory]
    [InlineData("$UNION")]
    [InlineData("$TEMPTABLE")]
    public void UnionOrTemp_NoSecurity_DistributesTopLevelOr(string flag)
    {
        const string userQuery = """
            { "Query": { "$select": "id", "$condition": { "$or": [
              { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "Nameplate" } ] },
              { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "TechnicalData" } ] }
            ] } } }
            """;

        using var db = _fixture.CreateDbContext();
        var def = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: true, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery, includeDebugSql: true);
        var flagged = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: true, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery + " " + flag, includeDebugSql: true);

        def.Should().NotBeNull();
        flagged.Should().NotBeNull();
        flagged!.Ids.Should().BeEquivalentTo(def!.Ids, "OR distribution must preserve query semantics");

        var sql = string.Join("\n", flagged.Sql);
        if (flag == "$UNION")
            sql.Should().Contain("UNION", "a fully-wrapped top-level OR must split even without security");
        else
            System.Text.RegularExpressions.Regex.Matches(sql, "INSERT OR IGNORE INTO union_ids").Count
                .Should().BeGreaterThan(1, "a fully-wrapped top-level OR must split into temp-table branches even without security");
    }

    private const string AnonymousSubmodelsAcl = """
        {
          "AllAccessPermissionRules": {
            "rules": [
              {
                "ACL": {
                  "ATTRIBUTES": [ { "CLAIM": "isNotAuthenticated" } ],
                  "RIGHTS": [ "READ" ],
                  "ACCESS": "ALLOW"
                },
                "OBJECTS": [ { "ROUTE": "/submodels" } ],
                "FORMULA": {
                  "$or": [
                    { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "Nameplate" } ] },
                    { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "TechnicalData" } ] }
                  ]
                },
                "FILTER": {
                  "FRAGMENT": "xxx",
                  "CONDITION": {
                    "$or": [
                      { "$starts-with": [ { "$field": "$sme#idShort" }, { "$strVal": "General" } ] },
                      { "$starts-with": [ { "$field": "$sme#idShort" }, { "$strVal": "Manufacturer" } ] }
                    ]
                  }
                }
              }
            ]
          }
        }
        """;

    /// <summary>
    /// Selective direct <c>$sme#value</c> equality must be realized value-index-driven (<c>sm.Id IN (…)</c>),
    /// and the access-rule element FILTER must sit INSIDE that value match — otherwise a value in a hidden
    /// element could satisfy the query (id-only leak).
    /// </summary>
    [Fact]
    public void ValueEquality_WithSecurity_IsValueDriven_AndFilterInsideMatch()
    {
        // $and wrapping routes the value predicate through the EXISTS/value-match realization
        // (a standalone $sme#value goes through the value-join path instead).
        const string userQuery = """
            { "Query": { "$select": "id", "$condition":
              { "$and": [ { "$eq": [ { "$field": "$sme#value" }, { "$strVal": "2500" } ] } ] } } }
            """;

        using var db = _fixture.CreateDbContext();
        var sec = LoadSubmodelAccessRuleSqlConditions(AnonymousSubmodelsAcl);
        var result = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: false, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery, includeDebugSql: true, securitySqlConditions: sec);

        result.Should().NotBeNull();
        var sql = string.Join("\n", result!.Sql);
        sql.Should().Contain("sm.Id IN (", "selective '=' on $sme#value must be value-driven");
        sql.Should().NotContain("sme_value.SMId = sm.Id", "the value-driven form must not be a correlated EXISTS");
        // FILTER must be applied inside the value-driven subquery (between the IN and its General* predicate).
        sql.Should().MatchRegex(@"sm\.Id IN \([\s\S]*?GLOB 'General\*'", "the element FILTER must sit inside the value match");
    }

    /// <summary>
    /// Non-selective direct <c>$sme#value</c> (<c>&lt;&gt;</c>) must stay a correlated EXISTS (short-circuits per
    /// submodel), and the element FILTER must sit INSIDE that EXISTS.
    /// </summary>
    [Fact]
    public void ValueInequality_WithSecurity_StaysExists_AndFilterInsideMatch()
    {
        const string userQuery = """
            { "Query": { "$select": "id", "$condition":
              { "$and": [ { "$ne": [ { "$field": "$sme#value" }, { "$strVal": "" } ] } ] } } }
            """;

        using var db = _fixture.CreateDbContext();
        var sec = LoadSubmodelAccessRuleSqlConditions(AnonymousSubmodelsAcl);
        var result = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: false, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery, includeDebugSql: true, securitySqlConditions: sec);

        result.Should().NotBeNull();
        var sql = string.Join("\n", result!.Sql);
        sql.Should().Contain("sme_value.SMId = sm.Id", "non-selective '<>' must stay a correlated EXISTS");
        sql.Should().MatchRegex(@"sme_value\.SMId = sm\.Id[\s\S]*?GLOB 'General\*'", "the element FILTER must sit inside the EXISTS");
    }

    /// <summary>
    /// A standalone <c>$sme#value</c> is realized via the value-join (not the EXISTS path). The element
    /// FILTER must be applied inside that join too, otherwise a value in a hidden element could match.
    /// </summary>
    [Fact]
    public void StandaloneValue_WithSecurity_FilterInsideValueJoin()
    {
        const string userQuery = """
            { "Query": { "$select": "id", "$condition":
              { "$eq": [ { "$field": "$sme#value" }, { "$strVal": "2500" } ] } } }
            """;

        using var db = _fixture.CreateDbContext();
        var sec = LoadSubmodelAccessRuleSqlConditions(AnonymousSubmodelsAcl);
        var result = new Query(_fixture.Grammar).GetQueryData(
            noSecurity: false, db, pageFrom: 0, pageSize: int.MaxValue,
            ResultType.Submodel, userQuery, includeDebugSql: true, securitySqlConditions: sec);

        result.Should().NotBeNull();
        var sql = string.Join("\n", result!.Sql);
        // The FILTER must sit inside the value join (before its ") AS value" close).
        sql.Should().MatchRegex(@"JOIN SMESets sme ON sme\.Id = v\.SMEId[\s\S]*?GLOB 'General\*'[\s\S]*?\) AS value",
            "the element FILTER must sit inside the value join");
    }

    // -------------------------------------------------------------------------
    // Access rule with $attribute(CLAIM(token:sub)) — regression cover for the
    // CLAIM substitution that was lost when SecurityService.GetCondition was
    // dropped (commit 4bef43ac). The parser must emit a deferred sentinel,
    // and SqlConditions.SubstituteTokenClaims must replace it with the
    // request's escaped token value.
    // -------------------------------------------------------------------------
    [Fact]
    public void TokenClaim_AttributeInFormula_SubstitutesIntoSqlConditions()
    {
        const string expression = """
            {
              "AllAccessPermissionRules": {
                "rules": [
                  {
                    "ACL": {
                      "ATTRIBUTES": [ { "CLAIM": "token:sub" } ],
                      "RIGHTS": [ "READ" ],
                      "ACCESS": "ALLOW"
                    },
                    "OBJECTS": [ { "ROUTE": "/submodels" } ],
                    "FORMULA": {
                      "$and": [
                        {
                          "$ends-with": [
                            { "$attribute": { "CLAIM": "token:sub" } },
                            { "$strVal": "xx.com" }
                          ]
                        },
                        {
                          "$eq": [
                            { "$field": "$sm#idShort" },
                            { "$strVal": "Nameplate" }
                          ]
                        }
                      ]
                    }
                  }
                ]
              }
            }
            """;

        var blazorDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AasxServerBlazor"));
        var grammar = new QueryGrammarJSON(new NoSecurityRules());
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(blazorDir);
            grammar.ParseAccessRules(expression);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }

        var rule = QueryGrammarJSON._accessRules.Rules.Single();
        rule._formula_sqlConditions.Should().NotBeNull();
        var ruleSql = rule._formula_sqlConditions!;

        // Parser must emit a deferred sentinel literal — not silently drop the CLAIM check.
        ruleSql.FormulaConditions["all"].Should().Contain(
            $"'{SqlConditions.ClaimSentinelPrefix}token:sub{SqlConditions.ClaimSentinelSuffix}'",
            because: "$attribute(CLAIM(...)) must be deferred via a sentinel SQL literal, not dropped");
        ruleSql.FormulaConditions["all"].Should().Contain(
            "GLOB '*xx.com'",
            because: "$ends-with translates to a GLOB pattern with leading wildcard");

        // Cloning must isolate per-request mutations from the cached rule.
        var perRequest = ruleSql.Clone();

        var tokenClaims = new List<Claim> { new("token:sub", "andreas@xx.com") };
        perRequest.SubstituteTokenClaims(tokenClaims);

        perRequest.FormulaConditions["all"].Should().Contain("'andreas@xx.com'",
            because: "the sentinel must be replaced with the SQL-escaped claim value");
        perRequest.FormulaConditions["all"].Should().NotContain(SqlConditions.ClaimSentinelPrefix,
            because: "no $$claim:* sentinel may survive after substitution");

        // Cached rule must remain intact for the next request with different claims.
        ruleSql.FormulaConditions["all"].Should().Contain(SqlConditions.ClaimSentinelPrefix,
            because: "Clone() must protect the cached rule SqlConditions from per-request mutations");
        ruleSql.FormulaConditions["all"].Should().NotContain("'andreas@xx.com'",
            because: "Clone() must protect the cached rule SqlConditions from per-request mutations");
    }

    [Fact]
    public void TokenClaim_QuoteInValue_GetsSqlEscaped()
    {
        const string expression = """
            {
              "AllAccessPermissionRules": {
                "rules": [
                  {
                    "ACL": {
                      "ATTRIBUTES": [ { "CLAIM": "token:sub" } ],
                      "RIGHTS": [ "READ" ],
                      "ACCESS": "ALLOW"
                    },
                    "OBJECTS": [ { "ROUTE": "/submodels" } ],
                    "FORMULA": {
                      "$eq": [
                        { "$attribute": { "CLAIM": "token:sub" } },
                        { "$strVal": "any" }
                      ]
                    }
                  }
                ]
              }
            }
            """;

        var blazorDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AasxServerBlazor"));
        var grammar = new QueryGrammarJSON(new NoSecurityRules());
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(blazorDir);
            grammar.ParseAccessRules(expression);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }

        var perRequest = QueryGrammarJSON._accessRules.Rules.Single()._formula_sqlConditions!.Clone();
        perRequest.SubstituteTokenClaims(new List<Claim> { new("token:sub", "O'Connor") });

        perRequest.FormulaConditions["all"].Should().Contain("'O''Connor'",
            because: "single quotes in claim values must be SQL-escaped via doubling");
        perRequest.FormulaConditions["all"].Should().NotContain("O'Connor'",
            because: "an unescaped quote would terminate the SQL string literal early");
    }

    [Fact]
    public void TokenClaim_MissingClaim_ResolvesToEmptyLiteral()
    {
        const string expression = """
            {
              "AllAccessPermissionRules": {
                "rules": [
                  {
                    "ACL": {
                      "ATTRIBUTES": [ { "CLAIM": "token:sub" } ],
                      "RIGHTS": [ "READ" ],
                      "ACCESS": "ALLOW"
                    },
                    "OBJECTS": [ { "ROUTE": "/submodels" } ],
                    "FORMULA": {
                      "$ends-with": [
                        { "$attribute": { "CLAIM": "token:sub" } },
                        { "$strVal": "xx.com" }
                      ]
                    }
                  }
                ]
              }
            }
            """;

        var blazorDir = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AasxServerBlazor"));
        var grammar = new QueryGrammarJSON(new NoSecurityRules());
        var originalCwd = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(blazorDir);
            grammar.ParseAccessRules(expression);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
        }

        var perRequest = QueryGrammarJSON._accessRules.Rules.Single()._formula_sqlConditions!.Clone();
        // No tokenClaims — sentinel must collapse to empty literal so SQLite evaluates
        // ('' GLOB '*xx.com') = 0 (false), keeping the rule fail-closed.
        perRequest.SubstituteTokenClaims(tokenClaims: null);

        perRequest.FormulaConditions["all"].Should().Contain("('' GLOB '*xx.com')",
            because: "missing claims must produce an empty SQL literal, not leave the sentinel in place");
        perRequest.FormulaConditions["all"].Should().NotContain(SqlConditions.ClaimSentinelPrefix);
    }
}
