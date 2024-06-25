namespace AasxCsharpLibary.Tests.AasxCompatibilityModels.V20;

using System.Text.Json;
using global::AasxCompatibilityModels;

public class AdaptiveFilterContractResolverTests
{
    private readonly Fixture _fixture;
    private readonly Mock<JsonSerializerOptions> _mockOptions;

    public AdaptiveFilterContractResolverTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _mockOptions = _fixture.Freeze<Mock<JsonSerializerOptions>>();
    }

    [Fact]
    public void AdaptiveFilterContractResolver_CanConvert_Should_Return_True_For_AdministrationShell()
    {
        // Arrange
        var resolver      = new AdminShell_V20.AdminShellConverters.AdaptiveFilterContractResolver();
        var typeToConvert = typeof(AdminShellV20.AdministrationShell);

        // Act
        var result = resolver.CanConvert(typeToConvert);

        // Assert
        result.Should().BeTrue();
    }

    // Add more tests as per other scenarios and edge cases
}