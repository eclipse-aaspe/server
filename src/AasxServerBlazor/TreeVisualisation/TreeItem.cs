using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AasxServer;
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

        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(AssetAdministrationShell), "AAS");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(Submodel), "Sub");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(Operation), "Opr");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(File), "File");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(Blob), "Blob");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(Range), "Range");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(MultiLanguageProperty), "Lang");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(RelationshipElement), "Rel");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(ReferenceElement), "Ref");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(Entity), "Ent");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(BasicEventElement), "Evt");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(AnnotatedRelationshipElement), "RelA");
        AppendNodeTypeIfMatchesType(tagObject, nodeType, typeof(Capability), "Cap");
        
        AppendSubmodelElementNodeType(tagObject, nodeType);

        return nodeType.ToString();
    }

    private bool IsReadme()
    {
        return Tag is string && Text.Contains("/readme");
    }

    private bool IsEncrypted()
    {
        return Program.envSymbols[EnvironmentIndex] == EncryptionEnvironmentSymbol;
    }

    private void AppendNodeTypeIfMatchesType(object tagObject, StringBuilder builder, Type type, string appendString)
    {
        if (tagObject is not null && tagObject.GetType() == type)
        {
            builder.Append(appendString);
        }
    }

    private static void AppendSubmodelElementNodeType(object tagObject, StringBuilder builder)
    {
        if (tagObject is not ISubmodelElement submodelElement)
        {
            return;
        }
        switch (submodelElement)
        {
            case SubmodelElementList:
                builder.Append("SML");
                break;
            case SubmodelElementCollection:
                builder.Append("Coll");
                break;
            case Property:
                builder.Append("Prop");
                break;
        }
    }
    
    public string ViewNodeInfo()
    {
        var ret = "";

        var o = Tag;

        if (o is AssetAdministrationShell)
        {
            var aas = o as AssetAdministrationShell;
        }

        if (o is Submodel)
        {
            var sm = o as Submodel;
            if (sm.Qualifiers != null && sm.Qualifiers.Count > 0)
            {
                ret += " @QUALIFIERS";
            }
        }

        if (o is SubmodelElementCollection)
        {
            var sme = o as SubmodelElementCollection;
            if (sme.Value != null)
            {
                if (sme.Value.Count > 0)
                {
                    ret += " #" + sme.Value.Count;
                }
            }

            if (sme.Qualifiers != null && sme.Qualifiers.Count > 0)
            {
                ret += " @QUALIFIERS";
            }
        }

        if (o is ISubmodelElement)
        {
            if (o is Property)
            {
                var prop = o as Property;
                if (prop.Value != null && prop.Value != "")
                {
                    var v = prop.Value;
                    if (v.Length > 100)
                        v = v.Substring(0, 100) + " ..";
                    ret = " = " + v;
                }

                if (prop.Qualifiers != null && prop.Qualifiers.Count > 0)
                {
                    ret += " @QUALIFIERS";
                }
            }

            if (o is AasCore.Aas3_0.File f)
            {
                if (f.Value != null)
                    ret = " = " + f.Value;
                if (f.Qualifiers != null && f.Qualifiers.Count > 0)
                {
                    ret += " @QUALIFIERS";
                }
            }
        }

        if (o is AasCore.Aas3_0.Range)
        {
            var r = o as AasCore.Aas3_0.Range;
            if (r.Min != null && r.Max != null)
                ret = " = " + r.Min + " .. " + r.Max;
            if (r.Qualifiers != null && r.Qualifiers.Count > 0)
            {
                ret += " @QUALIFIERS";
            }
        }

        if (o is MultiLanguageProperty)
        {
            var mlp = o as MultiLanguageProperty;
            var ls = mlp.Value;
            if (ls != null)
            {
                ret = " = ";
                for (var i = 0; i < ls.Count; i++)
                {
                    ret += ls[i].Language + " ";
                    if (i == 0)
                        ret += ls[i].Text + " ";
                }
            }

            if (mlp.Qualifiers != null && mlp.Qualifiers.Count > 0)
            {
                ret += " @QUALIFIERS";
            }
        }

        return (ret);
    }
}