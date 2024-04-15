using System.Text.Json;
using AasCore.Aas3_0;
using AdminShellNS.Lib.V3.Models;
using AutoFixture;
using AutoFixture.AutoMoq;
using DataTransferObjects;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Formatters;
using IO.Swagger.Models;
using Moq;

namespace IO.Swagger.Lib.V3.Tests.Formatters;

public class JsonSerializerStrategyTests
{
    private readonly JsonSerializerStrategy _sut;
    private readonly Fixture _fixture;
    private readonly Mock<IValueDTO> _mockValueDto;
    private readonly Mock<PagedResult> _mockPagedResult;

    public JsonSerializerStrategyTests()
    {
        _sut = new JsonSerializerStrategy();
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _fixture.Customize(new AutoMoqCustomization());
        _mockValueDto = new Mock<IValueDTO>();
        _mockPagedResult = new Mock<PagedResult>();
    }

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
        genericList.Add(new EntityValue(_fixture.Create<string>(),EntityType.CoManagedEntity)); 
        genericList.Add(new EntityValue(_fixture.Create<string>(),EntityType.CoManagedEntity)); 

        // Act
        var result = _sut.CanSerialize(genericList.GetType(), genericList);

        // Assert
        result.Should().BeTrue();
    }
}