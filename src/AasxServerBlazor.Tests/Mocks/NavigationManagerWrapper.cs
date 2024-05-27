using Microsoft.AspNetCore.Components;

namespace AasxServerBlazor.Tests.Mocks;

/// <summary>
/// The NavigationManager class cannot be directly mocked using Moq because it doesn't expose virtual members or interfaces that can be mocked.
/// However, we can create a wrapper interface around NavigationManager and use that in our code, allowing us to mock it in tests.
/// </summary>
public interface INavigationManagerWrapper
{
    string Uri { get; }
}

public class NavigationManagerWrapper : INavigationManagerWrapper
{
    private readonly NavigationManager _navigationManager;

    public NavigationManagerWrapper(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;
    }

    public string Uri => _navigationManager.Uri;
}