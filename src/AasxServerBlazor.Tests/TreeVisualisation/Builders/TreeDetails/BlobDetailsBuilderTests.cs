using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using System.Text;
using AasxServerBlazor.TreeVisualisation;
using Extensions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class BlobDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public BlobDetailsBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void Build_ShouldThrow_WhenTreeItemIsNull()
    {
        // Arrange
        var builder = new BlobDetailsBuilder();

        // Act
        Action action = () => builder.Build(null, 0, 0);

        // Assert
        action.Should().Throw<NullReferenceException>();
    }

    [Theory]
    [InlineData(0, 0, "Semantic ID")]
    [InlineData(1, 0, "ContentType")]
    [InlineData(2, 0, "Value")]
    [InlineData(3, 0, "Qualifiers,  ,  ,  ")]
    public void Build_ShouldReturnExpectedHeaders_WhenLineIsSpecified(int line, int column, string expectedHeader)
    {
        // Arrange
        var builder = new BlobDetailsBuilder();
        var blob = _fixture.Create<Blob>();
        var treeItem = new TreeItem { Tag = blob };

        // Act
        var result = builder.Build(treeItem, line, column);

        // Assert
        result.Should().Be(expectedHeader);
    }

    [Fact]
    public void Build_ShouldReturnSemanticIdRow_WhenLineIsZeroAndColumnIsOne()
    {
        // Arrange
        var builder = new BlobDetailsBuilder();
        var blob = _fixture.Create<Blob>();
        var treeItem = new TreeItem { Tag = blob };

        // Act
        var result = builder.Build(treeItem, 0, 1);

        // Assert
        result.Should().Be("NULL");
    }

    [Fact]
    public void Build_ShouldReturnContentTypeRow_WhenLineIsOneAndColumnIsOne()
    {
        // Arrange
        var builder = new BlobDetailsBuilder();
        var blob = _fixture.Create<Blob>();
        var treeItem = new TreeItem { Tag = blob };

        // Act
        var result = builder.Build(treeItem, 1, 1);

        // Assert
        var expected = blob.ContentType;
        result.Should().Be(expected);
    }

    [Fact]
    public void Build_ShouldReturnValueRow_WhenLineIsTwoAndColumnIsOne()
    {
        // Arrange
        var builder = new BlobDetailsBuilder();
        var blob = _fixture.Create<Blob>();
        var treeItem = new TreeItem { Tag = blob };

        // Act
        var result = builder.Build(treeItem, 2, 1);

        // Assert
        var expected = Encoding.ASCII.GetString(blob.Value ?? Array.Empty<byte>());
        result.Should().Be(expected);
    }
}