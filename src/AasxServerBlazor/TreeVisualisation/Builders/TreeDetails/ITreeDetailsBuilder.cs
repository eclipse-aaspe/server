namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;

public interface ITreeDetailsBuilder
{
    string Build(TreeItem treeItem, int line, int column);
}