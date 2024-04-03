namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;

public class EmptyTreeDetailsBuilder : BaseTreeDetailsBuilder
{
    public override string Build(TreeItem treeItem, int line, int column) => string.Empty;
}