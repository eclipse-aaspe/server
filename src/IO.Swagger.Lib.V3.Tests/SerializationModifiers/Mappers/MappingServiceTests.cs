using AasCore.Aas3_0;
using AutoFixture;
using DataTransferObjects.CommonDTOs;
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
    
    //TODO: add more tests
}