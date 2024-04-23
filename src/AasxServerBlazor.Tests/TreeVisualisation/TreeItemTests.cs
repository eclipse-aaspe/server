using AasCore.Aas3_0;
using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;

namespace AasxServerBlazor.Tests.TreeVisualisation;

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

public class TreeItemTests
{
    private readonly Fixture _fixture;

    public TreeItemTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    #region Constructor

    [Fact]
    public void Constructor_WithDefaultValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        // Act
        var treeItem = new TreeItem();

        // Assert
        treeItem.Text.Should().BeNull();
        treeItem.Childs.Should().BeNull();
        treeItem.Parent.Should().BeNull();
        treeItem.Type.Should().BeNull();
        treeItem.Tag.Should().BeNull();
        treeItem.EnvironmentIndex.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithValues_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        const string text = "Sample Text";
        var childs = new List<TreeItem>();
        var parent = new object();
        const string type = "Sample Type";
        var tag = new object();
        const int environmentIndex = 1;

        // Act
        var treeItem = new TreeItem
        {
            Text = text,
            Childs = childs,
            Parent = parent,
            Type = type,
            Tag = tag,
            EnvironmentIndex = environmentIndex
        };

        // Assert
        treeItem.Text.Should().Be(text);
        treeItem.Childs.Should().BeEquivalentTo(childs);
        treeItem.Parent.Should().Be(parent);
        treeItem.Type.Should().Be(type);
        treeItem.Tag.Should().Be(tag);
        treeItem.EnvironmentIndex.Should().Be(environmentIndex);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ReturnsExpectedString()
    {
        // Arrange
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        var treeItem = _fixture.Create<TreeItem>();
        var childItem1 = _fixture.Create<TreeItem>();
        var childItem2 = _fixture.Create<TreeItem>();
        var childOfChild2 = _fixture.Create<TreeItem>();
        childItem2.Childs = new[] {childOfChild2};
        treeItem.Childs = new[] {childItem1, childItem2};

        // Act
        var result = treeItem.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain($"Text: {treeItem.Text}");
        result.Should().Contain($"Type: {treeItem.Type}");
        result.Should().Contain($"EnvironmentIndex: {treeItem.EnvironmentIndex}");

        foreach (var child in treeItem.Childs)
        {
            result.Should().Contain(child.ToString());
        }
    }

    #endregion

    #region GetHtmlId

    [Fact]
    public void GetHtmlId_ShouldReturnCorrectIdWhenParentIsNull()
    {
        // Arrange
        var treeItem = _fixture.Create<TreeItem>();
        var expectedId = treeItem.GetIdentifier();

        // Act
        var result = treeItem.GetHtmlId();

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public void GetHtmlId_ShouldReturnCorrectIdWhenParentIsNotNull()
    {
        // Arrange
        var parentItem = _fixture.Create<TreeItem>();
        var treeItem = _fixture.Create<TreeItem>();
        treeItem.Parent = parentItem;
        var expectedId = $"{parentItem.GetHtmlId()}.{treeItem.GetIdentifier()}";

        // Act
        var result = treeItem.GetHtmlId();

        // Assert
        result.Should().Be(expectedId);
    }

    #endregion

    #region GetIdentifier

    [Fact]
    public void GetIdentifier_WhenEnvironmentIsSetToLAndTagIsNull_ShouldReturnExpectedText()
    {
        // Arrange
        const string expectedText = "Text";
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = null
        };
        Program.envSymbols = new[] {"L"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be(expectedText);
    }

    [Fact]
    public void GetIdentifier_WhenEnvironmentIsNotSetAndTagIsNull_ShouldReturnNullAsString()
    {
        // Arrange
        const string expectedText = "Text";
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = null
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be("NULL");
    }

    [Fact]
    public void GetIdentifier_WhenTagIsStringAndTextContainsReadme_ShouldReturnEmptyString()
    {
        // Arrange
        const string expectedText = "/readme";
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = expectedText
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetIdentifier_WhenTagIsAssetAdministrationShell_ShouldReturnAasIdShort()
    {
        // Arrange
        const string expectedText = "/readme";
        var assetAdministrationShell = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = assetAdministrationShell
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be(assetAdministrationShell.IdShort);
    }

    [Fact]
    public void GetIdentifier_WhenTagIsASubmodelAndSubmodelKindIsNull_ShouldReturnSubmodelIdShort()
    {
        // Arrange
        const string expectedText = "/readme";
        var submodel = _fixture.Create<Submodel>();
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = submodel
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be(submodel.IdShort);
    }

    [Fact]
    public void GetIdentifier_WhenTagIsASubmodelAndSubmodelKindIsTemplate_ShouldReturnSubmodelTypePlusIdShort()
    {
        // Arrange
        const string expectedText = "/readme";
        var submodel = _fixture.Create<Submodel>();
        submodel.Kind = ModellingKind.Template;
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = submodel
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be($"<T> {submodel.IdShort}");
    }


    [Fact]
    public void GetIdentifier_WhenTagIsISubmodelElement_ShouldReturnSubmodelElementIdShort()
    {
        // Arrange
        const string expectedText = "/readme";
        var submodel = _fixture.Create<ISubmodelElement>();
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = submodel
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be(submodel.IdShort);
    }

    [Fact]
    public void GetIdentifier_WhenTagIsFile_ShouldReturnFileIdShort()
    {
        // Arrange
        const string expectedText = "/readme";
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = file
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be(file.IdShort);
    }

    [Fact]
    public void GetIdentifier_WhenTagIsBlob_ShouldReturnBlobIdShort()
    {
        // Arrange
        const string expectedText = "/readme";
        var blob = _fixture.Create<Blob>();
        var treeItem = new TreeItem
        {
            Text = expectedText, EnvironmentIndex = 0,
            Tag = blob
        };
        Program.envSymbols = new[] {"M"};

        // Act
        var result = treeItem.GetIdentifier();

        // Assert
        result.Should().Be(blob.IdShort);
    }

    #endregion

    #region GetTimeStamp

    [Fact]
    public void GetTimeStamp_WhenTagIsNull_ShouldReturnEmptyString()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = null};

        // Act
        var result = treeItem.GetTimeStamp();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetTimeStamp_WhenTagIsNotIReferable_ShouldReturnEmptyString()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = new object()};

