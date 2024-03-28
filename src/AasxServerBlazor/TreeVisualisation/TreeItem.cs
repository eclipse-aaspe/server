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
        var nodeId = "NULL";

        if (Tag == null && Program.envSymbols[EnvironmentIndex] == EnvironmentSymbol)
        {
            nodeId = Text;
        }
        if (Tag is string && Text.Contains("/readme"))
        {
            nodeId = string.Empty;
        }
        if (Tag is AssetAdministrationShell assetAdministrationShell)
        {
            nodeId = assetAdministrationShell.IdShort;
        }
        if (Tag is Submodel submodel)
        {
            nodeId = string.Empty;
            if (submodel.Kind != null && submodel.Kind == ModellingKind.Template)
            {
                nodeId += "<T> ";
            }
            nodeId += submodel.IdShort;
        }
        if (Tag is ISubmodelElement submodelElement)
        {
            nodeId = submodelElement.IdShort;
        }
        if (Tag is File f)
        {
            nodeId = f.IdShort;
        }
        if (Tag is Blob blob)
        {
            nodeId = blob.IdShort;
        }
        if (Tag is Range range)
        {
            nodeId = range.IdShort;
        }
        else if (Tag is MultiLanguageProperty multiLanguageProperty)
        {
            nodeId = multiLanguageProperty.IdShort;
        }
        
        return (nodeId);
    }

}