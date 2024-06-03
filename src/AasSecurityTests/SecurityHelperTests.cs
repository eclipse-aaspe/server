using AasSecurity;
using AasxServer;
using AdminShellNS;
using FluentAssertions;

namespace AasSecurityTests;

public class SecurityHelperTests
{

    [Fact]
    public void ParseSecurityMetamodel_When_Environment_List_Is_Empty()
    {
        // Arrange
        Program.env = Array.Empty<AdminShellPackageEnv>();

        // Act
        var action = SecurityHelper.SecurityInit;

        // Assert
        action.Should().NotThrow();
    }

}