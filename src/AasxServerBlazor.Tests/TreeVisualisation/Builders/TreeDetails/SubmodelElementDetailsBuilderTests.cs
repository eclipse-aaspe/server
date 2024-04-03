using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class SubmodelElementDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public SubmodelElementDetailsBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Build_ShouldReturnSemanticIdHeader_WhenLineIsZeroAndColumnIsZero()
    {
        // Arrange
        var builder = new SubmodelElementDetailsBuilder();
        var submodelElement = _fixture.Create<ISubmodelElement>();
        var treeItem = new TreeItem { Tag = submodelElement };
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsOne()
    {
        // Arrange
        var builder = new SubmodelElementDetailsBuilder();
        var submodelElement = _fixture.Create<ISubmodelElement>();
        var treeItem = new TreeItem { Tag = submodelElement };
        const int line = 1;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(submodelElement.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", submodelElement.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanOne()
    {
        // Arrange
        var builder = new SubmodelElementDetailsBuilder();
        var submodelElement = _fixture.Create<ISubmodelElement>();
        var treeItem = new TreeItem { Tag = submodelElement };
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenTreeItemTagIsNotISubmodelElement()
    {
        // Arrange
        var builder = new SubmodelElementDetailsBuilder();
        var treeItem = new TreeItem { Tag = new object() };
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}