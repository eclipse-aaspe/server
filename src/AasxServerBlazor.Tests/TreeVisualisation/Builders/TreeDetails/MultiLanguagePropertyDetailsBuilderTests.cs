using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class MultiLanguagePropertyDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public MultiLanguagePropertyDetailsBuilderTests()
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
        var builder = new MultiLanguagePropertyDetailsBuilder();
        var multiLanguageProperty = _fixture.Create<MultiLanguageProperty>();
        var treeItem = new TreeItem { Tag = multiLanguageProperty };
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
        var builder = new MultiLanguagePropertyDetailsBuilder();
        var multiLanguageProperty = _fixture.Create<MultiLanguageProperty>();
        var treeItem = new TreeItem { Tag = multiLanguageProperty };
        const int line = 1;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(multiLanguageProperty.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", multiLanguageProperty.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnValueRow_WhenLineIsGreaterThanOne()
    {
        // Arrange
        var builder = new MultiLanguagePropertyDetailsBuilder();
        var multiLanguageProperty = _fixture.Create<MultiLanguageProperty>();
        var treeItem = new TreeItem { Tag = multiLanguageProperty };
        const int line = 2;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        if (multiLanguageProperty.Value?.Count > line - 2)
        {
            result.Should().Be(multiLanguageProperty.Value[line - 2].Language);
        }
        else
        {
            result.Should().BeEmpty();
        }
    }
}