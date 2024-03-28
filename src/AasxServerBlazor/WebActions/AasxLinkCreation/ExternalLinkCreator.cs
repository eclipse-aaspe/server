using System;
using AasxServerBlazor.TreeVisualisation;
using JetBrains.Annotations;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.WebActions.AasxLinkCreation;

/// <inheritdoc cref="IExternalLinkCreator"/>
internal class ExternalLinkCreator : IExternalLinkCreator
{
    /// <inheritdoc />
    public bool TryGetExternalLink(TreeItem selectedNode, out string externalUrl)
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

        externalUrl = GetSubModelAttachmentLink(selectedNode, externalUrl);
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

    private static string GetSubModelAttachmentLink(TreeItem selectedNode, string currentUrl)
    {
        // Extract subModel information
        var submodelId = FindParentSubModel(selectedNode)?.Id;
        var submodelElementPath = GetSubModelElementPath(selectedNode);

        // Validate submodelId and submodelElementPath
        if (string.IsNullOrEmpty(submodelId) || string.IsNullOrEmpty(submodelElementPath))
        {
            return string.Empty;
        }

        // Construct attachment link
        var attachmentLink = $"{currentUrl}submodels/{Base64UrlEncoder.Encode(submodelId)}/submodel-elements/{submodelElementPath}/attachment";

        return attachmentLink;
    }

    private static string GetSubModelElementPath(TreeItem selectedNode)
    {
        if (!TryGetSubModelElement(selectedNode, out var subModelElement))
        {
            return string.Empty;
        }

        var path = subModelElement.IdShort;
        var parent = subModelElement.Parent as IReferable;

        while (parent is not null && parent is not Submodel)
        {
            path = AppendPathSegment(parent, subModelElement, path);
            parent = parent.Parent as IReferable;
        }

        return path;
    }
    
    private static bool TryGetSubModelElement(TreeItem selectedNode, out ISubmodelElement subModelElement)
    {
        subModelElement = selectedNode.Tag as ISubmodelElement;
        return subModelElement != null;
    }

    private static string AppendPathSegment(IReferable parent, ISubmodelElement subModelElement, string path)
    {
        int? index;
        switch (parent)
        {
            case ISubmodelElementList parentList when path?.Equals(subModelElement.IdShort) != true:
                index = parentList.Value?.IndexOf(subModelElement);
                return $"[{index}]{parentList.IdShort}{path}";
            case ISubmodelElementList parentList:
                var indexList = parentList.Value?.IndexOf(subModelElement);
                return $"[{indexList}].{parent.IdShort}{path}";
            default:
                if (parent.Parent is not ISubmodelElementList prevParentList)
                {
                    return $"{parent.IdShort}.{path}";
                }

                index = prevParentList.Value?.IndexOf(parent as ISubmodelElement);
                return $"[{index}].{parent.IdShort}{path}";
        }
    }


    [CanBeNull]
    private static Submodel FindParentSubModel(TreeItem selectedNode)
    {
        var parent = (TreeItem) selectedNode.Parent;
        while (parent != null)
        {
            if (parent.Tag is Submodel subModel)
            {
                return subModel;
            }

            parent = (TreeItem) parent.Parent;
        }

        return null;
    }
}