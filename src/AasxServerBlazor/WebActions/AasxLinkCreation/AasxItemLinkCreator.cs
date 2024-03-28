﻿using System;
using AasxServer;
using AasxServerBlazor.TreeVisualisation;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.WebActions.AasxLinkCreation;

internal class AasxItemLinkCreator
{
    private readonly IExternalLinkCreator _externalLinkCreator;

    public AasxItemLinkCreator(IExternalLinkCreator externalLinkCreator)
    {
        _externalLinkCreator = externalLinkCreator;
    }
    
    public string GetLink(TreeItem selectedNode, string currentUrl, out bool external)
    {
        external = false;

        if (selectedNode == null)
            return "";

        var selectedNodeTag = selectedNode.Tag;

        switch (selectedNodeTag)
        {
            case null when Program.envSymbols[selectedNode.EnvironmentIndex] == "L":
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