using System;
using AasxServerBlazor.TreeVisualisation;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.WebActions.AasxLinkCreation;

internal static class ExternalLinkCreator
{
    public static bool TryGetExternalLink(TreeItem selectedNode, out string externalUrl)
    {
        externalUrl = string.Empty;
        var value = GetValue(selectedNode.Tag);

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (IsHttpUrl(value))
        {
            externalUrl = value;
            return true;
        }

        if (!IsAasxUrl(value))
        {
            return false;
        }

        externalUrl = GetSubmodelAttachmentLink(selectedNode, externalUrl);
        return true;
    }

    private static string GetValue(object selectedNodeTag)
    {
        return selectedNodeTag switch
        {
            File fileObject => fileObject.Value,
            Property propertyObject => propertyObject.Value,
            _ => string.Empty
        };
    }

    private static bool IsHttpUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsAasxUrl(string url)
    {
        return url.StartsWith("/aasx/", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetSubmodelAttachmentLink(TreeItem selectedNode, string currentUrl)
    {
        // Extract submodel information
        var submodelId = FindParentSubmodel(selectedNode).Id;
        var submodelElementPath = GetSubmodelElementPath(selectedNode);

        // Construct attachment link
        var attachmentLink = $"{currentUrl}submodels/{Base64UrlEncoder.Encode(submodelId)}/submodel-elements/{submodelElementPath}/attachment";

        return attachmentLink;
    }
    
    private static string GetSubmodelElementPath(TreeItem selectedNode)
    {
        if (!TryGetSubmodelElement(selectedNode, out var submodelElement))
        {
            return string.Empty;
        }

        var path = submodelElement.IdShort;
        var parent = submodelElement.Parent as IReferable;

        while (parent is not null && parent is not Submodel)
        {
            path = AppendPathSegment(parent, submodelElement, path);
            parent = parent.Parent as IReferable;
        }

        return path;
    }


    private static bool TryGetSubmodelElement(TreeItem selectedNode, out ISubmodelElement submodelElement)
    {
        submodelElement = selectedNode.Tag as ISubmodelElement;
        return submodelElement != null;
    }

    private static string AppendPathSegment(IReferable parent, ISubmodelElement submodelElement, string path)
    {
        if (parent is ISubmodelElementList parentList)
        {
            if (path?.Equals(submodelElement.IdShort) != true)
            {
                return $"{parentList.IdShort}{path}";
            }

            var index = parentList.Value?.IndexOf(submodelElement);
            return $"[{index}]{parentList.IdShort}{path}";
        }

        switch (parent.Parent)
        {
            case ISubmodelElementList prevParentList:
            {
                var index = prevParentList.Value?.IndexOf(parent as ISubmodelElement);
                return $"[{index}].{parent.IdShort}{path}";
            }
            default:
                return $"{parent.IdShort}.{path}";
        }
    }

    private static Submodel FindParentSubmodel(TreeItem selectedNode)
    {
        var parent = (TreeItem) selectedNode.Parent;
        while (parent != null)
        {
            if (parent.Tag is Submodel submodel)
            {
                return submodel;
            }

            parent = (TreeItem) parent.Parent;
        }

        return null;
    }
}