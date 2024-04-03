using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class EntityDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public EntityDetailsBuilderTests()
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
        var builder = new EntityDetailsBuilder();
        var entity = _fixture.Create<Entity>();
        var treeItem = new TreeItem {Tag = entity};
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Fact]
    public void Build_ShouldReturnEntityTypeHeader_WhenLineIsOneAndColumnIsZero()
    {
        // Arrange
        var builder = new EntityDetailsBuilder();
        var entity = _fixture.Create<Entity>();
        var treeItem = new TreeItem {Tag = entity};
        const int line = 1;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Entity Type");
    }

    [Fact]
    public void Build_ShouldReturnAssetHeader_WhenLineIsTwoAndEntityTypeIsSelfManagedEntity()
    {
        // Arrange
        var builder = new EntityDetailsBuilder();
        var entity = _fixture.Create<Entity>();
        entity.EntityType = EntityType.SelfManagedEntity;
        var treeItem = new TreeItem {Tag = entity};
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Asset");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsThree()
    {
        // Arrange
        var builder = new EntityDetailsBuilder();
        var entity = _fixture.Create<Entity>();
        var treeItem = new TreeItem {Tag = entity};
        const int line = 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(entity.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", entity.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsFourOrMore()
    {
        // Arrange
        var builder = new EntityDetailsBuilder();
        var entity = _fixture.Create<Entity>();
        var treeItem = new TreeItem {Tag = entity};
        var line = _fixture.Create<int>() + 4;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}