using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class OperationDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string CountInputsHeader = "CountInputs";
        private const string CountOutputsHeader = "CountOutputs";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var operation = (Operation)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(operation, column),
                1 => BuildCountInputsRow(operation, column),
                2 => BuildCountOutputsRow(operation, column),
                3 => GetQualifiers(operation.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics operation, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => operation.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildCountInputsRow(IOperation operation, int column)
        {
            return column switch
            {
                0 => CountInputsHeader,
                1 => $"{operation.InputVariables?.Count ?? 0}",
                _ => string.Empty
            };
        }

        private static string BuildCountOutputsRow(IOperation operation, int column)
        {
            return column switch
            {
                0 => CountOutputsHeader,
                1 => $"{operation.OutputVariables?.Count ?? 0}",
                _ => string.Empty
            };
        }
    }
}