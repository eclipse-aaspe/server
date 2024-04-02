using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AasxServer;
using AasxServerBlazor.TreeVisualisation.Builders;
using Microsoft.Extensions.Primitives;
using Blob = AasCore.Aas3_0.Blob;
using Range = AasCore.Aas3_0.Range;

namespace AasxServerBlazor.TreeVisualisation;

public class TreeItem
{
    private const string EncryptionEnvironmentSymbol = "L";
    public string Text { get; set; }
    public IEnumerable<TreeItem> Childs { get; set; }
    public object Parent { get; set; }
    public string Type { get; set; }
    public object Tag { get; set; }
    public int EnvironmentIndex { get; set; }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.AppendLine($"Text: {Text}");
        stringBuilder.AppendLine($"Type: {Type}");
        stringBuilder.AppendLine($"EnvironmentIndex: {EnvironmentIndex}");

        if (Childs == null)
        {
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLine("Childs:");
        foreach (var child in Childs)
        {
            stringBuilder.AppendLine(child.ToString()); // Recursive call to ToString for Childs
        }

        return stringBuilder.ToString();
    }

    public string GetHtmlId()
    {
        var nodeId = GetIdentifier();
        if (Parent is TreeItem parentItem)
        {
            nodeId = parentItem.GetHtmlId() + "." + nodeId;
        }

        return nodeId;
    }

    public string GetIdentifier()
    {
        switch (Tag)
        {
            case null when IsEncrypted():
                return Text;
            case string tagString when tagString.Contains("/readme"):
                return string.Empty;
            case AssetAdministrationShell assetAdministrationShell:
                return assetAdministrationShell.IdShort;
            case Submodel subModel:
                var nodeId = subModel.Kind == ModellingKind.Template ? "<T> " : string.Empty;
                nodeId += subModel.IdShort;
                return nodeId;
            case ISubmodelElement subModelElement:
                return subModelElement.IdShort;
            default:
                return GetIdShortFromTag(Tag);
        }
    }

    public string GetTimeStamp()
    {
        var timeStampString = string.Empty;

        var tagObject = Tag;

        if (tagObject is IReferable referableObject)
        {
            timeStampString = $" ({referableObject.TimeStampTree:yy-MM-dd HH:mm:ss.fff}) ";
        }

        return timeStampString;
    }

    public string GetSymbolicRepresentation()
    {
        var symbolicRepresentation = string.Empty;

        if (Tag is not AssetAdministrationShell)
        {
            return symbolicRepresentation;
        }

        if (EnvironmentIndex < 0 || EnvironmentIndex >= Program.envSymbols.Length)
        {
            return string.Empty;
        }

        var environmentSymbols = Program.envSymbols[EnvironmentIndex];

        if (environmentSymbols == null)
        {
            return symbolicRepresentation;
        }

        symbolicRepresentation = environmentSymbols.Split(';').Aggregate(symbolicRepresentation, (current, symbol) => current + (TranslateSymbol(symbol) + " "));

        return symbolicRepresentation.Trim();
    }

    public string BuildNodeRepresentation()
    {
        var nodeType = new StringBuilder();

        if (Type != null)
        {
            nodeType.Append(Type + " ");
        }

        if (IsReadme())
        {
            nodeType.Append(Text);
        }

        var tagObject = Tag;

        if (tagObject == null && IsEncrypted())
        {
            nodeType.Append("AASX2");
        }

        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(AssetAdministrationShell), "AAS"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(Submodel), "Sub"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(Operation), "Opr"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(File), "File"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(Blob), "Blob"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(Range), "Range"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(MultiLanguageProperty), "Lang"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(RelationshipElement), "Rel"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(ReferenceElement), "Ref"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(Entity), "Ent"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(BasicEventElement), "Evt"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(AnnotatedRelationshipElement), "RelA"));
        nodeType.Append(NodeRepresentationBuilder.AppendNodeTypeIfMatchesType(tagObject, typeof(Capability), "Cap"));

        nodeType.Append(NodeRepresentationBuilder.AppendSubmodelElementNodeType(tagObject));

        return nodeType.ToString();
    }

    public string BuildNodeDescription()
    {
        var nodeInfoBuilder = new StringBuilder();

        switch (Tag)
        {
            case Submodel submodel:
                NodeDescriptionBuilder.AppendSubmodelInfo(submodel, nodeInfoBuilder);
                break;
            case SubmodelElementCollection collection:
                NodeDescriptionBuilder.AppendCollectionInfo(collection, nodeInfoBuilder);
                break;
        }

        if (Tag is ISubmodelElement)
        {
            switch (Tag)
            {
                case Property property:
                    NodeDescriptionBuilder.AppendPropertyInfo(property, nodeInfoBuilder);
                    break;
                case File file:
                    NodeDescriptionBuilder.AppendFileInfo(file, nodeInfoBuilder);
                    break;
            }
        }

        NodeDescriptionBuilder.AppendAdditionalInfo(nodeInfoBuilder, Tag);

        return nodeInfoBuilder.ToString();
    }

    /// <summary>
    /// Checks if the node represents a SubmodelElementCollection.
    /// </summary>
    /// <returns>True if the node is a SubmodelElementCollection, otherwise false.</returns>
    public bool IsSubmodelElementCollection()
    {
        return Tag switch
        {
            SubmodelElementCollection => true,
            SubmodelElementList => false,
            _ => false
        };
    }

    public List<string> GetPath()
    {
        var upPath = new List<string>();

        // Traverse upwards from the current node and collect the path
        var currentNode = this;
        while (currentNode != null)
        {
            upPath.Add(currentNode.Text);
            currentNode = currentNode.Parent as TreeItem;
        }

        // Reverse the path to get the downward path
        upPath.Reverse();

        return upPath;
    }
    
    private bool IsReadme()
    {
        return Tag is string && Text.Contains("/readme");
    }

    private bool IsEncrypted()
    {
        return Program.envSymbols[EnvironmentIndex] == EncryptionEnvironmentSymbol;
    }

    private static string GetIdShortFromTag(object tag)
    {
        return tag switch
        {
            File f => f.IdShort,
            Blob blob => blob.IdShort,
            Range range => range.IdShort,
            MultiLanguageProperty multiLanguageProperty => multiLanguageProperty.IdShort,
            _ => "NULL" //This based on the previous implementation and I don't know the side effects of changing it yet, so it should stay this way for now.
        };
    }

    private static string TranslateSymbol(string symbol)
    {
        return symbol switch
        {
            "L" => "ENCRYPTED",
            "S" => "SIGNED",
            "V" => "VALIDATED",
            _ => string.Empty
        };
    }
}