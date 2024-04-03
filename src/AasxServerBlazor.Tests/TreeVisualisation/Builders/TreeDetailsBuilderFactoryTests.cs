using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using File = AasCore.Aas3_0.File;
using Range = AasCore.Aas3_0.Range;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders;

public class TreeDetailsBuilderFactoryTests
{
    private readonly Fixture _fixture;

    public TreeDetailsBuilderFactoryTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void Create_ShouldReturnAssetAdministrationShellDetailsBuilder_WhenTagIsAssetAdministrationShell()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AssetAdministrationShell>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<AssetAdministrationShellDetailsBuilder>();
    }


    [Fact]
    public void Create_ShouldReturnSubmodelDetailsBuilder_WhenTagIsSubmodel()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Submodel>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<SubmodelDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnPropertyDetailsBuilder_WhenTagIsProperty()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Property>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<PropertyDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnEntityDetailsBuilder_WhenTagIsEntity()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Entity>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<EntityDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnFileDetailsBuilder_WhenTagIsFile()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<File>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<FileDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnBlobDetailsBuilder_WhenTagIsBlob()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Blob>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<BlobDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnRangeDetailsBuilder_WhenTagIsRange()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Range>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<RangeDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnOperationDetailsBuilder_WhenTagIsOperation()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Operation>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<OperationDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnAnnotatedRelationshipElementDetailsBuilder_WhenTagIsAnnotatedRelationship()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AnnotatedRelationshipElement>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<AnnotatedRelationshipElementDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnRelationshipElementDetailsBuilder_WhenTagIsRelationshipElement()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<RelationshipElement>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<RelationshipElementDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnReferenceElementDetailsBuilder_WhenTagIsReferenceElement()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<ReferenceElement>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<ReferenceElementDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnMultiLanguagePropertyDetailsBuilder_WhenTagIsMultiLanguageProperty()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<MultiLanguageProperty>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<MultiLanguagePropertyDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnSubmodelElementDetailsBuilder_WhenTagIsSubmodel()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<ISubmodelElement>()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<SubmodelElementDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnEmptyTreeDetailsBuilder_WhenTagIsNull()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = null};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<EmptyTreeDetailsBuilder>();
    }

    [Fact]
    public void Create_ShouldReturnEmptyTreeDetailsBuilder_WhenTagIsNotRecognized()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = new UnrecognizedType()};

        // Act
        var builder = TreeDetailsBuilderFactory.Create(treeItem);

        // Assert
        builder.Should().BeOfType<EmptyTreeDetailsBuilder>();
    }
}

internal class UnrecognizedType
{
}