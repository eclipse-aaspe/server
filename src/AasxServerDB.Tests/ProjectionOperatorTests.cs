namespace AasxServerDB.Tests;

using Contracts.DbRequests;
using FluentAssertions;

/// <summary>
/// Tests der SQL-Batchprojektion (MCP-Fast-Path) gegen die importierten AASX-Testdaten:
/// exakte idShortPath-Treffer im Treffer-Submodel, Cross-Submodel-Auflösung über die AAS
/// und Not-Found-Verhalten. Die Erwartungswerte werden datengetrieben aus denselben
/// Tabellen (SMSets/SMESets/ValueSets) ermittelt.
/// </summary>
[Collection(DatabaseFixture.Collection)]
public sealed class ProjectionOperatorTests
{
    private readonly DatabaseFixture _fixture;

    public ProjectionOperatorTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private sealed record PropCandidate(string SubmodelIdentifier, string IdShortPath, string SValue);

    private static PropCandidate FirstDottedProperty(AasContext db, int? smId = null)
    {
        var candidate = (from sme in db.SMESets
                         join v in db.ValueSets on sme.Id equals v.SMEId
                         join sm in db.SMSets on sme.SMId equals sm.Id
                         where sme.SMEType == "Prop"
                             && sme.IdShortPath != null && sme.IdShortPath.Contains(".")
                             && v.SValue != null && v.SValue != ""
                             && sm.Identifier != null
                             && (smId == null || sme.SMId == smId)
                         orderby sme.Id
                         select new { sm.Identifier, sme.IdShortPath, v.SValue })
            .FirstOrDefault();

        candidate.Should().NotBeNull("the imported test data should contain a nested string property");
        return new PropCandidate(candidate!.Identifier!, candidate.IdShortPath!, candidate.SValue!);
    }

    [Fact]
    public void Project_SameSubmodelFullPath_ReturnsStoredValue()
    {
        using var db = _fixture.CreateDbContext();
        var candidate = FirstDottedProperty(db);

        var rows = ProjectionOperator.Project(db, new DbProjectionRequest
        {
            SubmodelIdentifiers = [candidate.SubmodelIdentifier],
            Paths = [new DbProjectionPath { RawPath = candidate.IdShortPath, ElementIdShortPath = candidate.IdShortPath }],
        });

        rows.Should().HaveCount(1);
        var cell = rows[0].Cells[candidate.IdShortPath];
        cell.Found.Should().BeTrue();
        cell.SmeType.Should().Be("Prop");
        cell.SourceSubmodelIdentifier.Should().Be(candidate.SubmodelIdentifier);
        cell.Values.Should().Contain(v => v.SValue == candidate.SValue);
    }

    [Fact]
    public void Project_AbsolutePathToHitSubmodel_ReturnsStoredValue()
    {
        using var db = _fixture.CreateDbContext();
        var candidate = FirstDottedProperty(db);
        var hitSubmodel = db.SMSets.Single(sm => sm.Identifier == candidate.SubmodelIdentifier);
        hitSubmodel.IdShort.Should().NotBeNullOrWhiteSpace();

        var rawPath = "/" + hitSubmodel.IdShort + "/" + candidate.IdShortPath;
        var rows = ProjectionOperator.Project(db, new DbProjectionRequest
        {
            SubmodelIdentifiers = [candidate.SubmodelIdentifier],
            Paths =
            [
                new DbProjectionPath
                {
                    RawPath = rawPath,
                    TargetSubmodelIdShort = hitSubmodel.IdShort,
                    ElementIdShortPath = candidate.IdShortPath,
                },
            ],
        });

        rows.Should().HaveCount(1);
        var cell = rows[0].Cells[rawPath];
        cell.Found.Should().BeTrue();
        cell.SmeType.Should().Be("Prop");
        cell.SourceSubmodelIdentifier.Should().Be(candidate.SubmodelIdentifier);
        cell.Values.Should().Contain(v => v.SValue == candidate.SValue);
    }

