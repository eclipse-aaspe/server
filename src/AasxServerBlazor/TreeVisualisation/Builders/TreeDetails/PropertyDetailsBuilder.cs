using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class PropertyDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string ValueTypeHeader = "Value Type";
        private const string ValueHeader = "Value";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var prop = (Property)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(prop, column),
                1 => BuildValueTypeRow(prop, column),
                2 => BuildValueRow(prop, column),
                3 => GetQualifiers(prop.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics prop, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => prop.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildValueTypeRow(IProperty prop, int column)
        {
            return column switch
            {
                0 => ValueTypeHeader,
                1 => $"{prop.ValueType}",
                _ => string.Empty
            };
        }

        private static string BuildValueRow(IProperty prop, int column)
        {
            return column switch
            {
                0 => ValueHeader,
                1 => $"{prop.Value}",
                _ => string.Empty
            };
        }
    }
}