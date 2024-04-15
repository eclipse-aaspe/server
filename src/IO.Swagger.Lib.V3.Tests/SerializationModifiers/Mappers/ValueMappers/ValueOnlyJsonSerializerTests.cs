using AutoFixture;
using AutoFixture.Kernel;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using Xunit;
using FluentAssertions;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers
{
    public class ValueOnlyJsonSerializerTests
    {
        private readonly Fixture _fixture;
        
        public ValueOnlyJsonSerializerTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _fixture.Customizations.Add(new TypeRelay(typeof(ISubmodelElementValue), typeof(EntityValue)));
            _fixture.Customizations.Add(new TypeRelay(typeof(EntityValue), typeof(EntityValue)));
        }

        [Fact(Skip = "Only one is testable, the others will break. needs a refactored implementation class first.")]
        public void ToJsonObject_WithEntityValue_ShouldTransformCorrectly()
        {
            // Arrange
            var entityValue = _fixture.Create<EntityValue>();
            
            // Act
            var result = new ValueOnlyJsonSerializer().ToJsonObject(entityValue);
            
            // Assert
            result.Should().NotBeNull();
        }
        
        [Fact]
        public void ToJsonObject_WithSubmodelValue_ShouldTransformCorrectly()
        {
            // Arrange
            var submodelValue = _fixture.Create<SubmodelValue>();
            
            // Act
            var result = new ValueOnlyJsonSerializer().ToJsonObject(submodelValue);
            
            // Assert
            result.Should().NotBeNull();
        }
    }
}
