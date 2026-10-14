namespace AasxServerDB.Tests;

using System.Security.Claims;
using AasSecurity;
using Contracts;
using FluentAssertions;

/// <summary>
/// End-to-end behaviour of <see cref="SecurityService.GetAccessRules"/> /
/// <c>GetSqlConditions</c> when several rules match one request.
/// <para>
/// Reproduces the reported "isSuperDuperUser sees no data" case: an unconditional grant
/// (<c>FORMULA: {"$boolean": true}</c>) matches alongside rules written for other subjects, and the
/// OR-combination has to let the unconditional grant win instead of dropping it and leaving the
/// other rules' predicates as the effective filter.
/// </para>
/// The rule set is inline on purpose — src/AasxServerBlazor/accessrules.txt is edited by developers
/// and must not be a test fixture, even though these rules mirror its shapes.
/// </summary>
[Collection(AccessRulesCollection.Name)]
public sealed class SecurityAccessRuleMergeTests : IDisposable
{
    private const string SuperDuperUser = "isSuperDuperUser";
    private const string Route = "/submodels";
    private const string Read = "READ";

    private readonly AccessRulesTestHost _accessRules;
    private readonly SecurityService _service;

    public SecurityAccessRuleMergeTests()
    {
        // Construct the service first: its ctor's parseAccessRuleFile early-returns because the test
        // bin directory has no accessrules.txt, so it cannot clobber what we publish afterwards.
        _service = new SecurityService();
        _accessRules = AccessRulesTestHost.Parse(RuleSetJson);
    }

    public void Dispose() => _accessRules.Dispose();

    // ---------------------------------------------------------------------------------------------
    // Premise: rule selection itself is correct — several rules legitimately match.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void GetAccessRules_SuperUserWithTokenClaims_MatchesRealmSuperAndSubRules()
    {
        // A token: ATTRIBUTE matches on claim *presence* (the attribute's single slot already holds
        // the claim type, so it cannot carry an expected value — that test lives in FORMULA). Hence
        // the realm_access and sub rules apply to every token-authenticated user by design.
        var rules = _service.GetAccessRules(SuperDuperUser, Read, Route, Claims("someOtherRole", "someone@other.com"));

        rules.Should().NotBeNull();
        rules!.Should().HaveCount(3, because: "the realm_access, isSuperDuperUser and token:sub rules all match");
    }

    [Fact]
    public void ParsedUnconditionalRule_YieldsUnrestrictedConditions()
    {
        var rules = _service.GetAccessRules(SuperDuperUser, Read, Route, Claims("someOtherRole", "someone@other.com"));

        var unconditional = rules!.Where(r => SqlConditionsMerger.IsUnrestricted(r._formula_sqlConditions)).ToList();
        unconditional.Should().HaveCount(1, because: "FORMULA {\"$boolean\": true} places no restriction");
        unconditional[0]._filter_sqlConditions.Should().BeNull();
    }

    // ---------------------------------------------------------------------------------------------
    // The reported bug.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void GetSqlConditions_UnrestrictedRuleAmongMatches_ReturnsNull()
    {
        // Claim values are deliberately chosen so BOTH other rules evaluate to false:
        // realm_access does not contain "isSuperDuperUser" and sub does not end with "xx.com".
        // Only the unconditional isSuperDuperUser rule can grant here — so there must be no
        // row-level restriction at all.
        var conditions = _service.GetSqlConditions(SuperDuperUser, Read, Route, Claims("someOtherRole", "someone@other.com"));

        conditions.Should().BeNull(
            because: "an unconditional grant among the matching rules absorbs the other rules' predicates");
    }

    [Fact]
    public void GetSqlConditions_UnrestrictedRuleMatchesAlone_ReturnsNull()
    {
        // Same rule, no token claims at all — the single-rule path, where OrMerge never runs.
        var conditions = _service.GetSqlConditions(SuperDuperUser, Read, Route, tokenClaims: null);

        conditions.Should().BeNull();
    }

