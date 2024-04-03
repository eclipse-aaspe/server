using AasCore.Aas3_0;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

namespace AasxServerBlazor.Tests.TreeVisualisation.Builders.TreeDetails;

public class AssetAdministrationShellDetailsBuilderTests
{
    private readonly Fixture _fixture;

    public AssetAdministrationShellDetailsBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void Build_ShouldReturnIdHeader_WhenLineIsZeroAndColumnIsZero()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 0, 0);

        // Assert
        result.Should().Be("ID");
    }


    [Fact]
    public void Build_ShouldReturnEmptyString_WhenTreeItemIsNull()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();

        // Act
        Action action = () => builder.Build(null, 0, 0);

        // Assert
        action.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenLineIsGreaterThanFour()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 5, 0);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnEmptyString_WhenColumnIsGreaterThanTwo()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 0, 3);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Build_ShouldReturnAssetHeader_WhenLineIsOneAndColumnIsZero()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 1, 0);

        // Assert
        result.Should().Be("ASSET");
    }

    [Fact]
    public void Build_ShouldReturnAssetIdHeader_WhenLineIsTwoAndColumnIsZero()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 2, 0);

        // Assert
        result.Should().Be("ASSETID");
    }

    [Fact]
    public void Build_ShouldReturnAssetIdUrlEncodedHeader_WhenLineIsThreeAndColumnIsZero()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 3, 0);

        // Assert
        result.Should().Be("ASSETID URLENCODED");
    }

    [Fact]
    public void Build_ShouldReturnExtensionsHeader_WhenLineIsFourAndColumnIsZero()
    {
        // Arrange
        var builder = new AssetAdministrationShellDetailsBuilder();
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas};

        // Act
        var result = builder.Build(treeItem, 4, 0);

        // Assert
        result.Should().Be("Extensions");
    }
}