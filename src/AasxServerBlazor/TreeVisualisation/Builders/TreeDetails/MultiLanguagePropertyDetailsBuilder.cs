using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class MultiLanguagePropertyDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var multiLanguageProperty = (MultiLanguageProperty)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(multiLanguageProperty, column),
                1 => GetQualifiers(multiLanguageProperty.Qualifiers),
                _ => BuildValueRow(multiLanguageProperty, line, column)
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics multiLanguageProperty, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => multiLanguageProperty.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildValueRow(IMultiLanguageProperty multiLanguageProperty, int line, int column)
        {
            if (multiLanguageProperty?.Value?.Count > line - 2)
            {
                return column switch
                {
                    0 => multiLanguageProperty.Value[line - 2].Language,
                    1 => multiLanguageProperty.Value[line - 2].Text,
                    _ => string.Empty
                };
            }
            return string.Empty;
        }
    }
}