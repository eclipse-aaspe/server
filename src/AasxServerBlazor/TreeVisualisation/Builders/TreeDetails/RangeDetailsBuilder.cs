using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class RangeDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string MinHeader = "Min";
        private const string MaxHeader = "Max";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var range = (Range)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(range, column),
                1 => BuildMinRow(range, column),
                2 => BuildMaxRow(range, column),
                3 => GetQualifiers(range.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics range, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => range.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildMinRow(IRange range, int column)
        {
            return column switch
            {
                0 => MinHeader,
                1 => $"{range.Min}",
                _ => string.Empty
            };
        }

        private static string BuildMaxRow(IRange range, int column)
        {
            return column switch
            {
                0 => MaxHeader,
                1 => $"{range.Max}",
                _ => string.Empty
            };
        }
    }
}