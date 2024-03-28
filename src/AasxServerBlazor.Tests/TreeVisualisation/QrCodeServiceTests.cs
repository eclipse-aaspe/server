using System.Drawing;
using System.Reflection;
using AasCore.Aas3_0;
using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using AutoFixture;
using AutoFixture.AutoMoq;
using FluentAssertions;
using Moq;
using QRCoder;

namespace AasxServerBlazor.Tests.TreeVisualisation;

public class QrCodeServiceTests
{
    private readonly Fixture _fixture;

    public QrCodeServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact]
    public void GetQrCodeLink_WithNullTreeItem_ReturnsEmptyString()
    {
        // Arrange
        var qrCodeService = new QrCodeService();

        // Act
        var result = QrCodeService.GetQrCodeLink(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetQrCodeLink_WithValidTreeItem_ReturnsLink()
    {
        // Arrange
        var qrCodeService = new QrCodeService();
        var treeItem = _fixture.Create<TreeItem>();
        var assetAdministrationShell = _fixture.Create<AssetAdministrationShell>();
        var assetInformation = _fixture.Create<AssetInformation>();
        assetInformation.GlobalAssetId = "http://example.com";
        assetAdministrationShell.AssetInformation = assetInformation;
        treeItem.Tag = assetAdministrationShell;

        // Act
        var result = QrCodeService.GetQrCodeLink(treeItem);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Be("http://example.com");
    }

    [Fact]
    public void GetQrCodeImage_WithNullTreeItem_ReturnsEmptyString()
    {
        // Arrange
        var qrCodeService = new QrCodeService();

        // Act
        var result = QrCodeService.GetQrCodeImage(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetQrCodeImage_WithValidTreeItem_ReturnsImage()
    {
        // Arrange
        var qrCodeService = new QrCodeService();
        var treeItem = _fixture.Create<TreeItem>();
        treeItem.Tag = _fixture.Create<AssetAdministrationShell>();
        var expectedImage = "base64ImageString";
        Program.generatedQrCodes.Add(treeItem.Tag, expectedImage);

        // Act
        var result = QrCodeService.GetQrCodeImage(treeItem);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Be(expectedImage);
    }

    [Fact]
    public void CreateQrCodeImage_WithNullTreeItem_DoesNotAddToGeneratedQrCodes()
    {
        // Arrange
        var qrCodeService = new QrCodeService();

        // Act
        qrCodeService.CreateQrCodeImage(null);

        // Assert
        Program.generatedQrCodes.Should().BeEmpty();
    }

    [Fact (Skip="We cannot let this test run at the moment, because the QRCodeGenerator is not mockable.")]
    public void CreateQrCodeImage_ValidTreeItem_CreatesQrCodeImage()
    {
        // Arrange
        var qrCodeService = new QrCodeService();

        var assetAdministrationShell = _fixture.Create<AssetAdministrationShell>();
        var assetInformation = _fixture.Create<AssetInformation>();
        assetInformation.GlobalAssetId = "http://example.com";
        assetAdministrationShell.AssetInformation = assetInformation;

        var treeItem = new TreeItem
        {
            Tag = assetAdministrationShell
        };
        var qrCodeGeneratorMock = new Mock<QRCodeGenerator>();
        var qrCodeMock = new Mock<QRCode>();
        qrCodeGeneratorMock.Setup(x => x.CreateQrCode(It.IsAny<string>(), It.IsAny<QRCodeGenerator.ECCLevel>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<QRCodeGenerator.EciMode>(), It.IsAny<int>()))
            .Returns(_fixture.Create<QRCodeData>());
        qrCodeMock.Setup(x => x.GetGraphic(It.IsAny<int>())).Returns(new Bitmap(10, 10));
        Program.generatedQrCodes = new System.Collections.Generic.Dictionary<object, string>();

        // Act
        qrCodeService.CreateQrCodeImage(treeItem);

        // Assert
        qrCodeGeneratorMock.Verify(x => x.CreateQrCode("http://example.com", QRCodeGenerator.ECCLevel.Q, It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<QRCodeGenerator.EciMode>(),
            It.IsAny<int>()), Times.Once);
        qrCodeMock.Verify(x => x.GetGraphic(20), Times.Once);
        Assert.Single(Program.generatedQrCodes);
        Assert.True(Program.generatedQrCodes.ContainsKey(treeItem.Tag));
        Assert.NotEmpty(Program.generatedQrCodes[treeItem.Tag]);
    }
}