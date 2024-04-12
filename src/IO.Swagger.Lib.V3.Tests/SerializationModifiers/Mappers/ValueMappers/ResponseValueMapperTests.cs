using AasCore.Aas3_0;
using AutoFixture;
using AutoFixture.AutoMoq;
using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using Moq;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers;

public class ResponseValueMapperTests
{
    private readonly Fixture _fixture;
    private readonly Mock<IResponseValueTransformer> _transformerMock;

    public ResponseValueMapperTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _transformerMock = new Mock<IResponseValueTransformer>();
    }

    [Fact (Skip = "Unable to cast object of type 'Castle.Proxies.IDTOProxy' to type 'DataTransferObjects.ValueDTOs.IValueDTO'")]
    public void Map_ShouldCallTransformMethod_WithProvidedSource()
    {
        // Arrange
        var source = _fixture.Create<IClass>();
        var expectedDto = _fixture.Create<DataTransferObjects.ValueDTOs.IValueDTO>();
        _transformerMock.Setup(t => t.Transform(source)).Returns(expectedDto);

        // Act
        var result = ResponseValueMapper.Map(source);

        // Assert
        _transformerMock.Verify(t => t.Transform(source), Times.Once);
        result.Should().Be(expectedDto);
    }

    [Fact (Skip = "Unable to cast object of type 'Castle.Proxies.IDTOProxy' to type 'DataTransferObjects.ValueDTOs.IValueDTO'")]
    public void Map_ShouldReturnNonNullValueDTO_WhenSourceIsProvided()
    {
        // Arrange
        var source = _fixture.Create<IClass>();
        var dto = _fixture.Create<IDTO>();
        _transformerMock.Setup(t => t.Transform(source)).Returns(dto);

        // Act
        var result = ResponseValueMapper.Map(source);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Map_ShouldReturnNull_WhenSourceIsNull()
    {
        // Arrange
        IClass source = null;

        // Act
        var result = ResponseValueMapper.Map(source);

        // Assert
        result.Should().BeNull();
    }
}