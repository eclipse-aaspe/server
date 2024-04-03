using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class FileDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public FileDetailsBuilderTests()
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
        var builder = new FileDetailsBuilder();
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        var treeItem = new TreeItem {Tag = file};
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Fact]
    public void Build_ShouldReturnValueHeader_WhenLineIsOneAndColumnIsZero()
    {
        // Arrange
        var builder = new FileDetailsBuilder();
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        var treeItem = new TreeItem {Tag = file};
        const int line = 1;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Value");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsTwo()
    {
        // Arrange
        var builder = new FileDetailsBuilder();
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        var treeItem = new TreeItem {Tag = file};
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(file.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", file.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsThreeOrMore()
    {
        // Arrange
        var builder = new FileDetailsBuilder();
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        var treeItem = new TreeItem {Tag = file};
        var line = _fixture.Create<int>() + 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}