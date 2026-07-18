namespace AasxServerDB.Tests;

using AasxServerDB.Entities;
using Contracts;
using Contracts.QueryResult;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

[Collection(DatabaseFixture.Collection)]
public sealed class SqliteTrigramIndexTests
{
    private readonly DatabaseFixture _fixture;

    public SqliteTrigramIndexTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Initialize_CreatesFtsTableAndMaintenanceTriggers()
    {
        using var db = _fixture.CreateDbContext();

        // A second initialization must be cheap and idempotent.
        SqliteTrigramIndex.Initialize(db);

        using var command = db.Database.GetDbConnection().CreateCommand();
        db.Database.OpenConnection();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE name IN (
                'ValueSets_fts', 'ValueSets_fts_ai', 'ValueSets_fts_ad', 'ValueSets_fts_au',
                'SMESets_fts', 'SMESets_fts_ai', 'SMESets_fts_ad', 'SMESets_fts_au'
            )
            """;

        Convert.ToInt32(command.ExecuteScalar()).Should().Be(8);
    }

    [Fact]
    public void ApplySqliteTrigramIndex_AddsCandidateFilterForContains()
    {
        const string input = "SELECT * FROM ValueSets AS v WHERE v.\"SValue\" GLOB '*Netzteil*'";

        var result = Query.ApplySqliteTrigramIndex(input);

        result.Should().Contain("v.\"Id\" IN");
        result.Should().Contain("FROM \"ValueSets_fts\"");
        result.Should().Contain("v.\"SValue\" GLOB '*Netzteil*'");
    }

    [Fact]
    public void ApplySqliteTrigramIndex_AddsIdShortCandidateFilterForContains()
    {
        const string input =
            "SELECT * FROM SMESets AS sme WHERE \"sme\".\"IdShort\" GLOB '*output_power*'";

        var result = Query.ApplySqliteTrigramIndex(input);

        result.Should().Contain("\"sme\".\"Id\" IN");
        result.Should().Contain("FROM \"SMESets_fts\"");
        result.Should().Contain("\"IdShort\" GLOB '*output_power*'");
    }

    [Fact]
    public void ApplySqliteTrigramIndex_SkipsPatternsInSkipSet()
    {
        const string input = "SELECT * FROM ValueSets AS v WHERE v.\"SValue\" GLOB '*Netzteil*'";
        var skip = new HashSet<string>(StringComparer.Ordinal) { "'*Netzteil*'" };

        var result = Query.ApplySqliteTrigramIndex(input, skip);

        result.Should().NotContain("ValueSets_fts", "patterns in the skip set must not get an FTS candidate filter");
        result.Should().Contain("v.\"SValue\" GLOB '*Netzteil*'", "the original GLOB stays unchanged");
    }

    [Theory]
    [InlineData("v.\"SValue\" GLOB '*Phoenix*'", true)]      // single indexable contains
    [InlineData("(v.\"SValue\" GLOB '*Phoenix*')", true)]    // wrapped in parens
    [InlineData("v.\"SValue\" GLOB '*Phoenix*' AND v.\"NValue\" = 5", false)] // compound (extra selectivity)
    [InlineData("v.\"SValue\" GLOB 'Phoenix*'", false)]      // prefix, not a trigram contains
    [InlineData("v.\"SValue\" = '2500'", false)]             // equality, no contains
    [InlineData("v.\"SValue\" GLOB '*ab*'", false)]          // fewer than 3 literal chars
    public void TryGetPureSValueContainsPattern_DetectsSingleIndexableContains(string predicate, bool expected)
    {
        Query.TryGetPureSValueContainsPattern(predicate, out _).Should().Be(expected);
    }

    [Fact]
    public void BuildRawSql_UsesUncorrelatedCandidateSetForIndexedValueContains()
    {
        var conditions = new SqlConditions();
        conditions.FormulaConditions["all"] = "$$exists0$$";
        conditions.ExistsConditions.Add(new ExistsCondition
        {
            Placeholder = "exists0",
            PredicateSql =
                "((\"v\".\"SValue\" GLOB '*power supply*') AND " +
                "(\"v\".\"SValue\" GLOB '*24*'))"
        });

        var rawSql = Query.BuildRawSqlFromSqlConditions(
            conditions,
            isWithAASTable: false,
            ResultType.Submodel,
            pageFrom: 0,
            pageSize: 50)!;
        var sqliteSql = Query.ApplySqliteTrigramIndex(rawSql);

        sqliteSql.Should().Contain("sm.Id IN (");
        sqliteSql.Should().Contain("FROM ValueSets v");
        sqliteSql.Should().Contain("CROSS JOIN SMESets sme_value");
        sqliteSql.Should().Contain("FROM \"ValueSets_fts\"");
        sqliteSql.Should().NotContain("WHERE sme_value.SMId = sm.Id");
    }

    [Fact]
    public void ApplySqliteIndexedPathJoinOrder_DrivesPathSearchFromValueCandidates()
    {
        const string input = """
            SELECT sme.SMId
            FROM SMESets sme
            LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v."SValue" GLOB '*power*')
            WHERE (v."SValue" GLOB '*power*')
            AND "sme"."IdShort" = 'ManufacturerProductDesignation'
            """;

        var reordered = Query.ApplySqliteIndexedPathJoinOrder(input);
        var indexed = Query.ApplySqliteTrigramIndex(reordered);

        indexed.Should().Contain("FROM ValueSets v");
        indexed.Should().Contain("CROSS JOIN SMESets sme");
        indexed.Should().Contain("WHERE sme.Id = v.SMEId");
        (indexed.Split("v.\"SValue\" GLOB '*power*'", StringSplitOptions.None).Length - 1)
            .Should().Be(1);
        indexed.Should().Contain("FROM \"ValueSets_fts\"");
    }

    [Fact]
    public void ApplySqliteIndexedPathJoinOrder_DrivesPrefixSearchFromValueIndex()
    {
        const string input = """
            SELECT sme.SMId
            FROM SMESets sme
            LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v."SValue" GLOB 'TRIO-*')
            WHERE (v."SValue" GLOB 'TRIO-*')
            AND "sme"."IdShort" = 'Product_type'
            """;

        var reordered = Query.ApplySqliteIndexedPathJoinOrder(input);
        var indexed = Query.ApplySqliteTrigramIndex(reordered);

        indexed.Should().Contain("FROM ValueSets v");
        indexed.Should().Contain("CROSS JOIN SMESets sme");
        indexed.Should().Contain("WHERE sme.Id = v.SMEId");
        (indexed.Split("v.\"SValue\" GLOB 'TRIO-*'", StringSplitOptions.None).Length - 1)
            .Should().Be(1);
        indexed.Should().NotContain("ValueSets_fts");
    }

    [Fact]
    public void ApplySqliteTrigramIndex_AcceleratesSuffixSearch()
    {
        const string input = """
            SELECT sme.SMId
            FROM SMESets sme
            LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v."SValue" GLOB '*supply')
            WHERE (v."SValue" GLOB '*supply')
            """;

        var reordered = Query.ApplySqliteIndexedPathJoinOrder(input);
        var indexed = Query.ApplySqliteTrigramIndex(reordered);

        indexed.Should().Contain("FROM ValueSets v");
        indexed.Should().Contain("CROSS JOIN SMESets sme");
        indexed.Should().Contain("FROM \"ValueSets_fts\"");
        indexed.Should().Contain("\"SValue\" GLOB '*supply'");
    }

    [Fact]
    public void BuildRawSql_SinglePositivePathDoesNotScanOuterSubmodels()
    {
        var conditions = new SqlConditions();
        conditions.FormulaConditions["all"] = "$$path0$$";
        conditions.Paths.Add(new PathJoin
        {
            Placeholder = "path0",
            IdShortPath = "Product_type",
            SubquerySql = "FROM SMESets sme\r\n" +
                "LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v.\"SValue\" GLOB '*24DC/40*')\r\n" +
                "WHERE (v.\"SValue\" GLOB '*24DC/40*')\r\n" +
                "AND \"sme\".\"IdShort\" = 'Product_type'"
        });

        var rawSql = Query.BuildRawSqlFromSqlConditions(
            conditions,
            isWithAASTable: false,
            ResultType.Submodel,
            pageFrom: 0,
            pageSize: 200)!;
        var indexed = Query.ApplySqliteTrigramIndex(
            Query.ApplySqliteIndexedPathJoinOrder(rawSql));

        indexed.Should().Contain("SELECT p1.SMId AS Id");
        indexed.Should().Contain("FROM ValueSets v");
        indexed.Should().Contain("FROM \"ValueSets_fts\"");
        indexed.Should().NotContain("FROM SMSets");
        indexed.Should().NotContain("LEFT JOIN(\r\nSELECT sme.SMId");
    }

    [Fact]
    public void BuildRawSql_PositivePathConjunctionIntersectsPathResultsDirectly()
    {
        var conditions = new SqlConditions();
        conditions.FormulaConditions["all"] = "($$path0$$ AND $$path1$$)";
        conditions.Paths.Add(new PathJoin
        {
            Placeholder = "path0",
            IdShortPath = "Product_type",
            SubquerySql = "FROM SMESets sme\r\nWHERE \"sme\".\"IdShort\" = 'Product_type'"
        });
        conditions.Paths.Add(new PathJoin
        {
            Placeholder = "path1",
            IdShortPath = "Power_output",
            SubquerySql = "FROM SMESets sme\r\nWHERE \"sme\".\"IdShort\" = 'Power_output'"
        });

        var rawSql = Query.BuildRawSqlFromSqlConditions(
            conditions,
            isWithAASTable: false,
            ResultType.Submodel,
            pageFrom: 0,
            pageSize: 200)!;

        rawSql.Should().Contain("p1 AS (");
        rawSql.Should().Contain("p2 AS (");
        rawSql.Should().Contain("INNER JOIN p2 ON p2.SMId = p1.SMId");
        rawSql.Should().NotContain("FROM SMSets");
    }

    [Fact]
    public void BuildRawSql_PositiveAndOrPathsUseJoinAndUnionWithoutSubmodelScan()
    {
        var conditions = new SqlConditions();
        conditions.FormulaConditions["all"] = "($$path0$$ AND ($$path1$$ OR $$path2$$))";
        for (var i = 0; i < 3; i++)
        {
            conditions.Paths.Add(new PathJoin
            {
                Placeholder = $"path{i}",
                IdShortPath = $"Field{i}",
                SubquerySql = $"FROM SMESets sme\r\nWHERE \"sme\".\"IdShort\" = 'Field{i}'"
            });
        }

        var rawSql = Query.BuildRawSqlFromSqlConditions(
            conditions,
            isWithAASTable: false,
            ResultType.Submodel,
            pageFrom: 0,
            pageSize: 50)!;

        rawSql.Should().Contain("INNER JOIN p2 ON p2.SMId = p1.SMId");
        rawSql.Should().Contain("UNION");
        rawSql.Should().Contain("INNER JOIN p3 ON p3.SMId = p1.SMId");
        rawSql.Should().NotContain("FROM SMSets");
        rawSql.Should().NotContain("LEFT JOIN(");
    }

    [Fact]
    public void BuildRawSql_ShellPathSearchStartsAtMatchingSubmodels()
    {
        var conditions = new SqlConditions();
        conditions.FormulaConditions["all"] = "$$path0$$";
        conditions.Paths.Add(new PathJoin
        {
            Placeholder = "path0",
            IdShortPath = "Manufacturer_product_designation",
            SubquerySql = "FROM SMESets sme\r\n" +
                "LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v.\"SValue\" GLOB '*Power supply*')\r\n" +
                "WHERE (v.\"SValue\" GLOB '*Power supply*')"
        });

        var rawSql = Query.BuildRawSqlFromSqlConditions(
            conditions,
            isWithAASTable: true,
            ResultType.AssetAdministrationShell,
            pageFrom: 0,
            pageSize: 500)!;
        var indexed = Query.ApplySqliteTrigramIndex(
            Query.ApplySqliteIndexedPathJoinOrder(rawSql));

        indexed.Should().Contain("FROM matching_sm match");
        indexed.Should().Contain("INNER JOIN SMSets sm ON sm.Id = match.Id");
        indexed.Should().Contain("INNER JOIN SMRefSets sx ON sx.Identifier = sm.Identifier");
        indexed.Should().Contain("FROM \"ValueSets_fts\"");
        indexed.Should().NotContain("FROM (\r\n  SELECT Id, Identifier\r\n  FROM AASSets");
        indexed.Should().NotContain("SCAN SMSets");
    }

    [Fact]
    public void BuildRawSql_ShellValueSearchStartsAtValueCandidates()
    {
        var conditions = new SqlConditions();
        conditions.FormulaConditions["all"] = "\"value\".\"SValue\" GLOB '*Netzteil*'";

        var rawSql = Query.BuildRawSqlFromSqlConditions(
            conditions,
            isWithAASTable: true,
            ResultType.AssetAdministrationShell,
            pageFrom: 0,
            pageSize: 10)!;
        var indexed = Query.ApplySqliteTrigramIndex(rawSql);

        indexed.Should().Contain("FROM ValueSets AS value");
        indexed.Should().Contain("INNER JOIN SMESets AS sme ON sme.Id = value.SMEId");
        indexed.Should().Contain("INNER JOIN SMRefSets AS sx ON sx.Identifier = sm.Identifier");
        indexed.Should().Contain("FROM \"ValueSets_fts\"");
        indexed.Should().NotContain("FROM (\r\n  SELECT Id, Identifier\r\n  FROM AASSets");
    }

    [Fact]
    public void MaintenanceTriggers_KeepSubstringIndexCurrent()
    {
        using var db = _fixture.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var smeId = db.SMESets.Select(sme => sme.Id).First();
        var value = new ValueSet
        {
            SMEId = smeId,
            SValue = "Labornetzteil-4711",
            Annotation = "test"
        };

        db.ValueSets.Add(value);
        db.SaveChanges();
        FindIndexedRowId("*netzteil*").Should().Be(value.Id);

        value.SValue = "Trenntransformator-4711";
        db.SaveChanges();
        FindIndexedRowId("*netzteil*").Should().BeNull();
        FindIndexedRowId("*transformator*").Should().Be(value.Id);

        db.ValueSets.Remove(value);
        db.SaveChanges();
        FindIndexedRowId("*transformator*").Should().BeNull();

        transaction.Rollback();

        int? FindIndexedRowId(string pattern)
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                SELECT rowid
                FROM "ValueSets_fts"
                WHERE rowid = $id AND "SValue" GLOB '__PATTERN__'
                """.Replace("__PATTERN__", pattern.Replace("'", "''", StringComparison.Ordinal));
            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "$id";
            idParameter.Value = value.Id;
            command.Parameters.Add(idParameter);
            var result = command.ExecuteScalar();
            return result is null ? null : Convert.ToInt32(result);
        }
    }

    [Fact]
    public void IdShortMaintenanceTrigger_KeepsSubstringIndexCurrent()
    {
        using var db = _fixture.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var sme = db.SMESets.First();
        var originalIdShort = sme.IdShort;

        sme.IdShort = "CodexUniqueOutputPower";
        db.SaveChanges();
        FindIndexedRowId("*UniqueOutput*").Should().Be(sme.Id);

        sme.IdShort = "CodexUniqueVoltage";
        db.SaveChanges();
        FindIndexedRowId("*UniqueOutput*").Should().BeNull();
        FindIndexedRowId("*UniqueVoltage*").Should().Be(sme.Id);

        transaction.Rollback();
        sme.IdShort = originalIdShort;

        int? FindIndexedRowId(string pattern)
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.Transaction = transaction.GetDbTransaction();
            command.CommandText = """
                SELECT rowid
                FROM "SMESets_fts"
                WHERE rowid = $id AND "IdShort" GLOB '__PATTERN__'
                """.Replace("__PATTERN__", pattern.Replace("'", "''", StringComparison.Ordinal));
            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "$id";
            idParameter.Value = sme.Id;
            command.Parameters.Add(idParameter);
            var result = command.ExecuteScalar();
            return result is null ? null : Convert.ToInt32(result);
        }
    }

    [Theory]
    [InlineData("'*ab*'")]
    [InlineData("'Netzteil*'")]
    [InlineData("'*Netz?eil*'")]
    public void ApplySqliteTrigramIndex_LeavesUnsupportedPatternsUnchanged(string pattern)
    {
        var input = $"SELECT * FROM ValueSets AS v WHERE v.\"SValue\" GLOB {pattern}";

        Query.ApplySqliteTrigramIndex(input).Should().Be(input);
    }

    [Fact]
    public void ApplySqliteIndexedPathJoinOrder_DrivesEqualityPathSearchFromSmeIdShort()
    {
        // Equality/range value predicates must be driven from the SMESets side (IdShort B-tree):
        // hot values like SValue='24' would otherwise drive the loop when sqlite_stat1 averages
        // underestimate them (measured 0.3 s -> 4.9 s after ANALYZE on the large corpus).
        const string input = """
            SELECT sme.SMId
            FROM SMESets sme
            LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v."SValue" = '24')
            WHERE (v."SValue" = '24')
            AND "sme"."IdShort" = 'nominal_value_1__output_voltage'
            AND 1=1
            """;

        var reordered = Query.ApplySqliteIndexedPathJoinOrder(input);

        reordered.Should().Contain("FROM SMESets sme");
        reordered.Should().Contain("CROSS JOIN ValueSets v");
        reordered.Should().Contain("WHERE v.SMEId = sme.Id");
        reordered.Should().Contain("AND \"sme\".\"IdShort\" = 'nominal_value_1__output_voltage'");
        reordered.Should().Contain("AND 1=1");
        (reordered.Split("v.\"SValue\" = '24'", StringSplitOptions.None).Length - 1)
            .Should().Be(1, "the duplicated JOIN/WHERE predicate must collapse into one");
    }

    [Fact]
    public void ApplySqliteIndexedPathJoinOrder_LeavesEqualityWithoutIdShortFilterUnchanged()
    {
        // Without an IdShort equality there is no selective SMESets entry point —
        // forcing sme-first would scan the full SMESets table.
        const string input = """
            SELECT sme.SMId
            FROM SMESets sme
            LEFT JOIN ValueSets v ON v.SMEId = sme.Id AND (v."SValue" = '24')
            WHERE (v."SValue" = '24')
            AND "sme"."IdShortPath" GLOB 'Documents[[]*[]].Title'
            """;

        Query.ApplySqliteIndexedPathJoinOrder(input).Should().Be(input);
    }
}
