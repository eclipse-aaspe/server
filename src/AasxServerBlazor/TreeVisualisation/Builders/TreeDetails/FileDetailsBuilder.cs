using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class FileDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string ValueHeader = "Value";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var file = (File)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(file, column),
                1 => BuildValueRow(file, column),
                2 => GetQualifiers(file.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics file, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => file.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildValueRow(IFile file, int column)
        {
            return column switch
            {
                0 => ValueHeader,
                1 => $"{file.Value}",
                _ => string.Empty
            };
        }
    }
}