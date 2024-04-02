using System.Collections.Generic;
using System.Text;
using AasxServer;
using Blob = AasCore.Aas3_0.Blob;

namespace AasxServerBlazor.TreeVisualisation;

public class TreeItem
{
    private const string EnvironmentSymbol = "L";
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
            case null when Program.envSymbols[EnvironmentIndex] == EnvironmentSymbol:
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
}