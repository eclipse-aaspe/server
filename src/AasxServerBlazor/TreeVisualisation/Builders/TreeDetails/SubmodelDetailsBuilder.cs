using System.Linq;
using Extensions;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class SubmodelDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string IdHeader = "ID";
        private const string SemanticIdHeader = "Semantic ID";
        private const string ExtensionsHeader = "Extensions";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var submodel = (Submodel)treeItem.Tag;

            return line switch
            {
                0 => BuildIdRow(submodel, column),
                1 => BuildSemanticIdRow(submodel, column),
                2 => GetQualifiers(submodel.Qualifiers),
                3 => BuildExtensionsRow(submodel, column),
                _ => string.Empty
            };
        }

        private static string BuildIdRow(IIdentifiable submodel, int column)
        {
            return column switch
            {
                0 => IdHeader,
                1 => $"{submodel.Id}",
                2 => $" ==> {Base64UrlEncoder.Encode(submodel.Id)}",
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics submodel, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => submodel.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildExtensionsRow(IHasExtensions submodel, int column)
        {
            return column switch
            {
                0 => ExtensionsHeader,
                1 => submodel.Extensions != null ? string.Join("; ", submodel.Extensions.Select(e => $"{e.Name} : {e.Value}")) : string.Empty,
                _ => string.Empty
            };
        }
    }
}
