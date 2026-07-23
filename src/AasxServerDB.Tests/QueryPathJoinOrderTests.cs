namespace AasxServerDB.Tests;

using Contracts;
using Contracts.QueryResult;
using FluentAssertions;

/// <summary>
/// End-to-end shape check for the forced path-join order: equality/range path
/// conditions must drive from SMESets (IdShort B-tree) via CROSS JOIN, so plans
/// stay deterministic whether or not sqlite_stat1 exists (hot values like
/// SValue='24' must never become the outer loop of a nested AND arm).
/// </summary>
[Collection(DatabaseFixture.Collection)]
public sealed class QueryPathJoinOrderTests
{
    private readonly DatabaseFixture _fixture;

    public QueryPathJoinOrderTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void ThreeAndPathConditions_ForceSmeFirstJoinOrder()
    {
        const string expression = """
            {
              "Query": {
                "$select": "id",
                "$condition":
                  { "$and": [
                    { "$eq": [ { "$field": "$sme.ProductClassifications.ProductClassificationItem.ProductClassId#value" }, { "$strVal": "27-04-07-01" } ] },
                    { "$or": [ { "$eq": [ { "$field": "$sme.%.nominal_value_1__output_voltage#value" }, { "$strVal": "24" } ] },
                               { "$eq": [ { "$field": "$sme.%.nominal_value_1__output_voltage#value" }, { "$numVal": 24 } ] } ] },
                    { "$ge": [ { "$field": "$sme.%.Power_output#value" }, { "$numVal": 960 } ] }
                  ] }
              }
            }
            """;

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
        var sql = string.Join("\n", result!.Sql ?? new List<string>());

        // Every path CTE starts at SMESets and forces the ValueSets probe second.
        sql.Should().Contain("FROM SMESets sme\r\nCROSS JOIN ValueSets v");
        sql.Should().NotContain("LEFT JOIN ValueSets v ON");

        // The IdShort filters survived the rewrite (they are the outer-loop entry).
        sql.Should().Contain("\"sme\".\"IdShort\" = 'nominal_value_1__output_voltage'");
        sql.Should().Contain("\"sme\".\"IdShort\" = 'Power_output'");
        sql.Should().Contain("\"sme\".\"IdShort\" = 'ProductClassId'");
    }
}