    [Fact]
    public void GetSqlConditions_RuleWithoutFormulaOrFilter_ReturnsNull()
    {
        var conditions = _service.GetSqlConditions("isAclOnlyUser", Read, Route, tokenClaims: null);

        _service.GetAccessRules("isAclOnlyUser", Read, Route, tokenClaims: null).Should().NotBeNull(
            because: "the ACL-only rule must match, otherwise this test proves nothing");
        conditions.Should().BeNull(because: "a rule with neither FORMULA nor FILTER is an unconditional grant");
    }

    // ---------------------------------------------------------------------------------------------
    // Guards against over-widening: without an unconditional grant nothing may change.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void GetSqlConditions_OnlyRestrictiveRuleMatches_StillRestricts()
    {
        var conditions = _service.GetSqlConditions("", Read, Route, Claims(realmAccess: null, sub: "someone@other.com"));

        conditions.Should().NotBeNull();
        conditions!.FormulaConditions["sm"].Should().Contain("'*xx.com'")
            .And.Contain("'Nameplate'")
            .And.Contain("'TechnicalData'");
    }

    [Fact]
    public void GetSqlConditions_TwoRestrictiveRules_OrCombinesOveralls()
    {
        var conditions = _service.GetSqlConditions("", Read, Route, Claims("someOtherRole", "someone@other.com"));

        conditions.Should().NotBeNull();
        conditions!.FormulaConditions["all"].Should().Contain(" OR ");
        // Claim sentinels are substituted after merging, so both operands are non-empty at merge
        // time and can never be mistaken for the neutral element.
        conditions.FormulaConditions["sm"].Should().Contain("someOtherRole")
            .And.Contain("'*xx.com'");
    }

    [Fact]
    public void GetSqlConditions_NotAuthenticated_SingleRestrictiveRule_Unchanged()
    {
        var conditions = _service.GetSqlConditions("isNotAuthenticated", Read, Route, tokenClaims: null);

        conditions.Should().NotBeNull();
        conditions!.FormulaConditions["sm"].Should().Contain("'Nameplate'").And.Contain("'TechnicalData'");
        conditions.FormulaConditions["sme"].Should().Contain("'General*'").And.Contain("'Manufacturer*'");
    }

    // ---------------------------------------------------------------------------------------------
    // Tree preview (TreePage.razor) — same defect class, separate code path.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void EvaluateTreeSubmodelRead_UnrestrictedRuleMatches_ReturnsTrue()
    {
        // TreePage.razor calls the tree evaluators without tokenClaims, so only the
        // isSuperDuperUser rule can match here.
        _service.EvaluateTreeSubmodelRead(SuperDuperUser, Submodel("SomeOtherSubmodel"), Route).Should().BeTrue();
    }

    [Fact]
    public void EvaluateTreeSubmodelRead_RuleWithoutFormula_ReturnsTrue()
    {
        _service.EvaluateTreeSubmodelRead("isAclOnlyUser", Submodel("SomeOtherSubmodel"), Route).Should().BeTrue(
            because: "a rule with no FORMULA grants READ unconditionally, like FORMULA $boolean: true does");
    }

    [Fact]
    public void EvaluateTreeSubmodelElementRead_RuleWithoutFormula_ReturnsTrue()
    {
        var sm = Submodel("SomeOtherSubmodel");

        _service.EvaluateTreeSubmodelElementRead("isAclOnlyUser", sm, "SomeOtherSubmodel.SomeElement", Route)
            .Should().BeTrue();
    }

    [Fact]
    public void EvaluateTreeSubmodelRead_NotAuthenticated_StillFiltersBySubmodelIdShort()
    {
        _service.EvaluateTreeSubmodelRead("isNotAuthenticated", Submodel("Nameplate"), Route).Should().BeTrue();
        _service.EvaluateTreeSubmodelRead("isNotAuthenticated", Submodel("SomeOtherSubmodel"), Route).Should().BeFalse();
    }

