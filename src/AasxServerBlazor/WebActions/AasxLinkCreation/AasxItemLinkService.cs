using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.WebActions.AasxLinkCreation;

/// <inheritdoc cref="IAasxItemLinkService"/>
internal class AasxItemLinkService: IAasxItemLinkService
{
    private const string EnvironmentSymbol = "L";
    private readonly IExternalLinkCreator _externalLinkCreator;

    public AasxItemLinkService(IExternalLinkCreator externalLinkCreator)
    {
        _externalLinkCreator = externalLinkCreator;
    }
    
    /// <inheritdoc/>
    public string Create(TreeItem selectedNode, string currentUrl, out bool external)
    {
        external = false;

        if (selectedNode == null)
            return "";

        var selectedNodeTag = selectedNode.Tag;

        switch (selectedNodeTag)
        {
            case null when Program.envSymbols[selectedNode.EnvironmentIndex] == EnvironmentSymbol:
                return $"{Program.externalRest}/server/getaasx/{selectedNode.EnvironmentIndex}";

            case AssetAdministrationShell _:
                return $"{currentUrl}packages/{Base64UrlEncoder.Encode(selectedNode.EnvironmentIndex.ToString())}";

            case File _ when _externalLinkCreator.TryGetExternalLink(selectedNode, out var fileUrl):
                external = true;
                return fileUrl;

            case Property _ when _externalLinkCreator.TryGetExternalLink(selectedNode, out var propertyUrl):
                external = true;
                return propertyUrl;
            default:
                return string.Empty;
        }
    }
}