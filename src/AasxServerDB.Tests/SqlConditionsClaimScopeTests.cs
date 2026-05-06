namespace AasxServerDB.Tests;

using System.Security.Claims;
using Contracts;
using FluentAssertions;

public sealed class SqlConditionsClaimScopeTests
{
    [Fact]
    public void CreateSqlConditions_ClaimOrSmCondition_KeepsClaimInSmScope()
    {
        var condition = new LogicalExpression
        {
            ExpressionType = "$or",
            ExpressionValue = new List<LogicalExpression>
            {
                new()
                {
                    ExpressionType = "$contains",
                    ExpressionValue = new List<LogicalExpression>
                    {
                        ClaimAttribute("token:realm_access"),
                        Str("isSuperDuperUser")
                    }
                },
                new()
                {
                    ExpressionType = "$or",
                    ExpressionValue = new List<LogicalExpression>
                    {
                        Eq(Field("$sm#idShort"), Str("Nameplate")),
                        Eq(Field("$sm#idShort"), Str("TechnicalData"))
                    }
                }
            }
        };

        var sc = QueryGrammarJSON.CreateSqlConditions(condition);

        sc.FormulaConditions["sm"].Should().Contain(SqlConditions.ClaimSentinelPrefix);
        sc.FormulaConditions["sm"].Should().Contain("\"IdShort\" = 'Nameplate'");
        sc.FormulaConditions["sm"].Should().Contain(" OR ");
        sc.FormulaConditions["aas"].Should().NotContain(SqlConditions.ClaimSentinelPrefix);
        sc.FormulaConditions["sme"].Should().NotContain(SqlConditions.ClaimSentinelPrefix);
        sc.FormulaConditions["value"].Should().NotContain(SqlConditions.ClaimSentinelPrefix);
        sc.FormulaConditionsCSharp.Should().BeEmpty(
            because: "SQL-to-LINQ must only run after per-request CLAIM substitution");
    }

    [Fact]
    public void ClaimScopeSql_AfterSubstitution_CanRefreshCSharpMirror()
    {
        var condition = new LogicalExpression
        {
            ExpressionType = "$or",
            ExpressionValue = new List<LogicalExpression>
            {
                new()
                {
                    ExpressionType = "$contains",
                    ExpressionValue = new List<LogicalExpression>
                    {
                        ClaimAttribute("token:realm_access"),
                        Str("isSuperDuperUser")
                    }
                },
                Eq(Field("$sm#idShort"), Str("Nameplate"))
            }
        };

        var sc = QueryGrammarJSON.CreateSqlConditions(condition);
        sc.SubstituteTokenClaims(new List<Claim> { new("token:realm_access", "xxx isSuperDuperUser yyy") });
        SqlConditions.RefreshFormulaConditionsCSharpFromFormulaSql(sc);

        sc.FormulaConditions["sm"].Should().NotContain(SqlConditions.ClaimSentinelPrefix);
        sc.FormulaConditionsCSharp["sm."].Should().Contain("\"xxx isSuperDuperUser yyy\".Contains(\"isSuperDuperUser\")");
        sc.FormulaConditionsCSharp["sm."].Should().Contain("idShort == \"Nameplate\"");
    }

    [Fact]
    public void CreateSqlConditions_ClaimOrSmCondition_DoesNotOverRestrictSmeScope()
    {
        var condition = new LogicalExpression
        {
            ExpressionType = "$and",
            ExpressionValue = new List<LogicalExpression>
            {
                new()
                {
                    ExpressionType = "$or",
                    ExpressionValue = new List<LogicalExpression>
                    {
                        new()
                        {
                            ExpressionType = "$contains",
                            ExpressionValue = new List<LogicalExpression>
                            {
                                ClaimAttribute("token:realm_access"),
                                Str("isSuperDuperUser")
                            }
                        },
                        Eq(Field("$sm#idShort"), Str("Nameplate"))
                    }
                },
                new()
                {
                    ExpressionType = "$or",
                    ExpressionValue = new List<LogicalExpression>
                    {
                        StartsWith(Field("$sme#idShort"), Str("General")),
                        StartsWith(Field("$sme#idShort"), Str("Manufacturer"))
                    }
                }
            }
        };

        var sc = QueryGrammarJSON.CreateSqlConditions(condition);

        sc.FormulaConditions["sme"].Should().NotContain(SqlConditions.ClaimSentinelPrefix);
        sc.FormulaConditions["sme"].Should().Contain("\"IdShort\" GLOB 'General*'");
        sc.FormulaConditions["sme"].Should().Contain("\"IdShort\" GLOB 'Manufacturer*'");
    }

    private static LogicalExpression ClaimAttribute(string claimType)
        => new()
        {
            ExpressionType = "$attribute",
            ExpressionValue = new LogicalExpression
            {
                ExpressionType = "CLAIM",
                ExpressionValue = claimType
            }
        };

    private static LogicalExpression Field(string name) => new() { ExpressionType = "$field", ExpressionValue = name };

    private static LogicalExpression Str(string value) => new() { ExpressionType = "$strVal", ExpressionValue = value };

    private static LogicalExpression Eq(LogicalExpression left, LogicalExpression right)
        => new() { ExpressionType = "$eq", ExpressionValue = new List<LogicalExpression> { left, right } };

    private static LogicalExpression StartsWith(LogicalExpression left, LogicalExpression right)
        => new() { ExpressionType = "$starts-with", ExpressionValue = new List<LogicalExpression> { left, right } };
}
