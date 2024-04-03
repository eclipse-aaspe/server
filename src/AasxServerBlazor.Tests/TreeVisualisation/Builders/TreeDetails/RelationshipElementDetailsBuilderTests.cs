using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class RelationshipElementDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public RelationshipElementDetailsBuilderTests()
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
        var builder = new RelationshipElementDetailsBuilder();
        var relationshipElement = _fixture.Create<RelationshipElement>();
        var treeItem = new TreeItem {Tag = relationshipElement};
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Fact]
    public void Build_ShouldReturnFirstHeader_WhenLineIsOneAndColumnIsZero()
    {
        // Arrange
        var builder = new RelationshipElementDetailsBuilder();
        var relationshipElement = _fixture.Create<RelationshipElement>();
        var treeItem = new TreeItem {Tag = relationshipElement};
        const int line = 1;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("First");
    }

    [Fact]
    public void Build_ShouldReturnSecondHeader_WhenLineIsTwoAndColumnIsZero()
    {
        // Arrange
        var builder = new RelationshipElementDetailsBuilder();
        var relationshipElement = _fixture.Create<RelationshipElement>();
        var treeItem = new TreeItem {Tag = relationshipElement};
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Second");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsThree()
    {
        // Arrange
        var builder = new RelationshipElementDetailsBuilder();
        var relationshipElement = _fixture.Create<RelationshipElement>();
        var treeItem = new TreeItem {Tag = relationshipElement};
        const int line = 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(relationshipElement.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", relationshipElement.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanThree()
    {
        // Arrange
        var builder = new RelationshipElementDetailsBuilder();
        var relationshipElement = _fixture.Create<RelationshipElement>();
        var treeItem = new TreeItem {Tag = relationshipElement};
        const int line = 4;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}