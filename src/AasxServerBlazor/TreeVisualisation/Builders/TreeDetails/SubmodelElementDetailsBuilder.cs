using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;

public class SubmodelElementDetailsBuilder : BaseTreeDetailsBuilder
{
    public override string Build(TreeItem treeItem, int line, int column)
    {
        if (treeItem.Tag is not ISubmodelElement submodelElement)
            return string.Empty;

        return line switch
        {
            0 => column switch
            {
                0 => "Semantic ID",
                1 => submodelElement.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            },
            1 => GetQualifiers(submodelElement.Qualifiers),
            _ => string.Empty
        };
    }
}