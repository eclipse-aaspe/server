using AasxServerBlazor.Shared;
using Bunit;
using FluentAssertions;

namespace AasxServerBlazor.Tests.Shared;

public class NavMenuTests
{
    [Fact]
    public async Task ToggleNavMenu_WhenToggleIsNeverCalled_TogglesNavMenuCollapsedStateToNull()
    {
        // Arrange
        using var testContext = new TestContext();
        var renderComponent = testContext.RenderComponent<NavMenu>();

        // Act
        await renderComponent.InvokeAsync(() => renderComponent.Instance.ToggleNavMenu());

        // Assert
        renderComponent.Instance.NavMenuCssClass.Should().BeNull();
        var navMenuDiv = renderComponent.Find("div.navbar");
        navMenuDiv.ClassList.Should().NotContain(renderComponent.Instance.NavMenuCssClass);
    }

    [Fact]
    public void NavMenu_IsInitiallyCollapsed()
    {
        // Arrange
        using var testContext = new TestContext();
        var renderComponent = testContext.RenderComponent<NavMenu>();

        // Act - no action needed as we're checking initial state

        // Assert
        var navMenuDiv = renderComponent.Find("div." + renderComponent.Instance.NavMenuCssClass);
        navMenuDiv.ClassList.Should().Contain("collapse");
    }
}