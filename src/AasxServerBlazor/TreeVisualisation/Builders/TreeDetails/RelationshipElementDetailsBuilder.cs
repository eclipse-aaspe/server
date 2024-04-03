using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class RelationshipElementDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string FirstHeader = "First";
        private const string SecondHeader = "Second";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var relationshipElement = (RelationshipElement)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(relationshipElement, column),
                1 => BuildFirstRow(relationshipElement, column),
                2 => BuildSecondRow(relationshipElement, column),
                3 => GetQualifiers(relationshipElement.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics relationshipElement, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => relationshipElement.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildFirstRow(IRelationshipElement relationshipElement, int column)
        {
            return column switch
            {
                0 => FirstHeader,
                1 => relationshipElement.First?.Keys.ToStringExtended() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildSecondRow(IRelationshipElement relationshipElement, int column)
        {
            return column switch
            {
                0 => SecondHeader,
                1 => relationshipElement.Second?.Keys.ToStringExtended() ?? NullValueName,
                _ => string.Empty
            };
        }
    }
}
