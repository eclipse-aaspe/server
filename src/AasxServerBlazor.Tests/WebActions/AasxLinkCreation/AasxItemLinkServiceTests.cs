using AasCore.Aas3_0;
using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.WebActions.AasxLinkCreation;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace AasxServerBlazor.Tests.WebActions.AasxLinkCreation;

public class AasxItemLinkServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IExternalLinkCreator> _externalLinkCreatorMock;
    private readonly AasxItemLinkService _aasxItemLinkService;

    public AasxItemLinkServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _externalLinkCreatorMock = _fixture.Freeze<Mock<IExternalLinkCreator>>();
        _aasxItemLinkService = _fixture.Create<AasxItemLinkService>();
    }

    [Fact]
    public void Create_ShouldReturnEmptyString_WhenSelectedNodeIsNull()
    {
        // Arrange
        TreeItem selectedNode = null;
        const string currentUrl = "http://example.com";

        // Act
        var result = _aasxItemLinkService.Create(selectedNode, currentUrl, out _);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldReturnLinkToGetAasx_WhenSelectedNodeTagIsNullAndEnvironmentIndexIsL()
    {
        // Arrange
        var selectedNode = new TreeItem();
        const string currentUrl = "http://example.com";
        selectedNode.EnvironmentIndex = 0;
        Program.envSymbols = new[] {"L"};

        // Act
        var result = _aasxItemLinkService.Create(selectedNode, currentUrl, out _);

        // Assert
        result.Should().Be($"{Program.externalRest}/server/getaasx/0");
    }


    [Fact]
    public void Create_ShouldReturnLinkToPackage_WhenSelectedNodeIsAssetAdministrationShell()
    {
        // Arrange
        var selectedNode = new TreeItem();
        const string currentUrl = "http://example.com";
        selectedNode.Tag = _fixture.Create<AssetAdministrationShell>();
        selectedNode.EnvironmentIndex = 0;

        // Act
        var result = _aasxItemLinkService.Create(selectedNode, currentUrl, out _);

        // Assert
        result.Should().Be($"{currentUrl}packages/{Base64UrlEncoder.Encode("0")}");
        Program.envSymbols = new[] {"L"};
    }

    [Fact]
    public void Create_ShouldReturnExternalLinkForFile_WhenExternalLinkCreatorReturnsTrueForFile()
    {
        // Arrange
        var selectedNode = new TreeItem();
        selectedNode.Tag = _fixture.Create<AasCore.Aas3_0.File>();
        const string currentUrl = "http://example.com";
        var fileUrl = "http://file.example.com";
        _externalLinkCreatorMock.Setup(x => x.TryGetExternalLink(selectedNode, out fileUrl)).Returns(true);
        Program.envSymbols = new[] {"L"};

        // Act
        var result = _aasxItemLinkService.Create(selectedNode, currentUrl, out var external);

        // Assert
        result.Should().Be(fileUrl);
        external.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldReturnExternalLinkForProperty_WhenExternalLinkCreatorReturnsTrueForProperty()
    {
        // Arrange
        var selectedNode = new TreeItem();
        selectedNode.Tag = _fixture.Create<Property>();
        const string currentUrl = "http://example.com";
        var propertyUrl = "http://property.example.com";
        _externalLinkCreatorMock.Setup(x => x.TryGetExternalLink(selectedNode, out propertyUrl)).Returns(true);
        Program.envSymbols = new[] {"L"};

        // Act
        var result = _aasxItemLinkService.Create(selectedNode, currentUrl, out var external);

        // Assert
        result.Should().Be(propertyUrl);
        external.Should().BeTrue();
    }
}