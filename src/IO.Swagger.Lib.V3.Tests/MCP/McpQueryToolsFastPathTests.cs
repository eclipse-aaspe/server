namespace IO.Swagger.Lib.V3.Tests.MCP;

using Contracts.DbRequests;
using IO.Swagger.Lib.V3.MCP;
using System.Text.Json.Nodes;

public class McpQueryToolsFastPathTests
{
    [Fact]
    public void TryPlanFastProjection_AcceptsFullDottedPaths()
    {
        var planned = McpQueryTools.TryPlanFastProjection(
            new[] { "GeneralInformation.ManufacturerArticleNumber", "TechnicalProperties.Manufacturer.Product_type" },
            out var plan);

        Assert.True(planned);
        Assert.Equal(2, plan.Count);
        Assert.Null(plan[0].TargetSubmodelIdShort);
        Assert.Equal("GeneralInformation.ManufacturerArticleNumber", plan[0].ElementIdShortPath);
        Assert.Equal("GeneralInformation.ManufacturerArticleNumber", plan[0].RawPath);
    }

    [Fact]
    public void TryPlanFastProjection_AcceptsCrossSubmodelPathsWithDot()
    {
        var planned = McpQueryTools.TryPlanFastProjection(
            new[] { "/TechnicalData/GeneralInformation.ManufacturerArticleNumber" },
            out var plan);

        Assert.True(planned);
        var path = Assert.Single(plan);
        Assert.Equal("TechnicalData", path.TargetSubmodelIdShort);
        Assert.Equal("GeneralInformation.ManufacturerArticleNumber", path.ElementIdShortPath);
        Assert.Equal("/TechnicalData/GeneralInformation.ManufacturerArticleNumber", path.RawPath);
    }

    [Fact]
    public void TryPlanFastProjection_AcceptsCrossSubmodelLeafPaths()
    {
        var planned = McpQueryTools.TryPlanFastProjection(
            new[] { "/Nameplate/ManufacturerName" },
            out var plan);

        Assert.True(planned);
        var path = Assert.Single(plan);
        Assert.Equal("Nameplate", path.TargetSubmodelIdShort);
        Assert.Equal("ManufacturerName", path.ElementIdShortPath);
        Assert.Equal("/Nameplate/ManufacturerName", path.RawPath);
    }

    [Theory]
    [InlineData("flowMax")] // Blattname ohne Punkt -> Ranking-Fallback
    [InlineData("Documents[0].Title")] // Indexpfad ist in SMESets.IdShortPath nicht adressierbar
    [InlineData("/TechnicalData")] // unvollständiger Cross-Pfad
    public void TryPlanFastProjection_RejectsNonFullPaths(string path)
    {
        Assert.False(McpQueryTools.TryPlanFastProjection(new[] { path }, out _));
    }

    [Fact]
    public void TryPlanFastProjection_RejectsMixedSelectListEntirely()
    {
        // Ein einziger Blattname im select deaktiviert den Fast Path komplett,
        // damit alle Zellen konsistent über den Objektpfad aufgelöst werden.
        var planned = McpQueryTools.TryPlanFastProjection(
            new[] { "GeneralInformation.ManufacturerArticleNumber", "flowMax" },
            out _);

        Assert.False(planned);
    }

    [Fact]
    public void TryPlanFastProjection_RejectsEmptySelect()
    {
        Assert.False(McpQueryTools.TryPlanFastProjection(null, out _));
        Assert.False(McpQueryTools.TryPlanFastProjection(new[] { " " }, out _));
    }

    [Theory]
    [InlineData("Prop", "Prop")]
    [InlineData("In-Prop", "Prop")]
    [InlineData("Out-MLP", "MLP")]
    [InlineData(null, "")]
    public void NormalizeSmeType_StripsOperationPrefix(string? smeType, string expected)
    {
        Assert.Equal(expected, McpQueryTools.NormalizeSmeType(smeType));
    }

    [Fact]
    public void IsFastProjectableCell_OnlyForPropertyAndMlp()
    {
        Assert.True(McpQueryTools.IsFastProjectableCell(new DbProjectionCell { Found = true, SmeType = "Prop" }));
        Assert.True(McpQueryTools.IsFastProjectableCell(new DbProjectionCell { Found = true, SmeType = "MLP" }));
        Assert.False(McpQueryTools.IsFastProjectableCell(new DbProjectionCell { Found = true, SmeType = "SMC" }));
        Assert.False(McpQueryTools.IsFastProjectableCell(new DbProjectionCell { Found = true, SmeType = "File" }));
        Assert.False(McpQueryTools.IsFastProjectableCell(new DbProjectionCell { Found = false, SmeType = "Prop" }));
        Assert.False(McpQueryTools.IsFastProjectableCell(null));
    }

    [Fact]
    public void ProjectFastCellValue_ReturnsStringPropertyValue()
    {
        var cell = new DbProjectionCell
        {
            Found = true,
            SmeType = "Prop",
            TValue = "S",
            Values = { new DbProjectionValue { SValue = "4711" } },
        };

        var value = McpQueryTools.ProjectFastCellValue(cell, "en");

        Assert.Equal("4711", value?.GetValue<string>());
    }

    [Fact]
    public void ProjectFastCellValue_ReturnsNumericPropertyValueAsNumber()
    {
        var cell = new DbProjectionCell
        {
            Found = true,
            SmeType = "Prop",
            TValue = "D",
            Values = { new DbProjectionValue { NValue = 80.5 } },
        };

        var value = McpQueryTools.ProjectFastCellValue(cell, "en");

        Assert.Equal(80.5, value?.GetValue<double>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("S")]
    public void ProjectFastCellValue_ReturnsNumericValueEvenWhenTValueIsNotNumeric(string? tValue)
    {
        var cell = new DbProjectionCell
        {
            Found = true,
            SmeType = "Prop",
            TValue = tValue,
            Values = { new DbProjectionValue { NValue = 9.002120415 } },
        };

        var value = McpQueryTools.ProjectFastCellValue(cell, "en");

        Assert.Equal(9.002120415, value?.GetValue<double>());
    }

    [Fact]
    public void ProjectFastCellValue_PicksRequestedLanguageForMlp()
    {
        var cell = new DbProjectionCell
        {
            Found = true,
            SmeType = "MLP",
            TValue = "S",
            Values =
            {
                new DbProjectionValue { SValue = "Ventil", Annotation = "de" },
                new DbProjectionValue { SValue = "Valve", Annotation = "en" },
            },
        };

        Assert.Equal("Valve", McpQueryTools.ProjectFastCellValue(cell, "en")?.GetValue<string>());
        Assert.Equal("Ventil", McpQueryTools.ProjectFastCellValue(cell, "de")?.GetValue<string>());

        // Unbekannte Sprache -> erste vorhandene.
        Assert.Equal("Ventil", McpQueryTools.ProjectFastCellValue(cell, "fr")?.GetValue<string>());
    }

    [Fact]
    public void ProjectFastCellValue_ReturnsNullWithoutValues()
    {
        var cell = new DbProjectionCell { Found = true, SmeType = "Prop", TValue = "S" };

        Assert.Null(McpQueryTools.ProjectFastCellValue(cell, "en"));
    }
}
