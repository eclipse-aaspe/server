using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using Extensions;
using FluentAssertions;
using Moq;
using Xunit;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class SubmodelDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public SubmodelDetailsBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Build_ShouldReturnIdHeader_WhenLineIsZeroAndColumnIsZero()
    {
        // Arrange
        var builder = new SubmodelDetailsBuilder();
        var submodel = _fixture.Create<Submodel>();
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("ID");
    }

    [Fact]
    public void Build_ShouldReturnSemanticIdHeader_WhenLineIsOneAndColumnIsZero()
    {
        // Arrange
        var builder = new SubmodelDetailsBuilder();
        var submodel = _fixture.Create<Submodel>();
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 1;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Fact]
    public void Build_ShouldReturnExtensionsHeader_WhenLineIsThreeAndColumnIsZero()
    {
        // Arrange
        var builder = new SubmodelDetailsBuilder();
        var submodel = _fixture.Create<Submodel>();
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Extensions");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsTwo()
    {
        // Arrange
        var builder = new SubmodelDetailsBuilder();
        var submodel = _fixture.Create<Submodel>();
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(submodel.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", submodel.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanThree()
    {
        // Arrange
        var builder = new SubmodelDetailsBuilder();
        var submodel = _fixture.Create<Submodel>();
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 4;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}