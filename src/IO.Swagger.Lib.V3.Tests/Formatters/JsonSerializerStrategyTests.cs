using System.Security.AccessControl;
using System.Text.Json;
using AasCore.Aas3_0;
using AdminShellNS.Lib.V3.Models;
using AutoFixture;
using AutoFixture.AutoMoq;
using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Lib.V3.SerializationModifiers;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using IO.Swagger.Models;
using Moq;

namespace IO.Swagger.Lib.V3.Tests.Formatters;

public class JsonSerializerStrategyTests
{
    private readonly JsonSerializerStrategy _sut;
    private readonly Fixture _fixture;
    private readonly Mock<IValueDTO> _mockValueDto;
    private readonly Mock<PagedResult> _mockPagedResult;
    private readonly Mock<IValueOnlyJsonSerializer> _mockValueOnlyJsonSerializer;
    private readonly Mock<ISerializationModifiersValidator> _mockSerializationModifiersValidator;

    public JsonSerializerStrategyTests()
    {
        _mockValueOnlyJsonSerializer = new Mock<IValueOnlyJsonSerializer>();
        _mockSerializationModifiersValidator = new Mock<ISerializationModifiersValidator>();
        _sut = new JsonSerializerStrategy(_mockValueOnlyJsonSerializer.Object, _mockSerializationModifiersValidator.Object);
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _fixture.Customize(new AutoMoqCustomization());
        _mockValueDto = new Mock<IValueDTO>();
        _mockPagedResult = new Mock<PagedResult>();
    }

    #region CanSerialize

    [Fact]
    public void CanSerialize_WhenObjectIsIClass_ShouldReturnTrue()
    {
        // Arrange
        var expected = _fixture.Create<Qualifier>();

        // Act
        var result = _sut.CanSerialize(expected.GetType(), expected);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanSerialize_WhenObjectIsValueOnlyPagedResult_ShouldReturnTrue()
    {
        // Arrange
        var expected = _fixture.Create<ValueOnlyPagedResult>();

        // Act
        var result = _sut.CanSerialize(expected.GetType(), expected);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanSerialize_WhenObjectIsIValueDTO_ShouldReturnTrue()
    {
        // Arrange
        var expected = _fixture.Create<IValueDTO>();

        // Act
        var result = _sut.CanSerialize(expected.GetType(), expected);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanSerialize_WhenObjectIsPagedResult_ShouldReturnTrue()
    {
        // Arrange
        var expected = _fixture.Create<PagedResult>();

        // Act
        var result = _sut.CanSerialize(expected.GetType(), expected);

        result.Should().BeTrue();
    }

    [Fact]
    public void CanSerialize_WhenObjectIsGenericListOfIClass_ShouldReturnTrue()
    {
        // Arrange
        var genericList = new List<IClass>();

        // Act
        var result = _sut.CanSerialize(typeof(List<IClass>), genericList);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void CanSerialize_WhenObjectIsGenericListOfIValueDto_ShouldReturnTrue()
    {
        // Arrange
        var genericList = _fixture.Create<List<IValueDTO>>();
        genericList.Add(new EntityValue(_fixture.Create<string>(), EntityType.CoManagedEntity));
        genericList.Add(new EntityValue(_fixture.Create<string>(), EntityType.CoManagedEntity));

        // Act
        var result = _sut.CanSerialize(genericList.GetType(), genericList);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Serialize

    [Fact]
    public void Serialize_ShouldThrowArgumentException_WhenUnsupportedTypeIsProvided()
    {
        // Arrange
        var unsupportedType = _fixture.Create<string>(); // Providing an unsupported type
        var mockWriter = new Mock<Utf8JsonWriter>(new MemoryStream(), new JsonWriterOptions());

        // Act & Assert
        _sut.Invoking(x => x.Serialize(mockWriter.Object, unsupportedType, LevelEnum.Core, ExtentEnum.WithoutBlobValue))
            .Should().Throw<ArgumentException>();
    }

    #endregion
}