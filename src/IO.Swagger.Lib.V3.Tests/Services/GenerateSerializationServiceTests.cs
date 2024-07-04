namespace AasxServerBlazorTests.Services;

using AasCore.Aas3_0;
using AasxServerStandardBib.Interfaces;
using AasxServerStandardBib.Logging;
using IO.Swagger.Lib.V3.Services;
using JetBrains.Annotations;

[TestSubject(typeof(GenerateSerializationService))]
public class GenerateSerializationServiceTests
{
    private readonly Mock<IAppLogger<GenerateSerializationService>> _mockLogger;
    private readonly Mock<IAssetAdministrationShellService> _mockAasService;
    private readonly Mock<ISubmodelService> _mockSubmodelService;
    private readonly GenerateSerializationService _service;

    public GenerateSerializationServiceTests()
    {
        _mockLogger          = new Mock<IAppLogger<GenerateSerializationService>>();
        _mockAasService      = new Mock<IAssetAdministrationShellService>();
        _mockSubmodelService = new Mock<ISubmodelService>();
        _service             = new GenerateSerializationService(_mockLogger.Object, _mockAasService.Object, _mockSubmodelService.Object);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        Action act = () => new GenerateSerializationService(null, _mockAasService.Object, _mockSubmodelService.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenAasServiceIsNull()
    {
        Action act = () => new GenerateSerializationService(_mockLogger.Object, null, _mockSubmodelService.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("aasService");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSubmodelServiceIsNull()
    {
        Action act = () => new GenerateSerializationService(_mockLogger.Object, _mockAasService.Object, null);
        act.Should().Throw<ArgumentNullException>().WithParameterName("submodelService");
    }

    [Fact]
    public void GenerateSerializationByIds_ShouldReturnEmptyEnvironment_WhenNoIdsProvided()
    {
        // Arrange
        _mockAasService.Setup(x => x.GetAllAssetAdministrationShells(It.IsAny<List<SpecificAssetId>?>(), It.IsAny<string?>())).Returns([]);
        _mockSubmodelService.Setup(x => x.GetAllSubmodels(It.IsAny<IReference>(), It.IsAny<string>())).Returns([]);

        // Act
        var result = _service.GenerateSerializationByIds();

        // Assert
        result.AssetAdministrationShells.Should().BeEmpty();
        result.Submodels.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSerializationByIds_ShouldFetchAASs_WhenAasIdsProvided()
    {
        // Arrange
        var aasId   = "aas1";
        var mockAas = new Mock<IAssetAdministrationShell>();
        mockAas.SetupGet(x => x.Id).Returns(aasId);

        _mockAasService.Setup(x => x.GetAllAssetAdministrationShells(It.IsAny<List<SpecificAssetId>?>(), It.IsAny<string?>())).Returns([mockAas.Object]);
        _mockSubmodelService.Setup(x => x.GetAllSubmodels(It.IsAny<IReference>(), It.IsAny<string>())).Returns([]);

        // Act
        var result = _service.GenerateSerializationByIds(new List<string> {aasId});

        // Assert
        result.AssetAdministrationShells.Should().HaveCount(1);
        result.AssetAdministrationShells.First().Id.Should().Be(aasId);
    }

    [Fact]
    public void GenerateSerializationByIds_ShouldFetchSubmodels_WhenSubmodelIdsProvided()
    {
        // Arrange
        var submodelId   = "submodel1";
        var mockSubmodel = new Mock<ISubmodel>();
        mockSubmodel.SetupGet(x => x.Id).Returns(submodelId);

        _mockAasService.Setup(x => x.GetAllAssetAdministrationShells(It.IsAny<List<SpecificAssetId>?>(), It.IsAny<string?>())).Returns([]);
        _mockSubmodelService.Setup(x => x.GetAllSubmodels(It.IsAny<IReference>(),It.IsAny<string>())).Returns([mockSubmodel.Object]);

        // Act
        var result = _service.GenerateSerializationByIds(null, new List<string> {submodelId});

        // Assert
        result.Submodels.Should().HaveCount(1);
        result.Submodels.First().Id.Should().Be(submodelId);
    }

    [Fact]
    public void GenerateSerializationByIds_ShouldFetchAASsAndSubmodels_WhenBothIdsProvided()
    {
        // Arrange
        var aasId      = "aas1";
        var submodelId = "submodel1";

        var mockAas = new Mock<IAssetAdministrationShell>();
        mockAas.SetupGet(x => x.Id).Returns(aasId);

        var mockSubmodel = new Mock<ISubmodel>();
        mockSubmodel.SetupGet(x => x.Id).Returns(submodelId);

        _mockAasService.Setup(x => x.GetAllAssetAdministrationShells(It.IsAny<List<SpecificAssetId>?>(), It.IsAny<string?>())).Returns([mockAas.Object]);
        _mockSubmodelService.Setup(x => x.GetAllSubmodels(It.IsAny<IReference>(),It.IsAny<string>())).Returns([mockSubmodel.Object]);

        // Act
        var result = _service.GenerateSerializationByIds(new List<string> {aasId}, new List<string> {submodelId});

        // Assert
        result.AssetAdministrationShells.Should().HaveCount(1);
        result.AssetAdministrationShells.First().Id.Should().Be(aasId);

        result.Submodels.Should().HaveCount(1);
        result.Submodels.First().Id.Should().Be(submodelId);
    }
}