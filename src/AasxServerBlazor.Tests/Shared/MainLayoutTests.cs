using AasxServer;
using AasxServerBlazor.Data;
using AasxServerBlazor.Shared;
using AasxServerBlazor.Tests.Mocks;
using Bunit;
using FluentAssertions;
using IO.Swagger.Registry.Lib.V3.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AasxServerBlazor.Tests.Shared
{
    public class MainLayoutTests
    {
        private TestContext _context;
        private Mock<INavigationManagerWrapper>? _navigationManagerWrapperMock;
        private Mock<AASService>? _submodelServiceMock;
        private Mock<IRegistryInitializerService>? _aasRegistryServiceMock;

        public MainLayoutTests()
        {
            _context = new TestContext();
        }

        private void Setup()
        {
            _navigationManagerWrapperMock = new Mock<INavigationManagerWrapper>();
            _navigationManagerWrapperMock.SetupGet(n => n.Uri).Returns("some-uri");
            _context.Services.AddSingleton(_navigationManagerWrapperMock.Object);

            _submodelServiceMock = new Mock<Data.AASService>();
            _context.Services.AddSingleton(_submodelServiceMock.Object);

            _aasRegistryServiceMock = new Mock<IRegistryInitializerService>();
            _context.Services.AddSingleton(_aasRegistryServiceMock.Object);

            Program.isLoading = false;
        }

        [Fact]
        public void MainLayout_RendersExpectedContent_WhenNotLoading()
        {
            // Arrange
            Setup();

            // Act
            var component = _context.RenderComponent<MainLayout>();

            // Assert
            component.Markup.Should().Contain("AASX Browser");
            component.FindAll(".logo").Should().HaveCount(3); // Check for 3 logo images
            component.FindAll(".nav-link").Should().HaveCount(2); // Check for 2 nav links
            component.Markup.Should().NotContain("Loading...");
        }
    }
}