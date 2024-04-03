using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Range = AasCore.Aas3_0.Range;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class RangeDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public RangeDetailsBuilderTests()
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
        var builder = new RangeDetailsBuilder();
        var range = _fixture.Create<Range>();
        var treeItem = new TreeItem { Tag = range };
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Theory]
    [InlineData(1, 0, "Min")]
    [InlineData(2, 0, "Max")]
    public void Build_ShouldReturnHeader_WhenLineIsSpecified(int line, int column, string expectedHeader)
    {
        // Arrange
        var builder = new RangeDetailsBuilder();
        var range = _fixture.Create<Range>();
        var treeItem = new TreeItem { Tag = range };

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(expectedHeader);
    }

    [Theory]
    [InlineData(1, 1, "Min")]
    [InlineData(2, 1, "Max")]
    public void Build_ShouldReturnMinOrMaxValue_WhenLineIsOneOrTwoAndColumnIsOne(int line, int column, string expectedValue)
    {
        // Arrange
        var builder = new RangeDetailsBuilder();
        var range = _fixture.Create<Range>();
        var treeItem = new TreeItem { Tag = range };

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Contain(expectedValue);
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsThree()
    {
        // Arrange
        var builder = new RangeDetailsBuilder();
        var range = _fixture.Create<Range>();
        var treeItem = new TreeItem { Tag = range };
        const int line = 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(range.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", range.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanThree()
    {
        // Arrange
        var builder = new RangeDetailsBuilder();
        var range = _fixture.Create<Range>();
        var treeItem = new TreeItem { Tag = range };
        const int line = 4;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}