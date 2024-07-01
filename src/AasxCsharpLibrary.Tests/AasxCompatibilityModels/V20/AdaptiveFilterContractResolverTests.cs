namespace AasxCsharpLibary.Tests.AasxCompatibilityModels.V20;

using System.Text.Json;
using global::AasxCompatibilityModels;

public class AdaptiveFilterContractResolverTests
{
    private readonly Fixture _fixture;

    public AdaptiveFilterContractResolverTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
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
}