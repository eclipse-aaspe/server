using System.Text.Json;
using AdminShellNS;

namespace AasxCsharpLibary.Tests;

using AasCore.Aas3_0;

public class JsonAasxConverterTests
{
    private readonly Fixture _fixture;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonAasxConverterTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _jsonOptions = new JsonSerializerOptions();
    }

    [Fact]
    public void Read_Should_Deserialize_JsonWithCategoryAndIdShort()
    {
        // Arrange
        const string jsonString = @"
            {
                ""upperClassProperty"": {
                    ""lowerClassProperty"": ""Submodel""
                },
                ""category"": ""test"",
                ""idShort"": ""123abc""
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Submodel>();
        result.Category.Should().Be("test");
        result.IdShort.Should().Be("123abc");
    }

    [Fact]
    public void Read_Should_Return_Null_For_Missing_UpperClassProperty()
    {
        // Arrange
        const string jsonString = @"
            {
                ""category"": ""test"",
                ""idShort"": ""123abc""
            }";

        var bytes  = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader = new Utf8JsonReader(bytes);

        var converter = new AdminShellConverters.JsonAasxConverter("upperClassProperty", "lowerClassProperty");

        // Act
        var result = converter.Read(ref reader, typeof(IReferable), _jsonOptions);

        // Assert
        result.Should().BeNull();
    }
}