        // Act
        var result = treeItem.GetTimeStamp();

        // Assert
        result.Should().BeEmpty();
    }


    [Fact]
    public void GetTimeStamp_WhenTagIsIReferableAndTimeStampTreeIsNotNull_ShouldReturnFormattedTimeStampString()
    {
        // Arrange
        var timeStamp = DateTime.Now;
        var referableObject = new Mock<IReferable>();
        referableObject.SetupGet(r => r.TimeStampTree).Returns(timeStamp);
        var treeItem = new TreeItem {Tag = referableObject.Object};

        // Act
        var result = treeItem.GetTimeStamp();

        // Assert
        result.Should().Be($" ({timeStamp:yy-MM-dd HH:mm:ss.fff}) ");
    }

    #endregion

    #region GetSymbolicRepresentation

    [Fact]
    public void GetSymbols_WhenTagIsNull_ShouldReturnEmptyString()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = null};

        // Act
        var result = treeItem.GetSymbolicRepresentation();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetSymbols_WhenTagIsNotAssetAdministrationShell_ShouldReturnEmptyString()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = new object()};

        // Act
        var result = treeItem.GetSymbolicRepresentation();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(999)]
    [InlineData(-1)]
    public void GetSymbols_WhenEnvironmentIndexIsOutOfRange_ShouldReturnEmptyString(int environmentIndex)
    {
        // Arrange
        var assetAdministrationShell = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = assetAdministrationShell, EnvironmentIndex = environmentIndex};

        // Act
        var result = treeItem.GetSymbolicRepresentation();

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("L;S;V", "ENCRYPTED SIGNED VALIDATED")]
    [InlineData("S;L;V", "SIGNED ENCRYPTED VALIDATED")]
    [InlineData("S;V;L", "SIGNED VALIDATED ENCRYPTED")]
    [InlineData("V;S;L", "VALIDATED SIGNED ENCRYPTED")]
    [InlineData("L;S", "ENCRYPTED SIGNED")]
    [InlineData("S;L", "SIGNED ENCRYPTED")]
    [InlineData("V", "VALIDATED")]
    [InlineData("S", "SIGNED")]
    [InlineData("L", "ENCRYPTED")]
    [InlineData("", "")]
    public void GetSymbols_WhenAssetAdministrationShellAndValidSymbols_ShouldReturnExpectedString(string symbols, string expectedSymbols)
    {
        // Arrange
        var assetAdministrationShell = _fixture.Create<AssetAdministrationShell>();
        var envSymbols = new[] {symbols};
        Program.envSymbols = envSymbols;
        var treeItem = new TreeItem {Tag = assetAdministrationShell, EnvironmentIndex = 0};

        // Act
        var result = treeItem.GetSymbolicRepresentation();

        // Assert
        result.Should().Be(expectedSymbols);
    }

    #endregion

    #region BuildNodeRepresentation

    [Fact]
    public void ViewNodeType_WhenTypeIsNotNull_ShouldReturnType()
    {
        // Arrange
        var treeItem = new TreeItem {Type = "SampleType"};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Contain("SampleType");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsStringAndTextContainsReadme_ShouldReturnText()
    {
        // Arrange
        var treeItem = new TreeItem {Text = "/readme", Tag = "SampleTag"};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("/readme");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsNullAndEnvironmentIndexIsL_ShouldReturnAASX2()
    {
        // Arrange
        var envSymbols = new[] {"L"};
        Program.envSymbols = envSymbols;
        var treeItem = new TreeItem {EnvironmentIndex = 0};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("AASX2");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsAssetAdministrationShell_ShouldReturnAAS()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AssetAdministrationShell>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("AAS");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsSubmodel_ShouldReturnSub()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Submodel>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Sub");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsISubmodelElementSubclass_ShouldReturnCorrectSubtypeName()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<SubmodelElementList>()}; // Any subclass of ISubmodelElement

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("SML");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsOperation_ShouldReturnOpr()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Operation>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Opr");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsFile_ShouldReturnFile()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AasCore.Aas3_0.File>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("File");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsBlob_ShouldReturnBlob()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Blob>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Blob");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsRange_ShouldReturnRange()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AasCore.Aas3_0.Range>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Range");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsMultiLanguageProperty_ShouldReturnLang()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<MultiLanguageProperty>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Lang");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsRelationshipElement_ShouldReturnRel()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<RelationshipElement>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Rel");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsReferenceElement_ShouldReturnRef()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<ReferenceElement>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Ref");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsEntity_ShouldReturnEnt()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Entity>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Ent");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsBasicEventElement_ShouldReturnEvt()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<BasicEventElement>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Evt");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsAnnotatedRelationshipElement_ShouldReturnRelA()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AnnotatedRelationshipElement>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("RelA");
    }

    [Fact]
    public void ViewNodeType_WhenTagIsCapability_ShouldReturnCap()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<Capability>()};

        // Act
        var result = treeItem.BuildNodeRepresentation();

        // Assert
        result.Should().Be("Cap");
    }

    #endregion

    #region BuildNodeDescription

    [Fact]
    public void ViewNodeInfo_WhenTagIsAssetAdministrationShell_ShouldReturnEmptyString()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<AssetAdministrationShell>()};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsSubmodelAndQualifiersNotNull_ShouldReturnQualifiersString()
    {
        // Arrange
        var qualifiers = new List<IQualifier>
        {
            _fixture.Create<IQualifier>(),
            _fixture.Create<IQualifier>()
        };
        var submodel = _fixture.Build<Submodel>().With(sm => sm.Qualifiers, qualifiers).Create();
        var treeItem = new TreeItem {Tag = submodel};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" @QUALIFIERS");
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsRangeAndQualifiersNotNull_ShouldReturnQualifiersString()
    {
        // Arrange
        var qualifiers = _fixture.CreateMany<IQualifier>(2).ToList();
        var range = _fixture.Build<AasCore.Aas3_0.Range>().With(r => r.Min, "Value1").With(r => r.Max, "Value2").With(r => r.Qualifiers, qualifiers).Create();
        var treeItem = new TreeItem {Tag = range};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" = Value1 .. Value2 @QUALIFIERS");
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsSubmodelElementCollectionAndValueNotNull_ShouldReturnValueCountString()
    {
        // Arrange
        var submodelElements = _fixture.CreateMany<ISubmodelElement>(2).ToList();
        var submodelElementCollection = _fixture.Build<SubmodelElementCollection>().With(sme => sme.Value, submodelElements).Create();
        var treeItem = new TreeItem {Tag = submodelElementCollection};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" #2 @QUALIFIERS");
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsSubmodelElementCollectionAndQualifiersNotNull_ShouldReturnQualifiersString()
    {
        // Arrange
        var qualifiers = _fixture.CreateMany<IQualifier>(2).ToList();
        var submodelElementCollection = _fixture.Build<SubmodelElementCollection>().With(sme => sme.Qualifiers, qualifiers).Create();
        var treeItem = new TreeItem {Tag = submodelElementCollection};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" #3 @QUALIFIERS");
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsPropertyAndQualifiersNotNull_ShouldReturnQualifiersString()
    {
        // Arrange
        var qualifiers = _fixture.CreateMany<IQualifier>(2).ToList();
        var property = _fixture.Build<Property>()
            .With(prop => prop.Qualifiers, qualifiers)
            .With(prop => prop.Value, "PropertyValue")
            .Create();
        var treeItem = new TreeItem {Tag = property};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" = PropertyValue @QUALIFIERS");
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsFileAndQualifiersNotNull_ShouldReturnQualifiersString()
    {
        // Arrange
        var qualifiers = _fixture.CreateMany<IQualifier>(2).ToList();
        var file = _fixture.Build<AasCore.Aas3_0.File>()
            .With(f => f.Qualifiers, qualifiers)
            .With(f => f.Value, string.Empty)
            .Create();
        var treeItem = new TreeItem {Tag = file};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" =  @QUALIFIERS");
    }

    [Fact]
    public void BuildNodeDescription_WhenTagIsMultiLanguagePropertyAndQualifiersNotNull_ShouldReturnQualifiersString()
    {
        // Arrange
        var qualifiers = _fixture.CreateMany<IQualifier>(2).ToList();
        var multiLanguageProperty = _fixture.Build<MultiLanguageProperty>().With(mlp => mlp.Qualifiers, qualifiers).Create();
        var treeItem = new TreeItem {Tag = multiLanguageProperty};

        // Act
        var result = treeItem.BuildNodeDescription();

        // Assert
        result.Should().Be(" =      @QUALIFIERS");
    }

    #endregion

    #region IsSubmodelElementCollection

    [Fact]
    public void IsSubmodelElementCollection_WhenTagIsSubmodelElementList_ShouldReturnFalse()
    {
        // Arrange
        var treeItem = new TreeItem
        {
            Tag = _fixture.Create<SubmodelElementList>()
        };

        // Act
        var result = treeItem.IsSubmodelElementCollection();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSubmodelElementCollection_WhenTagIsSubmodelElementCollection_ShouldReturnTrue()
    {
        // Arrange
        var treeItem = new TreeItem
        {
            Tag = _fixture.Create<SubmodelElementCollection>()
        };

        // Act
        var result = treeItem.IsSubmodelElementCollection();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsSubmodelElementCollection_WhenTagIsNotSubmodelElement_ShouldReturnFalse()
    {
        // Arrange
        var treeItem = new TreeItem
        {
            Tag = _fixture.Create<SomeOtherType>()
        };

        // Act
        var result = treeItem.IsSubmodelElementCollection();

        // Assert
        Assert.False(result);
    }

    private class SomeOtherType
    {
    }

    #endregion

    #region GetPath

    [Fact]
    public void GetPath_ShouldReturnCorrectPath_WhenNoParent()
    {
        // Arrange
        var treeItem = new TreeItem {Text = "Root"};

        // Act
        var path = treeItem.GetPath();

        // Assert
        Assert.Collection(path,
            item => Assert.Equal("Root", item)
        );
    }

    [Fact]
    public void GetPath_ShouldReturnCorrectPath_WhenSingleParent()
    {
        // Arrange
        var parent = new TreeItem {Text = "Parent"};
        var child = new TreeItem {Text = "Child", Parent = parent};

        // Act
        var path = child.GetPath();

        // Assert
        Assert.Collection(path,
            item => Assert.Equal("Parent", item),
            item => Assert.Equal("Child", item)
        );
    }

    [Fact]
    public void GetPath_ShouldReturnCorrectPath_WhenMultipleParents()
    {
        // Arrange
        var grandparent = new TreeItem {Text = "Grandparent"};
        var parent = new TreeItem {Text = "Parent", Parent = grandparent};
        var child = new TreeItem {Text = "Child", Parent = parent};

        // Act
        var path = child.GetPath();

        // Assert
        Assert.Collection(path,
            item => Assert.Equal("Grandparent", item),
            item => Assert.Equal("Parent", item),
            item => Assert.Equal("Child", item)
        );
    }

    #endregion
}