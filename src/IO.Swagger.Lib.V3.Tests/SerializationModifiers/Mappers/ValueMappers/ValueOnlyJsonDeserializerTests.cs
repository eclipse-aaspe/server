using System;
using System.Text.Json.Nodes;
using AasxServerStandardBib.Logging;
using AasxServerStandardBib.Services;
using AutoFixture;
using AutoFixture.Kernel;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using Moq;
using Xunit;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers
{
    public class ValueOnlyJsonDeserializerTests
    {
        private readonly Fixture _fixture;
        private readonly ValueOnlyJsonDeserializer _deserializer;

        public ValueOnlyJsonDeserializerTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            _fixture.Customizations.Add(
                new TypeRelay(
                    typeof(AasxServerStandardBib.Interfaces.ISubmodelService),
                    typeof(SubmodelService)));

            // Mocking the IAppLogger<T> interface
            var mockLogger = new Mock<IAppLogger<AdminShellPackageEnvironmentService>>();
            _fixture.Inject(mockLogger.Object);

            _deserializer = _fixture.Create<ValueOnlyJsonDeserializer>();
        }

        // Write your test methods here
        [Fact (Skip = "untestable class")]
        public void DeserializeSubmodelElementValue_WithNonNullNode_ShouldReturnNotNull()
        {
            // Arrange
            var node = _fixture.Create<JsonNode>();

            // Act
            var result = _deserializer.DeserializeSubmodelElementValue(node);

            // Assert
            result.Should().NotBeNull();
        }

        // Add more test methods as needed
    }
}