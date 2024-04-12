using AutoFixture;
using AutoFixture.Kernel;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers;

public class ValueOnlyJsonSerializerTests
{
    private readonly Fixture _fixture;
        
    public ValueOnlyJsonSerializerTests()
    {
        _fixture = new Fixture();
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _fixture.Customizations.Add(new TypeRelay(typeof(ISubmodelElementValue), typeof(EntityValue)));
    }

    [Fact]
    public void ToJsonObject_WithEntityValue_ShouldTransformCorrectly()
    {
        // Arrange
        var entityValue = _fixture.Create<EntityValue>();
            
        // Act
        var result = ValueOnlyJsonSerializer.ToJsonObject(entityValue);
            
        // Assert
        result.Should().NotBeNull();
    }

}