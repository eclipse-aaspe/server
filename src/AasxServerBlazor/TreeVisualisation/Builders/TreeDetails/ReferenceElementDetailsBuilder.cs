using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class ReferenceElementDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string ValueHeader = "Value";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var referenceElement = (ReferenceElement)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(referenceElement, column),
                1 => BuildValueRow(referenceElement, column),
                2 => GetQualifiers(referenceElement.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics referenceElement, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => referenceElement.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildValueRow(IReferenceElement referenceElement, int column)
        {
            return column switch
            {
                0 => ValueHeader,
                1 => referenceElement.Value != null ? referenceElement.Value.Keys.ToStringExtended() : NullValueName,
                _ => string.Empty
            };
        }
    }
}