    // ---------------------------------------------------------------------------------------------
    // The global accumulator has the same merge bug. It has no production consumer today; this test
    // pins the corrected semantics so nobody "fixes" it back.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void ParseAccessRules_UnconditionalRuleAmongRules_MakesGlobalAccumulatorUnrestricted()
    {
        SqlConditionsMerger.IsUnrestricted(QueryGrammarJSON.allAccessRuleSqlConditions).Should().BeTrue();
    }

    // ---------------------------------------------------------------------------------------------

    private static List<Claim> Claims(string? realmAccess, string? sub)
    {
        var claims = new List<Claim>();
        if (realmAccess != null)
        {
            claims.Add(new Claim("token:realm_access", realmAccess));
        }
        if (sub != null)
        {
            claims.Add(new Claim("token:sub", sub));
        }
        return claims;
    }

    private static AasCore.Aas3_1.Submodel Submodel(string idShort)
        => new($"https://example.com/sm/{idShort}")
        {
            IdShort = idShort,
            SubmodelElements = new List<AasCore.Aas3_1.ISubmodelElement>
            {
                new AasCore.Aas3_1.Property(AasCore.Aas3_1.DataTypeDefXsd.String) { IdShort = "SomeElement" }
            }
        };

    /// <summary>
    /// Mirrors the shapes in accessrules.txt: a claim-value rule, an unconditional role rule, a
    /// per-subject rule gated only in FORMULA, the anonymous FORMULA+FILTER rule, and an ACL-only
    /// rule with no FORMULA at all.
    /// </summary>
    private const string RuleSetJson = """
        {
          "AllAccessPermissionRules": {
            "rules": [
              {
                "ACL": {
                  "ATTRIBUTES": [ { "CLAIM": "token:realm_access" } ],
                  "RIGHTS": [ "READ" ],
                  "ACCESS": "ALLOW"
                },
                "OBJECTS": [ { "ROUTE": "*" } ],
                "FORMULA": {
                  "$contains": [
                    { "$attribute": { "CLAIM": "token:realm_access" } },
                    { "$strVal": "isSuperDuperUser" }
                  ]
                }
              },
              {
                "ACL": {
                  "ATTRIBUTES": [ { "CLAIM": "isSuperDuperUser" } ],
                  "RIGHTS": [ "READ" ],
                  "ACCESS": "ALLOW"
                },
                "OBJECTS": [ { "ROUTE": "*" } ],
                "FORMULA": { "$boolean": true }
              },
              {
                "ACL": {
                  "ATTRIBUTES": [ { "CLAIM": "token:sub" } ],
                  "RIGHTS": [ "READ" ],
                  "ACCESS": "ALLOW"
                },
                "OBJECTS": [ { "ROUTE": "/submodels" } ],
                "FORMULA": {
                  "$or": [
                    {
                      "$and": [
                        { "$ends-with": [ { "$attribute": { "CLAIM": "token:sub" } }, { "$strVal": "xx.com" } ] },
                        { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "Nameplate" } ] }
                      ]
                    },
                    {
                      "$and": [
                        { "$ends-with": [ { "$attribute": { "CLAIM": "token:sub" } }, { "$strVal": "xx.com" } ] },
                        { "$eq": [ { "$field": "$sm#idShort" }, { "$strVal": "TechnicalData" } ] }
                      ]
                    }
                  ]
                }
              },
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
              },
              {
                "ACL": {
                  "ATTRIBUTES": [ { "CLAIM": "isAclOnlyUser" } ],
                  "RIGHTS": [ "READ" ],
                  "ACCESS": "ALLOW"
                },
                "OBJECTS": [ { "ROUTE": "/submodels" } ]
              }
            ]
          }
        }
        """;
}
