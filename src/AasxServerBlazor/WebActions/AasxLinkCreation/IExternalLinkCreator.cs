using AasxServerBlazor.TreeVisualisation;

namespace AasxServerBlazor.WebActions.AasxLinkCreation;

/// <summary>
/// Provides methods to create external links based on selected nodes in a tree.
/// </summary>
public interface IExternalLinkCreator
{
    /// <summary>
    /// Tries to get an external link based on the selected node.
    /// </summary>
    /// <param name="selectedNode">The selected node in the tree.</param>
    /// <param name="externalUrl">The external URL generated.</param>
    /// <returns><see langword="true"/> if an external link was successfully generated; otherwise, <see langword="false"/>.</returns>
    bool TryGetExternalLink(TreeItem selectedNode, out string externalUrl);
}