using AasCore.Aas3_0;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using DataTransferObjects.ValueDTOs;
using FluentAssertions;
using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers;
using Moq;
using Xunit;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers;

public class ResponseValueTransformerTests
{
    private readonly Fixture _fixture;
    private readonly ResponseValueTransformer _transformer;

    public ResponseValueTransformerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Behaviors.Remove(new ThrowingRecursionBehavior());
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        _transformer = new ResponseValueTransformer();
    }

    [Fact]
    public void TransformAdministrativeInformation_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformAdministrativeInformation(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformAssetAdministrationShell_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformAssetAdministrationShell(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformAssetInformation_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformAssetInformation(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }


    [Fact]
    public void TransformCapability_ShouldThrow_InvalidSerializationModifierException()
    {
        // Arrange
        _fixture.Customizations.Add(
            new TypeRelay(
                typeof(ICapability),
                typeof(Capability)));
        
        var capability = _fixture.Create<Capability>();

        // Act
        Action act = () => _transformer.TransformCapability(capability);

        // Assert
        act.Should().Throw<InvalidSerializationModifierException>();
    }
    
    [Fact]
    public void TransformCapability_ShouldReturnNull_WhenInputIsNull()
    {
        // Arrange

        // Act
        var result = _transformer.TransformCapability(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TransformConceptDescription_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformConceptDescription(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformDataSpecificationIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformDataSpecificationIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformEmbeddedDataSpecification_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformEmbeddedDataSpecification(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformEnvironment_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformEnvironment(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformEventPayload_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformEventPayload(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformExtension_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformExtension(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringDefinitionTypeIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringDefinitionTypeIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringNameType_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringNameType(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringPreferredNameTypeIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringPreferredNameTypeIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringShortNameTypeIec61360_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringShortNameTypeIec61360(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLangStringTextType_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLangStringTextType(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformLevelType_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformLevelType(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformQualifier_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformQualifier(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformResource_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformResource(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformSpecificAssetId_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformSpecificAssetId(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformValueList_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformValueList(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }

    [Fact]
    public void TransformValueReferencePair_ShouldThrow_NotImplementedException()
    {
        // Arrange

        // Act
        Action act = () => _transformer.TransformValueReferencePair(null!);

        // Assert
        act.Should().Throw<System.NotImplementedException>();
    }
}