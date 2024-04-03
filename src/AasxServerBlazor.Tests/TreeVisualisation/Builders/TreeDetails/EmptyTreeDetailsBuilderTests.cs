using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;


namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class EmptyTreeDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public EmptyTreeDetailsBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_Always()
    {
        // Arrange
        var builder = new EmptyTreeDetailsBuilder();
        var treeItem = _fixture.Create<TreeItem>();
        var line = _fixture.Create<int>();
        var column = _fixture.Create<int>();

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().BeEmpty();
    }
}