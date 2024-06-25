namespace AasxCsharpLibary.Tests.AasxCompatibilityModels.V20;

using System.Text.Json;
using global::AasxCompatibilityModels;

public class AdminShellConvertersTests
{
    private readonly Fixture _fixture;

    public AdminShellConvertersTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void JsonAasxConverter_CanConvert_Should_Return_True_For_Referable()
    {
        // Arrange
        var converter     = new AdminShell_V20.AdminShellConverters.JsonAasxConverter("modelType", "name");
        var typeToConvert = typeof(AdminShellV20.Referable);

        // Act
        var result = converter.CanConvert(typeToConvert);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void JsonAasxConverter_Read_Should_Populate_Target_Object()
    {
        // Arrange
        var jsonString = "{\"modelType\": { \"name\": \"SubmodelElement\" }, \"otherProperty\": \"value\" }";
        var options    = new JsonSerializerOptions(); // Use real JsonSerializerOptions
        var bytes      = System.Text.Encoding.UTF8.GetBytes(jsonString);
        var reader     = new Utf8JsonReader(bytes, new JsonReaderOptions {AllowTrailingCommas = true});

        var converter = new AdminShell_V20.AdminShellConverters.JsonAasxConverter("modelType", "name");

        // Act
        var result = converter.Read(ref reader, typeof(AdminShellV20.Referable), options);

        // Assert
        result.Should().BeOfType<AdminShellV20.SubmodelElement>(); // Adjust type as needed
        // Add more assertions based on expected behavior
    }
}