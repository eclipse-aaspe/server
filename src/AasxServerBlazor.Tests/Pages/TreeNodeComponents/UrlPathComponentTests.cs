using AasCore.Aas3_0;
using AasxServer;
using AasxServerBlazor.Pages.TreeNodeComponents;
using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using AutoFixture.AutoMoq;
using Bunit;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AasxServerBlazor.Tests.Pages.TreeNodeComponents;

public class UrlPathComponentTests
{
    private readonly Fixture _fixture;

    public UrlPathComponentTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Render_SelectedNode_Is_Submodel_Should_Display_Correct_URL()
    {
        // Arrange
        var selectedNode = _fixture.Create<TreeItem>();
        selectedNode.Tag = _fixture.Create<Submodel>();
        Program.externalBlazor = "http://example.com";
        var expectedIdEncoded = Base64UrlEncoder.Encode(((Submodel) selectedNode.Tag).Id);
        var expectedPath = $"http://example.com/submodels/{expectedIdEncoded}";
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<UrlPathComponent>(parameters => parameters
            .Add(p => p.SelectedNode, selectedNode));

        // Assert
        cut.Find("a").GetAttribute("href").Should().Be(expectedPath);
    }

    [Fact]
    public void Render_SelectedNode_Is_Null_Should_Not_Render_URL()
    {
        // Arrange
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<UrlPathComponent>(parameters => parameters
            .Add(p => p.SelectedNode, null));

        // Assert
        cut.FindAll("a").Should().BeEmpty();
    }

    [Fact]
    public void Render_SelectedNode_Is_Not_Submodel_Or_SubmodelElement_Should_Not_Render_URL()
    {
        // Arrange
        var selectedNode = _fixture.Create<TreeItem>();
        // Assuming SelectedNode is neither Submodel nor SubmodelElement
        selectedNode.Tag = _fixture.Create<object>();
        using var ctx = new TestContext();

        // Act
        var cut = ctx.RenderComponent<UrlPathComponent>(parameters => parameters
            .Add(p => p.SelectedNode, selectedNode));

        // Assert
        cut.FindAll("a").Should().BeEmpty();
    }
}