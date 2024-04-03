using System.Collections.Generic;
using System.Linq;
using System.Net;
using Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.TreeVisualisation.Builders;

public class TreeNodeBuilder
{
    public string CreateDetails(TreeItem treeItem, int line, int column)
    {
        if (treeItem == null)
        {
            return string.Empty;
        }

        var treeDetailsInformation = string.Empty;

        var treeItemTag = treeItem.Tag;

        if (treeItemTag == null)
        {
            return string.Empty;
        }

        if (treeItemTag is AssetAdministrationShell)
        {
            string subjectIssuer = null;

            treeDetailsInformation = string.Empty;
            var aas = treeItemTag as AssetAdministrationShell;

            var asset = aas.AssetInformation;

            switch (line)
            {
                case 0:
                    treeDetailsInformation = column switch
                    {
                        0 => "ID",
                        1 => aas.Id + string.Empty,
                        2 => " ==> " + Base64UrlEncoder.Encode(aas.Id) + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 1:
                    if (asset != null)
                    {
                        treeDetailsInformation = column switch
                        {
                            0 => "ASSET",
                            1 => asset.GlobalAssetId,
                            _ => treeDetailsInformation
                        };
                    }

                    break;
                case 2:
                    if (asset != null)
                    {
                        switch (column)
                        {
                            case 0:
                                treeDetailsInformation = "ASSETID";
                                break;
                            case 1:
                                treeDetailsInformation = asset.GlobalAssetId + string.Empty;
                                break;
                            case 2:
                            {
                                if (asset.GlobalAssetId != null)
                                {
                                    treeDetailsInformation = " ==> " + Base64UrlEncoder.Encode(asset.GlobalAssetId) + string.Empty;
                                }

                                break;
                            }
                        }
                    }

                    break;
                case 3:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "ASSETID URLENCODED";
                            break;
                        case 1:
                        {
                            var url = WebUtility.UrlEncode(asset.GlobalAssetId);
                            treeDetailsInformation = url;
                            break;
                        }
                    }

                    break;
                case 4:
                    if (aas.Extensions != null)
                    {
                        treeDetailsInformation = column switch
                        {
                            0 => "Extensions",
                            1 => aas.Extensions.Aggregate(string.Empty, (current, e) => current + (e.Name + " : " + e.Value + "; ")),
                            _ => treeDetailsInformation
                        };
                    }

                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is Submodel)
        {
            var submodel = treeItemTag as Submodel;
            treeDetailsInformation = string.Empty;
            switch (line)
            {
                case 0:
                    treeDetailsInformation = column switch
                    {
                        0 => "ID",
                        1 => submodel.Id + string.Empty,
                        2 => " ==> " + Base64UrlEncoder.Encode(submodel.Id) + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 1:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = submodel.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 2:
                    treeDetailsInformation = getQualifiers(submodel.Qualifiers, column);
                    break;
                case 3:
                    if (submodel.Extensions != null)
                    {
                        treeDetailsInformation = column switch
                        {
                            0 => "Extensions",
                            1 => submodel.Extensions.Aggregate(string.Empty, (current, e) => current + (e.Name + " : " + e.Value + "; ")),
                            _ => treeDetailsInformation
                        };
                    }

                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is Property)
        {
            var prop = treeItemTag as Property;
            treeDetailsInformation = string.Empty;
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = prop.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = column switch
                    {
                        0 => "Value Type",
                        1 => prop.ValueType + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 2:
                    treeDetailsInformation = column switch
                    {
                        0 => "Value",
                        1 => prop.Value + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 3:
                    treeDetailsInformation = getQualifiers(prop.Qualifiers, column);
                    break;
                case 4:
                    if (column == 0)
                        treeDetailsInformation = string.Empty;
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is Entity entity)
        {
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = entity.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = column switch
                    {
                        0 => "Entity Type",
                        1 => entity.EntityType + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 2:
                    treeDetailsInformation = string.Empty;
                    if (entity.EntityType == EntityType.SelfManagedEntity)
                    {
                        if (entity.GlobalAssetId != null)
                        {
                            switch (column)
                            {
                                case 0:
                                    treeDetailsInformation = "Asset";
                                    break;
                                case 1:
                                {
                                    break;
                                }
                            }
                        }

                        if (entity.SpecificAssetIds != null)
                        {
                            switch (column)
                            {
                                case 0:
                                    treeDetailsInformation = "Asset";
                                    break;
                                case 1:
                                {
                                    foreach (var k in entity.SpecificAssetIds.Select(specificAssetId => specificAssetId.Value).Where(k => !string.IsNullOrEmpty(k)))
                                    {
                                        treeDetailsInformation = "[" + k + "]";
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    break;
                case 3:
                    treeDetailsInformation = getQualifiers(entity.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is AasCore.Aas3_0.File file)
        {
            treeDetailsInformation += ", " + file.Value;
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = file.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = column switch
                    {
                        0 => "Value",
                        1 => file.Value,
                        _ => treeDetailsInformation
                    };
                    break;
                case 2:
                    treeDetailsInformation = getQualifiers(file.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is Blob blob)
        {
            treeDetailsInformation = string.Empty;
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = blob.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = column switch
                    {
                        0 => "ContentType",
                        1 => blob.ContentType,
                        _ => treeDetailsInformation
                    };
                    break;
                case 2:
                    treeDetailsInformation = column switch
                    {
                        0 => "Value",
                        1 => System.Text.Encoding.ASCII.GetString(blob.Value),
                        _ => treeDetailsInformation
                    };
                    break;
                case 3:
                    treeDetailsInformation = getQualifiers(blob.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is AasCore.Aas3_0.Range range)
        {
            treeDetailsInformation = range.IdShort;
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var k = range.SemanticId?.GetAsExactlyOneKey();
                            if (k != null)
                            {
                                treeDetailsInformation = $"[{k.Type}, {k.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = column switch
                    {
                        0 => "Min",
                        1 => range.Min + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 2:
                    treeDetailsInformation = column switch
                    {
                        0 => "Max",
                        1 => range.Max + string.Empty,
                        _ => treeDetailsInformation
                    };
                    break;
                case 3:
                    treeDetailsInformation = getQualifiers(range.Qualifiers, column);
                    break;
                case 4:
                    if (column == 0)
                        treeDetailsInformation = string.Empty;
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is Operation operation)
        {
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = operation.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "CountInputs";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "0";
                            if (operation.InputVariables != null)
                            {
                                treeDetailsInformation = operation.InputVariables.Count + string.Empty;
                            }

                            break;
                        }
                    }

                    break;
                case 2:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "CountOutputs";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "0";
                            if (operation.OutputVariables != null)
                            {
                                treeDetailsInformation = operation.OutputVariables.Count + string.Empty;
                            }

                            break;
                        }
                    }

                    break;
                case 3:
                    treeDetailsInformation = getQualifiers(operation.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is AnnotatedRelationshipElement)
        {
            var annotatedRelationshipElement = treeItemTag as AnnotatedRelationshipElement;
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = annotatedRelationshipElement.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "First";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = annotatedRelationshipElement.First;
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = semanticIdKey.Keys.ToStringExtended();
                            }

                            break;
                        }
                    }

                    break;
                case 2:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Second";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = annotatedRelationshipElement.Second;
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = semanticIdKey.Keys.ToStringExtended();
                            }

                            break;
                        }
                    }

                    break;
                case 3:
                    if (column == 0)
                        treeDetailsInformation = getQualifiers(annotatedRelationshipElement.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is RelationshipElement relationshipElement)
        {
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = relationshipElement.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "First";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = relationshipElement.First;
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = semanticIdKey.Keys.ToStringExtended();
                            }

                            break;
                        }
                    }

                    break;
                case 2:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Second";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = relationshipElement.Second;
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = semanticIdKey.Keys.ToStringExtended();
                            }

                            break;
                        }
                    }

                    break;
                case 3:
                    treeDetailsInformation = getQualifiers(relationshipElement.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is ReferenceElement referenceElement)
        {
            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = referenceElement.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Value";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            if (referenceElement.Value != null)
                            {
                                var semanticIdKey = referenceElement.Value.Keys;
                                if (semanticIdKey != null)
                                {
                                    treeDetailsInformation = semanticIdKey.ToStringExtended();
                                }
                            }

                            break;
                        }
                    }

                    break;
                case 2:
                    treeDetailsInformation = getQualifiers(referenceElement.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is MultiLanguageProperty multiLanguageProperty)
        {
            var langStringTextTypes = multiLanguageProperty.Value;
            treeDetailsInformation = langStringTextTypes.Aggregate(treeDetailsInformation, (current, t) => current + (t.Language + " "));

            switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var semanticIdKey = multiLanguageProperty.SemanticId?.GetAsExactlyOneKey();
                            if (semanticIdKey != null)
                            {
                                treeDetailsInformation = $"[{semanticIdKey.Type}, {semanticIdKey.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = getQualifiers(multiLanguageProperty.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    if (langStringTextTypes.Count > line - 2)
                    {
                        treeDetailsInformation = column switch
                        {
                            0 => langStringTextTypes[line - 2].Language,
                            1 => langStringTextTypes[line - 2].Text + string.Empty,
                            _ => treeDetailsInformation
                        };
                    }

                    break;
            }

            return treeDetailsInformation;
        }

        if (treeItemTag is not ISubmodelElement submodelElement) return (treeDetailsInformation);
        switch (line)
            {
                case 0:
                    switch (column)
                    {
                        case 0:
                            treeDetailsInformation = "Semantic ID";
                            break;
                        case 1:
                        {
                            treeDetailsInformation = "NULL";
                            var k = submodelElement.SemanticId?.GetAsExactlyOneKey();
                            if (k != null)
                            {
                                treeDetailsInformation = $"[{k.Type}, {k.Value}]";
                            }

                            break;
                        }
                    }

                    break;
                case 1:
                    treeDetailsInformation = getQualifiers(submodelElement.Qualifiers, column);
                    break;
                default:
                    treeDetailsInformation = string.Empty;
                    break;
            }

        return treeDetailsInformation;
    }
     
     
    private static string getQualifiers(IReadOnlyList<IQualifier> q, int col)
    {
        var qualifiers = string.Empty;

        if (q == null || q.Count <= 0) return qualifiers;
        switch (col)
        {
            case 0:
                qualifiers = "Qualifiers";
                break;
            case 1:
            {
                qualifiers = string.Empty;
                for (var i = 0; i < q.Count; i++)
                {
                    if (i != 0)
                        qualifiers += ", ";
                    if (q[i].Type != null && q[i].Type != string.Empty)
                    {
                        qualifiers += q[i].Type + " ";
                    }

                    if (q[i].Value != null && q[i].Value != string.Empty)
                    {
                        qualifiers += "= " + q[i].Value;
                    }
                }

                break;
            }
        }

        return qualifiers;
    }
}