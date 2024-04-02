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
}