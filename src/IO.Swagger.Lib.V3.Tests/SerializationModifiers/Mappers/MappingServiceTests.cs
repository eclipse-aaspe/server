using AasCore.Aas3_0;
using AutoFixture;
using DataTransferObjects;
using DataTransferObjects.CommonDTOs;
using DataTransferObjects.MetadataDTOs;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using Moq;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers;

public class MappingServiceTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IAdministrativeInformation> _administrativeInformationMock;
    private readonly Mock<AdministrativeInformationDTO> _administrativeInformationDTOMock;
    private readonly MappingService _mappingService;
    private readonly Mock<IResponseMetadataMapper> _responseMetadataMapperMock;
    private readonly Mock<IResponseValueMapper> _responseValueMapperMock;
    private readonly Mock<IRequestMetadataMapper> _requestMetadataMapperMock;
    private readonly Mock<IRequestValueMapper> _requestValueMapperMock;


    public MappingServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _administrativeInformationMock = new Mock<IAdministrativeInformation>();
        _administrativeInformationDTOMock = new Mock<AdministrativeInformationDTO>();

        _responseMetadataMapperMock = new Mock<IResponseMetadataMapper>();
        _responseValueMapperMock = new Mock<IResponseValueMapper>();
        _requestMetadataMapperMock = new Mock<IRequestMetadataMapper>();
        _requestValueMapperMock = new Mock<IRequestValueMapper>();

        _mappingService = new MappingService(
            _responseMetadataMapperMock.Object,
            _responseValueMapperMock.Object,
            _requestMetadataMapperMock.Object,
            _requestValueMapperMock.Object);
    }

    [Fact]
    public void Map_WithNullMappingResolverKey_ThrowsException()
    {
        // Arrange
        string mappingResolverKey = null;

        // Act
        Action act = () => _mappingService.Map(It.IsAny<IClass>(), mappingResolverKey);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Could not resolve serializer modifier mapper.");
    }

    [Fact]
    public void Map_WithInvalidMappingResolverKey_ThrowsException()
    {
        // Arrange
        var mappingResolverKey = "invalid_key";

        // Act
        Action act = () => _mappingService.Map(It.IsAny<IClass>(), mappingResolverKey);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Invalid modifier mapping resolved key");
    }

    [Fact]
    public void MapList_WithNullMappingResolverKey_ThrowsException()
    {
        // Arrange
        string mappingResolverKey = null;

        // Act
        Action act = () => _mappingService.Map(new List<IClass>(), mappingResolverKey);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Could not resolve serializer modifier mapper.");
    }

    [Fact]
    public void MapList_WithInvalidMappingResolverKey_ThrowsException()
    {
        // Arrange
        var mappingResolverKey = "invalid_key";

        // Act
        Action act = () => _mappingService.Map(new List<IClass>(), mappingResolverKey);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Invalid modifier mapping resolved key");
    }

    [Fact]
    public void MapDTO_WithNullMappingResolverKey_ThrowsException()
    {
        // Arrange
        string mappingResolverKey = null;

        // Act
        Action act = () => _mappingService.Map(It.IsAny<IDTO>(), mappingResolverKey);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Could not resolve serializer modifier mapper.");
    }

    [Fact]
    public void MapDTO_WithInvalidMappingResolverKey_ThrowsException()
    {
        // Arrange
        var mappingResolverKey = "invalid_key";
        var dtoMock = new Mock<IDTO>();

        // Act
        Action act = () => _mappingService.Map(dtoMock.Object, mappingResolverKey);

        // Assert
        act.Should().Throw<Exception>().WithMessage("Invalid modifier mapping resolved key");
    }

    [Fact]
    public void Map_WithMetadataMappingResolverKey_ReturnsMappedDTO()
    {
        // Arrange
        string mappingResolverKey = "metadata";
        var source = new Mock<IClass>();
        var mappedDtoMock = new Mock<IDTO>();
        _responseMetadataMapperMock.Setup(m => m.Map(It.IsAny<IClass>())).Returns(mappedDtoMock.Object);

        // Act
        var result = _mappingService.Map(source.Object, mappingResolverKey);

        // Assert
        result.Should().Be(mappedDtoMock.Object);
    }


    [Fact]
    public void MapList_WithMetadataMappingResolverKey_ReturnsMappedDTOList()
    {
        // Arrange
        var mappingResolverKey = "metadata";
        var sourceList = new List<IClass> {new Mock<IClass>().Object};
        var mappedDtoListMock = new List<IDTO> {new Mock<IDTO>().Object};
        _responseMetadataMapperMock.Setup(m => m.Map(It.IsAny<IClass>())).Returns(mappedDtoListMock[0]);

        // Act
        var result = _mappingService.Map(sourceList, mappingResolverKey);

        // Assert
        result.Should().BeEquivalentTo(mappedDtoListMock);
    }


    [Fact]
    public void MapDTO_WithMetadataMappingResolverKeyAndMetadataDTO_ReturnsMappedClass()
    {
        // Arrange
        var mappingResolverKey = "metadata";
        var dtoMock = new Mock<IMetadataDTO>();
        var mappedClassMock = new Mock<IClass>();
        _requestMetadataMapperMock.Setup(m => m.Map(It.IsAny<IMetadataDTO>())).Returns(mappedClassMock.Object);

        // Act
        var result = _mappingService.Map(dtoMock.Object, mappingResolverKey);

        // Assert
        result.Should().Be(mappedClassMock.Object);
    }

    [Fact]
    public void MapDTO_WithValueMappingResolverKeyAndValueDTO_ReturnsMappedClass()
    {
        // Arrange
        var mappingResolverKey = "value";
        var dtoMock = new Mock<IValueDTO>();
        var mappedClassMock = new Mock<IClass>();
        _requestValueMapperMock.Setup(m => m.Map(It.IsAny<IValueDTO>())).Returns(mappedClassMock.Object);

        // Act
        var result = _mappingService.Map(dtoMock.Object, mappingResolverKey);

        // Assert
        result.Should().Be(mappedClassMock.Object);
    }
}