using System.Text;
using System.Text.Json.Nodes;
using AasxServerStandardBib.Interfaces;
using AutoFixture;
using AutoFixture.AutoMoq;
using DataTransferObjects.ValueDTOs;
using IO.Swagger.Lib.V3.Interfaces;
using IO.Swagger.Lib.V3.SerializationModifiers.Mappers.ValueMappers.JsonObjectParser;
using Moq;

namespace IO.Swagger.Lib.V3.Tests.SerializationModifiers.Mappers.ValueMappers.JsonObjectParser;

public class ValueObjectParserTests
{
    private readonly Fixture _fixture;
    private readonly Mock<ISubmodelService> _submodelServiceMock;
    private readonly Mock<IBase64UrlDecoderService> _decoderServiceMock;

    public ValueObjectParserTests()
    {
        _fixture = new Fixture();
        var customization = new SupportMutableValueTypesCustomization();
        customization.Customize(_fixture);
        _fixture.Customize(new AutoMoqCustomization());
        _submodelServiceMock = _fixture.Freeze<Mock<ISubmodelService>>();
        _decoderServiceMock = _fixture.Freeze<Mock<IBase64UrlDecoderService>>();
    }

    [Fact]
    public void Parse_WithRangeValue_ReturnsRangeValue()
    {
        // Arrange
        var idShort = "range";
        var valueObject = new JsonObject(new Dictionary<string, JsonNode>
        {
            ["min"] = "1",
            ["max"] = "10"
        });
        var parser = _fixture.Create<ValueObjectParser>();

        // Act
        var result = parser.Parse(idShort, valueObject, null, null);

        // Assert
        result.Should().BeOfType<RangeValue>();
        var rangeValue = (RangeValue) result;
        rangeValue.idShort.Should().Be(idShort);
        rangeValue.min.Should().Be("1");
        rangeValue.max.Should().Be("10");
    }

    [Fact]
    public void Parse_WithBlobValue_ReturnsBlobValue()
    {
        // Arrange
        const string idShort = "blob";
        const string base64String = "VGhpcyBpcyB0ZXN0IGJsb2IgdmFsaWQ="; // Valid Base64 string for "This is test blob valid"

        var valueObject = new JsonObject(new Dictionary<string, JsonNode>
        {
            ["contentType"] = "image/jpeg",
            ["value"] = base64String
        });
        var parser = _fixture.Create<ValueObjectParser>();

        // Act
        var result = parser.Parse(idShort, valueObject, null, null);

        // Assert
        result.Should().BeOfType<BlobValue>();
        var blobValue = (BlobValue) result;
        blobValue.idShort.Should().Be(idShort);
    }


    [Fact]
    public void Parse_WithAnnotatedRelationshipElementValue_ReturnsAnnotatedRelationshipElementValue()
    {
        // Arrange
        const string idShort = "annotated_relationship";
        var valueObject = new JsonObject(new Dictionary<string, JsonNode>
        {
            ["annotations"] = new JsonArray(new JsonObject(), new JsonObject()),
            ["first"] = new JsonObject(),
            ["second"] = new JsonObject()
        });
        var parser = _fixture.Create<ValueObjectParser>();

        // Act
        var result = parser.Parse(idShort, valueObject, null, null);

        // Assert
        result.Should().BeOfType<AnnotatedRelationshipElementValue>();
    }

    [Fact]
    public void Parse_WithRelationshipElementValue_ReturnsRelationshipElementValue()
    {
        // Arrange
        var idShort = "relationship";
        var valueObject = new JsonObject(new Dictionary<string, JsonNode>
        {
            ["first"] = new JsonObject(),
            ["second"] = new JsonObject()
        });
        var parser = _fixture.Create<ValueObjectParser>();

        // Act
        var result = parser.Parse(idShort, valueObject, null, null);

        // Assert
        result.Should().BeOfType<RelationshipElementValue>();
    }

    [Fact]
    public void Parse_WithReferenceElementValue_ReturnsReferenceElementValue()
    {
        // Arrange
        const string idShort = "reference";
        var valueObject = new JsonObject(new Dictionary<string, JsonNode>
        {
        });
        var parser = _fixture.Create<ValueObjectParser>();

        // Act
        var result = parser.Parse(idShort, valueObject, null, null);

        // Assert
        result.Should().BeOfType<SubmodelElementCollectionValue>();
    }
}