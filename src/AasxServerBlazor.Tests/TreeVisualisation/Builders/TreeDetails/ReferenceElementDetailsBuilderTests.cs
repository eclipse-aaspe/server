using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using Extensions;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class ReferenceElementDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public ReferenceElementDetailsBuilderTests()
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
        var builder = new ReferenceElementDetailsBuilder();
        var referenceElement = _fixture.Create<ReferenceElement>();
        var treeItem = new TreeItem { Tag = referenceElement };
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Theory]
    [InlineData(1, 0, "Value")]
    [InlineData(2, 0, "Qualifiers,  ,  ,  ")]
    public void Build_ShouldReturnHeader_WhenLineIsSpecified(int line, int column, string expectedHeader)
    {
        // Arrange
        var builder = new ReferenceElementDetailsBuilder();
        var referenceElement = _fixture.Create<ReferenceElement>();
        var treeItem = new TreeItem { Tag = referenceElement };

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(expectedHeader);
    }

    [Fact]
    public void Build_ShouldReturnKeysToStringExtended_WhenLineIsOneAndColumnIsOne()
    {
        // Arrange
        var builder = new ReferenceElementDetailsBuilder();
        var referenceElement = _fixture.Create<ReferenceElement>();
        var treeItem = new TreeItem { Tag = referenceElement };
        const int line = 1;
        const int column = 1;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(referenceElement.Value != null ? referenceElement.Value.Keys.ToStringExtended() : "NULL");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsTwo()
    {
        // Arrange
        var builder = new ReferenceElementDetailsBuilder();
        var referenceElement = _fixture.Create<ReferenceElement>();
        var treeItem = new TreeItem { Tag = referenceElement };
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(referenceElement.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", referenceElement.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanTwo()
    {
        // Arrange
        var builder = new ReferenceElementDetailsBuilder();
        var referenceElement = _fixture.Create<ReferenceElement>();
        var treeItem = new TreeItem { Tag = referenceElement };
        const int line = 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}