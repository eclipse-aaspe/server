using Xunit;
using Moq;
using FluentAssertions;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.MetadataMappers;
using DataTransferObjects.MetadataDTOs;
using DataTransferObjects.CommonDTOs;
using AasCore.Aas3_0;
using System.Collections.Generic;
using AutoFixture;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.MetadataMappers;

public class RequestMetadataMapperTests
{
    private readonly Fixture _fixture;
    private readonly RequestMetadataMapper _mapper;
    private readonly Mock<IMetadataDTO> _metadataDTOMock;

    public RequestMetadataMapperTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

        _mapper = new RequestMetadataMapper();
        _metadataDTOMock = new Mock<IMetadataDTO>();
    }


    [Fact]
    public void Map_WithNullSource_ReturnsNull()
    {
        // Arrange
        IMetadataDTO source = null;

        // Act
        var result = _mapper.Map(source);

        // Assert
        result.Should().BeNull();
    }


    [Fact(Skip = "Tests need a better way for mocking and transforming")]
    public void Map_WithPropertyMetadata_ReturnsTransformedProperty()
    {
        // Arrange
        var propertyMetadata = _fixture.Create<PropertyMetadata>();

        // Act
        var result = _mapper.Map(propertyMetadata);

        // Assert
        result.Should().BeOfType<Property>();
    }
}