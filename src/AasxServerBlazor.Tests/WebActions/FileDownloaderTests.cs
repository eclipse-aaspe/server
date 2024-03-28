using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using AasxServerBlazor.WebActions;
using AdminShellNS;
using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.JSInterop;
using Moq;

namespace AasxServerBlazor.Tests.WebActions;

public class FileDownloaderTests
{
    private readonly Fixture _fixture;

    public FileDownloaderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    [Fact(Skip = "Cannot verify extension method. need to create a wrapper around JSInterop to test this correctly.")]
    public async Task DownloadFile_WithValidFile_CallsSaveAsMethod()
    {
        // Arrange
        var jsRuntimeMock = new Mock<IJSRuntime>();
        var fileDownloader = new FileDownloader(jsRuntimeMock.Object);
        var aasFile = _fixture.Create<AasCore.Aas3_0.File>();
        aasFile.Value = "sample.txt";
        var selectedNode = new TreeItem {Tag = aasFile, EnvironmentIndex = 0};

        // Act
        await fileDownloader.DownloadFile(selectedNode);

        // Assert
        jsRuntimeMock.Verify(js => js.InvokeAsync<object>("saveAsFile", It.IsAny<string>(), It.IsAny<byte[]>()), Times.Once);
    }

    [Fact(Skip = "Cannot verify extension method. need to create a wrapper around JSInterop to test this correctly.")]
    public async Task DownloadFile_WithInvalidFile_DoesNotCallSaveAsMethod()
    {
        // Arrange
        var jsRuntimeMock = new Mock<IJSRuntime>();
        var fileDownloader = new FileDownloader(jsRuntimeMock.Object);
        var aasFile = _fixture.Create<AasCore.Aas3_0.File>();
        aasFile.Value = string.Empty;
        var selectedNode = new TreeItem {Tag = aasFile}; // Invalid file

        // Act
        await fileDownloader.DownloadFile(selectedNode);

        // Assert
        jsRuntimeMock.Verify(js => js.InvokeAsync<object>("saveAsFile", It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public void DownloadFile_NullNode_ThrowsArgumentNullException()
    {
        // Arrange
        var jsRuntimeMock = new Mock<IJSRuntime>();
        var fileDownloader = new FileDownloader(jsRuntimeMock.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => fileDownloader.DownloadFile(null));
    }

    [Fact]
    public void DownloadFile_InvalidTag_ThrowsArgumentException()
    {
        // Arrange
        var jsRuntimeMock = new Mock<IJSRuntime>();
        var fileDownloader = new FileDownloader(jsRuntimeMock.Object);
        var selectedNode = new TreeItem {Tag = "Not a file"}; // Invalid tag

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => fileDownloader.DownloadFile(selectedNode));
    }
}