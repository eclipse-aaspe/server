using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders;

public class TreeNodeBuilderTests
{
    private readonly Fixture _fixture;
    private readonly TreeNodeBuilder _treeNodeBuilder;

    public TreeNodeBuilderTests()
    {
        _treeNodeBuilder = new TreeNodeBuilder();

        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void CreateDetails_ShouldReturnEmptyString_WhenTreeItemIsNull()
    {
        // Arrange
        TreeItem treeItem = null;
        const int line = 0;
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetails_ShouldReturnEmptyString_WhenTagIsNull()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = null};
        const int line = 0;
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetails_ShouldReturnEmptyString_WhenLineIsInvalid()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AssetAdministrationShell>()};
        const int line = 10; // An invalid line number
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        aas.Id = "AAS001";
        var treeItem = new TreeItem {Tag = aas};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("AAS001");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsSubmodel()
    {
        // Arrange
        var submodel = _fixture.Create<Submodel>();
        submodel.Id = "Submodel001";
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("Submodel001");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsProperty()
    {
        // Arrange
        var property = _fixture.Create<Property>();
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "PropertyId")
        };
        reference.Keys = keys;
        property.SemanticId = reference;
        var treeItem = new TreeItem {Tag = property};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, PropertyId]");
        property.SemanticId.Should().NotBeNull(); // Ensure SemanticId is not null
        property.SemanticId.Keys.Should().Contain(keys); // Ensure SemanticId contains expected keys
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsEntity()
    {
        // Arrange
        var entity = _fixture.Create<Entity>();
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "PropertyId")
        };
        reference.Keys = keys;
        entity.SemanticId = reference;
        var treeItem = new TreeItem {Tag = entity};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, PropertyId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsFile()
    {
        // Arrange
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = "file.txt";
        var treeItem = new TreeItem {Tag = file};
        const int line = 1; // Choose a valid line for File
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("file.txt");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsRange()
    {
        // Arrange
        var range = _fixture.Create<AasCore.Aas3_0.Range>();
        range.IdShort = "RangeId";

        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "PropertyId")
        };
        reference.Keys = keys;
        range.SemanticId = reference;

        var treeItem = new TreeItem {Tag = range};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, PropertyId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsOperation()
    {
        // Arrange
        var operation = _fixture.Create<Operation>();
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "OperationId")
        };
        reference.Keys = keys;
        operation.SemanticId = reference;
        var treeItem = new TreeItem {Tag = operation};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, OperationId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAnnotatedRelationshipElement()
    {
        // Arrange
        var annotatedRelationshipElement = _fixture.Create<AnnotatedRelationshipElement>();
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "AnnotatedRelationId")
        };
        reference.Keys = keys;
        annotatedRelationshipElement.SemanticId = reference;
        var treeItem = new TreeItem {Tag = annotatedRelationshipElement};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, AnnotatedRelationId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsRelationshipElement()
    {
        // Arrange
        var relationshipElement = _fixture.Create<RelationshipElement>();
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "RelationId")
        };
        reference.Keys = keys;
        relationshipElement.SemanticId = reference;
        var treeItem = new TreeItem {Tag = relationshipElement};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, RelationId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsReferenceElement()
    {
        // Arrange
        var referenceElement = _fixture.Create<ReferenceElement>();
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "ReferenceId")
        };
        reference.Keys = keys;
        referenceElement.SemanticId = reference;
        var treeItem = new TreeItem {Tag = referenceElement};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, ReferenceId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsBlob()
    {
        // Arrange
        var blob = _fixture.Create<Blob>();
        blob.ContentType = "image/png";
        blob.Value = System.Text.Encoding.ASCII.GetBytes("BlobContent");

        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "ReferenceId")
        };
        reference.Keys = keys;
        blob.SemanticId = reference;
        var treeItem = new TreeItem {Tag = blob};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("[Blob, ReferenceId]");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsReference()
    {
        // Arrange
        var reference = _fixture.Create<Reference>();
        var keys = new List<IKey>
        {
            new Key(KeyTypes.Blob, "ReferenceId")
        };
        reference.Keys = keys;
        var treeItem = new TreeItem {Tag = reference};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsMultiLanguageProperty()
    {
        // Arrange
        var multiLanguageProperty = _fixture.Create<MultiLanguageProperty>();
        var treeItem = new TreeItem {Tag = multiLanguageProperty};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("NULL");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsISubmodelElement()
    {
        // Arrange
        var submodelElement = _fixture.Create<ISubmodelElement>();
        var treeItem = new TreeItem {Tag = submodelElement};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("NULL");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsZero_ColIsZero()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};
        const int line = 0;
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("ID");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsZero_ColIsOne()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        aas.Id = "AAS001";
        var treeItem = new TreeItem {Tag = aas};
        const int line = 0;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("AAS001");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsZero_ColIsTwo()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        aas.Id = "AAS001";
        var treeItem = new TreeItem {Tag = aas};
        const int line = 0;
        const int col = 2;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be(" ==> " + Base64UrlEncoder.Encode("AAS001"));
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsOne_ColIsZero()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};
        const int line = 1;
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("ASSET");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsOne_ColIsOne()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var assetInfo = _fixture.Create<AssetInformation>();
        assetInfo.GlobalAssetId = "Asset001";
        aas.AssetInformation = assetInfo;
        var treeItem = new TreeItem {Tag = aas};
        const int line = 1;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("Asset001");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsTwo_ColIsZero()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};
        const int line = 2;
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("ASSETID");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsTwo_ColIsOne()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var assetInfo = _fixture.Create<AssetInformation>();
        assetInfo.GlobalAssetId = "Asset001";
        aas.AssetInformation = assetInfo;
        var treeItem = new TreeItem {Tag = aas};
        const int line = 2;
        const int col = 1;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be("Asset001");
    }

    [Fact]
    public void CreateDetails_ShouldReturnExpectedDetails_WhenTagIsAssetAdministrationShell_AndLineIsTwo_ColIsTwo()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var assetInfo = _fixture.Create<AssetInformation>();
        assetInfo.GlobalAssetId = "Asset001";
        aas.AssetInformation = assetInfo;
        var treeItem = new TreeItem {Tag = aas};
        const int line = 2;
        const int col = 2;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().Be(" ==> " + Base64UrlEncoder.Encode("Asset001"));
    }

    [Fact]
    public void CreateDetails_ShouldReturnEmptyString_WhenTagIsNotRecognizedType()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = new object()};
        const int line = 0;
        const int col = 0;

        // Act
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void CreateDetails_ShouldHandleNullSemanticId_WhenTagIsAssetAdministrationShell()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        aas.Id = "AAS001";
        aas.AssetInformation = null; // Simulate null SemanticId
        var treeItem = new TreeItem {Tag = aas};
        const int line = 0;
        const int col = 1;

        // Act
        Action act = () => _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        act.Should().NotThrow();
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);
        result.Should().Be("AAS001");
    }

    [Fact]
    public void CreateDetails_ShouldHandleNullSemanticId_WhenTagIsSubmodel()
    {
        // Arrange
        var submodel = _fixture.Create<Submodel>();
        submodel.Id = "Submodel001";
        submodel.SemanticId = null; // Simulate null SemanticId
        var treeItem = new TreeItem {Tag = submodel};
        const int line = 0;
        const int col = 1;

        // Act
        Action act = () => _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        act.Should().NotThrow();
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);
        result.Should().Be("Submodel001");
    }

    [Fact]
    public void CreateDetails_ShouldHandleNullSemanticId_WhenTagIsProperty()
    {
        // Arrange
        var property = _fixture.Create<Property>();
        property.SemanticId = null; // Simulate null SemanticId
        var treeItem = new TreeItem {Tag = property};
        const int line = 0;
        const int col = 1;

        // Act
        Action act = () => _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        act.Should().NotThrow();
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);
        result.Should().Be("NULL");
    }

    [Fact]
    public void CreateDetails_ShouldHandleNullSemanticId_WhenTagIsEntity()
    {
        // Arrange
        var entity = _fixture.Create<Entity>();
        entity.SemanticId = null; // Simulate null SemanticId
        var treeItem = new TreeItem {Tag = entity};
        const int line = 0;
        const int col = 1;

        // Act
        Action act = () => _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        act.Should().NotThrow();
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);
        result.Should().Be("NULL");
    }

    [Fact]
    public void CreateDetails_ShouldHandleNullSemanticId_WhenTagIsRange()
    {
        // Arrange
        var range = _fixture.Create<AasCore.Aas3_0.Range>();
        range.IdShort = "RangeId";
        range.SemanticId = null; // Simulate null SemanticId
        var treeItem = new TreeItem {Tag = range};
        const int line = 0;
        const int col = 1;

        // Act
        Action act = () => _treeNodeBuilder.CreateDetails(treeItem, line, col);

        // Assert
        act.Should().NotThrow();
        var result = _treeNodeBuilder.CreateDetails(treeItem, line, col);
        result.Should().Be("NULL");
    }
}