namespace AasxServerDB.Tests;

using Contracts.QueryResult;
using FluentAssertions;

[Collection(DatabaseFixture.Collection)]
public sealed class QueryTests
{
    private readonly DatabaseFixture _fixture;

    public QueryTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
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
            "https://i4d.de/T/3209510/submodel/TechnicalData",
            "https://i4d.de/T/3209510/submodel/Nameplate",
            "https://i4d.de/T/3209510/submodel/HandoverDocumentation",
            "https://i4d.de/T/3209510/submodel/CarbonFootprint",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(noSecurity: true, db, securityCondition: null,
                pageFrom: 0, pageSize: int.MaxValue,
                ResultType.Submodel, expression);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);
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
            "https://i4d.de/T/3209510/submodel/TechnicalData",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(noSecurity: true, db, securityCondition: null,
                pageFrom: 0, pageSize: int.MaxValue,
                ResultType.Submodel, expression);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);
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
            "https://i4d.de/T/3209510/submodel/TechnicalData",
        };

        using var db = _fixture.CreateDbContext();
        var result = new Query(_fixture.Grammar)
            .GetQueryData(noSecurity: true, db, securityCondition: null,
                pageFrom: 0, pageSize: int.MaxValue,
                ResultType.Submodel, expression);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);
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
            .GetQueryData(noSecurity: true, db, securityCondition: null,
                pageFrom: 0, pageSize: int.MaxValue,
                ResultType.Submodel, expression);

        result.Should().NotBeNull();
        result!.Ids.Should().BeEquivalentTo(expected);
    }
}
