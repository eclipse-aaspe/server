using AasCore.Aas3_0;
using AutoFixture;
using AutoFixture.AutoMoq;
using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers;

public class RequestValueMapperTests
{
    private readonly Fixture _fixture;

    public RequestValueMapperTests()
    {
        _fixture = new Fixture();
         _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact (Skip = "AutoFixture was unable to create an instance from DataTransferObjects.CommonDTOs.ReferenceDTO, most likely because it has no public constructor, is an abstract or non-public type.")]
    public void Map_ShouldTransformBasicEventElementValue()
    {
        // Arrange
        var valueDTO = _fixture.Create<BasicEventElementValue>();

        // Act
        var result = RequestValueMapper.Map(valueDTO);

        // Assert
        result.Should().BeOfType<BasicEventElement>();
    }


    [Fact]
    public void Map_ShouldThrowExceptionForUnimplementedType()
    {
        // Arrange
        var valueDTO = _fixture.Create<IDTO>(); // Create a DTO of unimplemented type

        // Act
        Action action = () => RequestValueMapper.Map((IValueDTO) valueDTO);

        // Assert
        action.Should().Throw<System.InvalidCastException>();
    }
}