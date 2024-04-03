using System.Collections.Generic;
using System.Net;
using Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.TreeVisualisation.Builders;

public class TreeNodeBuilder
{
    public string CreateDetails(TreeItem treeItem, int line, int col)
    {
        if (treeItem == null)
        {
            return "";
        }

        var ret = "";

        var o = treeItem.Tag;

        if (o == null)
        {
            return "";
        }

        if (o is AssetAdministrationShell)
        {
            string subjectIssuer = null;

            ret = "";
            var aas = o as AssetAdministrationShell;

            var asset = aas.AssetInformation;

            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "ID";
                    if (col == 1)
                        ret = aas.Id + "";
                    if (col == 2)
                        ret = " ==> " + Base64UrlEncoder.Encode(aas.Id) + "";
                    break;
                case 1:
                    if (asset != null)
                    {
                        if (col == 0)
                            ret = "ASSET";
                        if (col == 1)
                            ret = asset.GlobalAssetId;
                    }

                    break;
                case 2:
                    if (asset != null)
                    {
                        if (col == 0)
                            ret = "ASSETID";
                        if (col == 1)
                            ret = asset.GlobalAssetId + "";
                        if (col == 2)
                            if (asset.GlobalAssetId != null)
                            {
                                ret = " ==> " + Base64UrlEncoder.Encode(asset.GlobalAssetId) + "";
                            }
                    }

                    break;
                case 3:
                    if (asset != null)
                    {
                        if (col == 0)
                            ret = "ASSETID URLENCODED";
                        if (col == 1)
                        {
                            string url = WebUtility.UrlEncode(asset.GlobalAssetId);
                            ret = url;
                        }
                    }

                    break;
                case 4:
                    if (aas.Extensions != null)
                    {
                        if (col == 0)
                            ret = "Extensions";
                        if (col == 1)
                        {
                            ret = "";
                            foreach (var e in aas.Extensions)
                            {
                                ret += e.Name + " : " + e.Value + "; ";
                            }
                        }
                    }

                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is Submodel)
        {
            var sm = o as Submodel;
            ret = "";
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "ID";
                    if (col == 1)
                        ret = sm.Id + "";
                    if (col == 2)
                        ret = " ==> " + Base64UrlEncoder.Encode(sm.Id) + "";
                    break;
                case 1:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = sm.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 2:
                    ret = getQualifiers(sm.Qualifiers, col);
                    break;
                case 3:
                    if (sm.Extensions != null)
                    {
                        if (col == 0)
                            ret = "Extensions";
                        if (col == 1)
                        {
                            ret = "";
                            foreach (var e in sm.Extensions)
                            {
                                ret += e.Name + " : " + e.Value + "; ";
                            }
                        }
                    }

                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is Property)
        {
            var prop = o as Property;
            ret = "";
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = prop.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "Value Type";
                    if (col == 1)
                        ret = prop.ValueType + "";
                    break;
                case 2:
                    if (col == 0)
                        ret = "Value";
                    if (col == 1)
                        ret = prop.Value + "";
                    break;
                case 3:
                    ret = getQualifiers(prop.Qualifiers, col);
                    break;
                case 4:
                    if (col == 0)
                        ret = string.Empty;
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is Entity)
        {
            var e = o as Entity;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = e.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "Entity Type";
                    if (col == 1)
                        ret = e.EntityType + "";
                    break;
                case 2:
                    ret = "";
                    if (e.EntityType == EntityType.SelfManagedEntity)
                    {
                        if (e.GlobalAssetId != null)
                        {
                            if (col == 0)
                                ret = "Asset";
                            if (col == 1)
                            {
                                var k = e.GlobalAssetId;
                            }
                        }

                        if (e.SpecificAssetIds != null)
                        {
                            if (col == 0)
                                ret = "Asset";
                            if (col == 1)
                            {
                                foreach (var specificAssetId in e.SpecificAssetIds)
                                {
                                    var k = specificAssetId.Value;
                                    if (!string.IsNullOrEmpty(k))
                                    {
                                        ret = "[" + k + "]";
                                    }
                                }
                            }
                        }
                    }

                    break;
                case 3:
                    ret = getQualifiers(e.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is AasCore.Aas3_0.File f)
        {
            ret += ", " + f.Value;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = f.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "Value";
                    if (col == 1)
                        ret = f.Value;
                    break;
                case 2:
                    ret = getQualifiers(f.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is Blob b)
        {
            ret = "";
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = b.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "ContentType";
                    if (col == 1)
                        ret = b.ContentType;
                    break;
                case 2:
                    if (col == 0)
                        ret = "Value";
                    if (col == 1)
                        ret = System.Text.Encoding.ASCII.GetString(b.Value);
                    break;
                case 3:
                    ret = getQualifiers(b.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is AasCore.Aas3_0.Range)
        {
            var r = o as AasCore.Aas3_0.Range;
            ret = r.IdShort;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "Min";
                    if (col == 1)
                        ret = r.Min + "";
                    break;
                case 2:
                    if (col == 0)
                        ret = "Max";
                    if (col == 1)
                        ret = r.Max + "";
                    break;
                case 3:
                    ret = getQualifiers(r.Qualifiers, col);
                    break;
                case 4:
                    if (col == 0)
                        ret = string.Empty;
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is Operation)
        {
            var op = o as Operation;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = op.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "CountInputs";
                    if (col == 1)
                    {
                        ret = "0";
                        if (op.InputVariables != null)
                        {
                            ret = op.InputVariables.Count + "";
                        }
                    }

                    break;
                case 2:
                    if (col == 0)
                        ret = "CountOutputs";
                    if (col == 1)
                    {
                        ret = "0";
                        if (op.OutputVariables != null)
                        {
                            ret = op.OutputVariables.Count + "";
                        }
                    }

                    break;
                case 3:
                    ret = getQualifiers(op.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is AnnotatedRelationshipElement)
        {
            var r = o as AnnotatedRelationshipElement;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "First";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.First;
                        if (k != null)
                        {
                            ret = k.Keys.ToStringExtended();
                        }
                    }

                    break;
                case 2:
                    if (col == 0)
                        ret = "Second";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.Second;
                        if (k != null)
                        {
                            ret = k.Keys.ToStringExtended();
                        }
                    }

                    break;
                case 3:
                    if (col == 0)
                        ret = getQualifiers(r.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is RelationshipElement)
        {
            var r = o as RelationshipElement;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "First";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.First;
                        if (k != null)
                        {
                            ret = k.Keys.ToStringExtended();
                        }
                    }

                    break;
                case 2:
                    if (col == 0)
                        ret = "Second";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.Second;
                        if (k != null)
                        {
                            ret = k.Keys.ToStringExtended();
                        }
                    }

                    break;
                case 3:
                    ret = getQualifiers(r.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is ReferenceElement)
        {
            var r = o as ReferenceElement;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = r.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    if (col == 0)
                        ret = "Value";
                    if (col == 1)
                    {
                        ret = "NULL";
                        if (r.Value != null)
                        {
                            var k = r.Value.Keys;
                            if (k != null)
                            {
                                ret = k.ToStringExtended();
                            }
                        }
                    }

                    break;
                case 2:
                    ret = getQualifiers(r.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        if (o is MultiLanguageProperty)
        {
            var mlp = o as MultiLanguageProperty;
            var ls = mlp.Value;
            for (var i = 0; i < ls.Count; i++)
            {
                ret += ls[i].Language + " ";
            }

            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = mlp.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    ret = getQualifiers(mlp.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    if (ls.Count > line - 2)
                    {
                        if (col == 0)
                            ret = ls[line - 2].Language;
                        if (col == 1)
                            ret = ls[line - 2].Text + "";
                    }

                    break;
            }

            return ret;
        }

        if (o is ISubmodelElement)
        {
            var sme = o as ISubmodelElement;
            switch (line)
            {
                case 0:
                    if (col == 0)
                        ret = "Semantic ID";
                    if (col == 1)
                    {
                        ret = "NULL";
                        var k = sme.SemanticId?.GetAsExactlyOneKey();
                        if (k != null)
                        {
                            ret = $"[{k.Type}, {k.Value}]";
                        }
                    }

                    break;
                case 1:
                    ret = getQualifiers(sme.Qualifiers, col);
                    break;
                default:
                    ret = "";
                    break;
            }

            return ret;
        }

        return (ret);
    }
     
     
    private static string getQualifiers(List<IQualifier> q, int col)
    {
        string ret = "";

        if (q != null && q.Count > 0)
        {
            if (col == 0)
                ret = "Qualifiers";
            if (col == 1)
            {
                ret = "";
                for (var i = 0; i < q.Count; i++)
                {
                    if (i != 0)
                        ret += ", ";
                    if (q[i].Type != null && q[i].Type != "")
                    {
                        ret += q[i].Type + " ";
                    }

                    if (q[i].Value != null && q[i].Value != "")
                    {
                        ret += "= " + q[i].Value;
                    }
                }
            }
        }

        return ret;
    }
}