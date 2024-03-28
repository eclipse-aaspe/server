using AasxServerBlazor.TreeVisualisation;

namespace AasxServerBlazor.WebActions.AasxLinkCreation;

/// <summary>
/// Provides functionality to create links for AASX items based on the type of selected node.
/// </summary>
public interface IAasxItemLinkService
{
    /// <summary>
    /// Gets the link for the selected node based on its type and the current URL.
    /// </summary>
    /// <param name="selectedNode">The selected node for which the link is to be generated.</param>
    /// <param name="currentUrl">The current URL.</param>
    /// <param name="external">Indicates whether the link is external.</param>
    /// <returns>The generated link for the selected node.</returns>
    string Create(TreeItem selectedNode, string currentUrl, out bool external);
}