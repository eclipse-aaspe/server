using AasCore.Aas3_0;
using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.TreeVisualisation.Builders;
using AdminShellNS;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;

public class ImageBuilderTests
{
    private readonly Fixture _fixture;

    public ImageBuilderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void CreateDetailsImage_ShouldReturnEmptyString_WhenTreeItemIsNull()
    {
        // Arrange
        TreeItem? treeItem = null;
        bool url, svg;

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out url, out svg);

        // Assert
        result.Should().BeEmpty();
        url.Should().BeFalse();
        svg.Should().BeFalse();
    }

    [Fact]
    public void CreateDetailsImage_ShouldReturnEmptyString_WhenTagIsNotAssetAdministrationShellOrFile()
    {
        // Arrange
        var treeItem = new TreeItem {Tag = _fixture.Create<object>()};
        bool url, svg;

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out url, out svg);

        // Assert
        result.Should().BeEmpty();
        url.Should().BeFalse();
        svg.Should().BeFalse();
    }

    [Fact(Skip = "Only testable when we fix the Program singleton into something we can Mock")]
    public void CreateDetailsImage_ShouldReturnBase64String_WhenTagIsAssetAdministrationShell()
    {
        // Arrange
        var aas = _fixture.Create<AssetAdministrationShell>();
        var treeItem = new TreeItem {Tag = aas, EnvironmentIndex = 0};
        Program.env = new[] {new AdminShellPackageEnv()};

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out var url, out var svg);

        // Assert
        result.Should().NotBeNullOrEmpty();
        url.Should().BeFalse();
        svg.Should().BeFalse();
    }

    [Theory(Skip = "Only testable when we fix the Program singleton into something we can Mock")]
    [InlineData("some/image.jpg")]
    [InlineData("some/image.png")]
    [InlineData("some/image.svg")]
    [InlineData("some/image.bmp")]
    public void CreateDetailsImage_ShouldReturnBase64String_WhenTagIsFileWithValidExtension(string value)
    {
        // Arrange
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = value;
        var treeItem = new TreeItem {Tag = file, EnvironmentIndex = 0};
        Program.env = new[] {new AdminShellPackageEnv()};

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out var url, out var svg);

        // Assert
        result.Should().NotBeNullOrEmpty();
        url.Should().BeFalse();
        svg.Should().BeTrue();
    }

    [Fact(Skip = "Only testable when we fix the Program singleton into something we can Mock")]
    public void CreateDetailsImage_ShouldReturnUrl_WhenTagIsFileWithNonImageExtension()
    {
        // Arrange
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = "some/file.txt";
        var treeItem = new TreeItem {Tag = file, EnvironmentIndex = 0};
        Program.env = new[] {new AdminShellPackageEnv()}; // Ensure the environment is available

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out var url, out var svg);

        // Assert
        result.Should().Be("some/file.txt");
        url.Should().BeTrue();
        svg.Should().BeFalse();
    }

    [Fact]
    public void CreateDetailsImage_ShouldReturnEmptyString_WhenTagIsFileWithInvalidExtension()
    {
        // Arrange
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = "some/image.xyz";
        var treeItem = new TreeItem {Tag = file, EnvironmentIndex = 0};
        Program.env = new[] {new AdminShellPackageEnv()};

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out var url, out var svg);

        // Assert
        result.Should().BeEmpty();
        url.Should().BeFalse();
        svg.Should().BeFalse();
    }

    [Fact]
    public void CreateDetailsImage_ShouldReturnEmptyString_WhenEnvironmentIsNotAvailable()
    {
        // Arrange
        var file = _fixture.Create<AasCore.Aas3_0.File>();
        file.Value = "some/image.jpg";
        var treeItem = new TreeItem {Tag = file, EnvironmentIndex = 0};
        Program.env = null;

        // Act
        var result = ImageBuilder.CreateDetailsImage(treeItem, out var url, out var svg);

        // Assert
        result.Should().BeEmpty();
        url.Should().BeFalse();
        svg.Should().BeFalse();
    }
}