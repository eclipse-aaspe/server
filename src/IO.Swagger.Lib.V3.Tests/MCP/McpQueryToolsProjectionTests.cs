namespace IO.Swagger.Lib.V3.Tests.MCP;

using IO.Swagger.Lib.V3.MCP;

public class McpQueryToolsProjectionTests
{
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
