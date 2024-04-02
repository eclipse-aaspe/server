using System.Text;
using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation.Builders;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders;

public class NodeRepresentationBuilderTests
{
    private readonly Fixture _fixture;

    public NodeRepresentationBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void AppendNodeTypeIfMatchesType_WhenObjectTypeMatchesType_ShouldReturnAppendString()
    {
        // Arrange
        var tagObject = new SomeOtherType();
        var type = typeof(SomeOtherType);
        const string appendString = "Type: ";

        // Act
        var result = NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, type, appendString);

        // Assert
        result.Should().Be(appendString);
    }

    [Fact]
    public void AppendNodeTypeIfMatchesType_WhenObjectTypeDoesNotMatchType_ShouldReturnEmptyString()
    {
        // Arrange
        var tagObject = new SomeOtherType();
        var type = typeof(SomeOtherType);
        const string appendString = "Type: ";

        // Act
        var result = NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, type, appendString);

        // Assert
        result.Should().Be(appendString);
    }

    [Fact]
    public void AppendNodeTypeIfMatchesType_WhenTagObjectIsNull_ShouldReturnEmptyString()
    {
        // Arrange
        object tagObject = null;
        var type = typeof(SomeOtherType);
        const string appendString = "Type: ";

        // Act
        var result = NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, type, appendString);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void AppendSubmodelElementNodeType_WhenTagIsSubmodelElementList_ShouldAppendSML()
    {
        // Arrange
        var tag = _fixture.Create<SubmodelElementList>();

        // Act
        var result = NodeRepresentationBuilder.AppendSubmodelElementNodeType(tag);

        // Assert
        result.Should().Be("SML");
    }

    [Fact]
    public void AppendSubmodelElementNodeType_WhenTagIsSubmodelElementCollection_ShouldAppendColl()
    {
        // Arrange
        var tag = _fixture.Create<SubmodelElementCollection>();

        // Act
        var result = NodeRepresentationBuilder.AppendSubmodelElementNodeType(tag);

        // Assert
        result.Should().Be("Coll");
    }

    [Fact]
    public void AppendSubmodelElementNodeType_WhenTagIsProperty_ShouldAppendProp()
    {
        // Arrange
        var tag = _fixture.Create<Property>();

        // Act
        var result = NodeRepresentationBuilder.AppendSubmodelElementNodeType(tag);

        // Assert
        result.Should().Be("Prop");
    }

    [Fact]
    public void AppendSubmodelElementNodeType_WhenTagIsNotSubmodelElement_ShouldNotAppend()
    {
        // Arrange
        var tag = _fixture.Create<SomeOtherType>();

        // Act
        var result = NodeRepresentationBuilder.AppendSubmodelElementNodeType(tag);

        // Assert
        result.Should().BeEmpty();
    }

}

internal class SomeOtherType
{
}