    [Fact]
    public void Project_CrossSubmodelPath_ResolvesSiblingOfSameAas()
    {
        using var db = _fixture.CreateDbContext();

        // Ein Treffer-Submodel plus ein Geschwister-Submodel derselben AAS (verknüpft über die
        // Shell-Referenzen in SMRefSets — in den Testdaten ist SMSets.AASId nicht gesetzt),
        // dessen IdShort innerhalb der AAS eindeutig ist und das ein verschachteltes
        // String-Property enthält.
        var pair = (from hitRef in db.SMRefSets
                    join siblingRef in db.SMRefSets on hitRef.AASId equals siblingRef.AASId
                    where hitRef.AASId != null && hitRef.Id != siblingRef.Id
                        && hitRef.Identifier != null && siblingRef.Identifier != null
                    join sibling in db.SMSets on siblingRef.Identifier equals sibling.Identifier
                    where sibling.IdShort != null
                    select new { HitIdentifier = hitRef.Identifier, hitRef.AASId, SiblingId = sibling.Id, SiblingIdShort = sibling.IdShort })
            .ToList()
            .FirstOrDefault(p =>
            {
                var refIdentifiers = db.SMRefSets
                    .Where(r => r.AASId == p.AASId && r.Identifier != null)
                    .Select(r => r.Identifier!)
                    .ToList();
                return db.SMSets.Count(sm => sm.Identifier != null && refIdentifiers.Contains(sm.Identifier) && sm.IdShort == p.SiblingIdShort) == 1
                    && db.SMESets.Any(sme => sme.SMId == p.SiblingId && sme.SMEType == "Prop"
                        && sme.IdShortPath != null && sme.IdShortPath.Contains(".")
                        && sme.ValueSets.Any(v => v.SValue != null && v.SValue != ""));
            });

        pair.Should().NotBeNull("the test data should contain an AAS with at least two submodels");
        var target = FirstDottedProperty(db, pair!.SiblingId);

        var rawPath = "/" + pair.SiblingIdShort + "/" + target.IdShortPath;
        var rows = ProjectionOperator.Project(db, new DbProjectionRequest
        {
            SubmodelIdentifiers = [pair.HitIdentifier!],
            Paths =
            [
                new DbProjectionPath
                {
                    RawPath = rawPath,
                    TargetSubmodelIdShort = pair.SiblingIdShort,
                    ElementIdShortPath = target.IdShortPath,
                },
            ],
        });

        rows.Should().HaveCount(1);
        var cell = rows[0].Cells[rawPath];
        cell.Found.Should().BeTrue();
        cell.SourceSubmodelIdentifier.Should().Be(target.SubmodelIdentifier);
        cell.Values.Should().Contain(v => v.SValue == target.SValue);
    }

    [Fact]
    public void Project_UnknownPathOrSubmodel_YieldsNotFoundCells()
    {
        using var db = _fixture.CreateDbContext();
        var candidate = FirstDottedProperty(db);

        var rows = ProjectionOperator.Project(db, new DbProjectionRequest
        {
            SubmodelIdentifiers = [candidate.SubmodelIdentifier, "urn:does:not:exist"],
            Paths =
            [
                new DbProjectionPath { RawPath = "No.Such.Path", ElementIdShortPath = "No.Such.Path" },
                new DbProjectionPath
                {
                    RawPath = "/NoSuchSubmodel/Some.Path",
                    TargetSubmodelIdShort = "NoSuchSubmodel",
                    ElementIdShortPath = "Some.Path",
                },
            ],
        });

        rows.Should().HaveCount(2);
        rows[0].SubmodelIdentifier.Should().Be(candidate.SubmodelIdentifier);
        rows[0].Cells["No.Such.Path"].Found.Should().BeFalse();
        rows[0].Cells["/NoSuchSubmodel/Some.Path"].Found.Should().BeFalse();
        rows[1].Cells["No.Such.Path"].Found.Should().BeFalse();
    }

    [Fact]
    public void Project_MultipleIdentifiers_PreservesRequestOrder()
    {
        using var db = _fixture.CreateDbContext();
        var identifiers = db.SMSets
            .Where(sm => sm.Identifier != null)
            .OrderBy(sm => sm.Id)
            .Select(sm => sm.Identifier!)
            .Take(3)
            .ToList();
        identifiers.Should().NotBeEmpty();
        identifiers.Reverse();

        var rows = ProjectionOperator.Project(db, new DbProjectionRequest
        {
            SubmodelIdentifiers = identifiers,
            Paths = [new DbProjectionPath { RawPath = "A.B", ElementIdShortPath = "A.B" }],
        });

        rows.Select(r => r.SubmodelIdentifier).Should().ContainInOrder(identifiers);
    }
}
