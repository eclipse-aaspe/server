using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using Extensions;
using FluentAssertions;
using Xunit;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class OperationDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public OperationDetailsBuilderTests()
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
        var builder = new OperationDetailsBuilder();
        var operation = _fixture.Create<Operation>();
        var treeItem = new TreeItem { Tag = operation };
        const int line = 0;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be("Semantic ID");
    }

    [Theory]
    [InlineData(1, 0, "CountInputs")]
    [InlineData(2, 0, "CountOutputs")]
    public void Build_ShouldReturnHeader_WhenLineIsSpecified(int line, int column, string expectedHeader)
    {
        // Arrange
        var builder = new OperationDetailsBuilder();
        var operation = _fixture.Create<Operation>();
        var treeItem = new TreeItem { Tag = operation };

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(expectedHeader);
    }

    [Fact]
    public void Build_ShouldReturnInputCount_WhenLineIsOneAndColumnIsOne()
    {
        // Arrange
        var builder = new OperationDetailsBuilder();
        var operation = _fixture.Create<Operation>();
        var treeItem = new TreeItem { Tag = operation };
        const int line = 1;
        const int column = 1;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be($"{operation.InputVariables?.Count ?? 0}");
    }

    [Fact]
    public void Build_ShouldReturnOutputCount_WhenLineIsTwoAndColumnIsOne()
    {
        // Arrange
        var builder = new OperationDetailsBuilder();
        var operation = _fixture.Create<Operation>();
        var treeItem = new TreeItem { Tag = operation };
        const int line = 2;
        const int column = 1;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be($"{operation.OutputVariables?.Count ?? 0}");
    }

    [Fact]
    public void Build_ShouldReturnQualifiers_WhenLineIsThree()
    {
        // Arrange
        var builder = new OperationDetailsBuilder();
        var operation = _fixture.Create<Operation>();
        var treeItem = new TreeItem { Tag = operation };
        const int line = 3;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(operation.Qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", operation.Qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty);
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanThree()
    {
        // Arrange
        var builder = new OperationDetailsBuilder();
        var operation = _fixture.Create<Operation>();
        var treeItem = new TreeItem { Tag = operation };
        const int line = 4;
        const int column = 0;

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}