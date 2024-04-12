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

    public MappingServiceTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _administrativeInformationMock = new Mock<IAdministrativeInformation>();
        _administrativeInformationDTOMock = new Mock<AdministrativeInformationDTO>();
    }


}