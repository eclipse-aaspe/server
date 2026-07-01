namespace IO.Swagger.Lib.V3.Tests.MCP;

using IO.Swagger.Lib.V3.MCP;

public class McpQueryToolsProjectionTests
{
    [Fact]
    public void NormalizeLimit_DefaultsAndCapsAtMcpPageSize()
    {
        Assert.Equal(500, McpQueryTools.NormalizeLimit(null));
        Assert.Equal(500, McpQueryTools.NormalizeLimit(0));
        Assert.Equal(25, McpQueryTools.NormalizeLimit(25));
        Assert.Equal(500, McpQueryTools.NormalizeLimit(1000));
    }

    [Fact]
    public void NormalizeProductSubmodelLimit_DefaultsAndCapsAtProductPageSize()
    {
        Assert.Equal(50, McpQueryTools.NormalizeProductSubmodelLimit(null));
        Assert.Equal(50, McpQueryTools.NormalizeProductSubmodelLimit(-1));
        Assert.Equal(10, McpQueryTools.NormalizeProductSubmodelLimit(10));
        Assert.Equal(50, McpQueryTools.NormalizeProductSubmodelLimit(100));
    }

    [Fact]
    public void NormalizeCursor_UsesZeroForInvalidOrNegativeValues()
    {
        Assert.Equal(0, McpQueryTools.NormalizeCursor(null));
        Assert.Equal(0, McpQueryTools.NormalizeCursor(""));
        Assert.Equal(0, McpQueryTools.NormalizeCursor("abc"));
        Assert.Equal(0, McpQueryTools.NormalizeCursor("-5"));
        Assert.Equal(20, McpQueryTools.NormalizeCursor("20"));
    }

    [Fact]
    public void ValidateWildcardOperatorForField_AllowsValueIdShortAndIdShortPath()
    {
        McpQueryTools.ValidateWildcardOperatorForField("contains", "sme", "value");
        McpQueryTools.ValidateWildcardOperatorForField("starts-with", "sme", "idShort");
        McpQueryTools.ValidateWildcardOperatorForField("ends-with", "sme", "idShortPath");
        McpQueryTools.ValidateWildcardOperatorForField("contains", "sme", "idShort", "TechnicalData.Power_output");
    }

    [Fact]
    public void ValidateWildcardOperatorForField_RejectsSemanticIdAndId()
    {
        var semanticId = Assert.Throws<ArgumentException>(
            () => McpQueryTools.ValidateWildcardOperatorForField("contains", "sm", "semanticId"));
        Assert.Contains("Wildcard search is not supported for field 'semanticId'", semanticId.Message);

        var id = Assert.Throws<ArgumentException>(
            () => McpQueryTools.ValidateWildcardOperatorForField("starts-with", "sm", "id"));
        Assert.Contains("Wildcard search is not supported for field 'id'", id.Message);
    }

    [Fact]
    public void SelectPreferredProjectionPath_PutsDeprioritizedBranchesLast()
    {
        var paths = new[]
        {
            "Part_relation.Associated_part.Product_type",
            "GeneralInformation.Product_type",
        };

        var result = McpQueryTools.SelectPreferredProjectionPath(paths);

        Assert.Equal("GeneralInformation.Product_type", result);
    }

    [Fact]
    public void SelectPreferredProjectionPath_UsesConfiguredPriority()
    {
        var paths = new[]
        {
            "TechnicalProperties.Product_type",
            "GeneralInformation.Product_type",
        };

        var result = McpQueryTools.SelectPreferredProjectionPath(
            paths, new[] { "TechnicalProperties", "GeneralInformation" });

        Assert.Equal("TechnicalProperties.Product_type", result);
    }

    [Fact]
    public void SelectPreferredProjectionPath_UsesShallowerPathForEqualPriority()
    {
        var paths = new[]
        {
            "Other.Nested.Product_type",
            "Other.Product_type",
        };

        var result = McpQueryTools.SelectPreferredProjectionPath(paths);

        Assert.Equal("Other.Product_type", result);
    }

    [Fact]
    public void SelectPreferredProjectionPath_MatchesWholeSegmentsOnly()
    {
        var paths = new[]
        {
            "NotAssociated_part.Product_type",
            "Associated_part.Product_type",
        };

        var result = McpQueryTools.SelectPreferredProjectionPath(paths);

        Assert.Equal("NotAssociated_part.Product_type", result);
    }

    [Fact]
    public void SelectPreferredProjectionPath_AllowsDisablingDefaultPenalty()
    {
        var paths = new[]
        {
            "Associated_part.Product_type",
            "Other.Nested.Product_type",
        };

        var result = McpQueryTools.SelectPreferredProjectionPath(
            paths, priority: Array.Empty<string>(), deprioritize: Array.Empty<string>());

        Assert.Equal("Associated_part.Product_type", result);
    }
}
