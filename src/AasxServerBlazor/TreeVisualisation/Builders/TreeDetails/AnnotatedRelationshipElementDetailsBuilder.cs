using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class AnnotatedRelationshipElementDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string FirstHeader = "First";
        private const string SecondHeader = "Second";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var annotatedRelationshipElement = (AnnotatedRelationshipElement) treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(annotatedRelationshipElement, column),
                1 => BuildFirstRow(annotatedRelationshipElement, column),
                2 => BuildSecondRow(annotatedRelationshipElement, column),
                3 => column == 0 ? GetQualifiers(annotatedRelationshipElement.Qualifiers) : string.Empty,
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IAnnotatedRelationshipElement annotatedRelationshipElement, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => annotatedRelationshipElement.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildFirstRow(IAnnotatedRelationshipElement annotatedRelationshipElement, int column)
        {
            return column switch
            {
                0 => FirstHeader,
                1 => annotatedRelationshipElement.First?.Keys.ToStringExtended() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildSecondRow(IAnnotatedRelationshipElement annotatedRelationshipElement, int column)
        {
            return column switch
            {
                0 => SecondHeader,
                1 => annotatedRelationshipElement.Second?.Keys.ToStringExtended() ?? NullValueName,
                _ => string.Empty
            };
        }
    }
}