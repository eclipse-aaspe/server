using System.Linq;
using System.Net;
using Microsoft.IdentityModel.Tokens;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class AssetAdministrationShellDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string IdHeader = "ID";
        private const string AssetHeader = "ASSET";
        private const string AssetIdHeader = "ASSETID";
        private const string AssetIdUrlEncodedHeader = "ASSETID URLENCODED";
        private const string ExtensionsHeader = "Extensions";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var aas = (AssetAdministrationShell) treeItem.Tag;

            return line switch
            {
                0 => BuildIdRow(aas, column),
                1 => BuildAssetRow(aas, column),
                2 => BuildAssetIdRow(aas, column),
                3 => BuildAssetIdUrlEncodedRow(aas, column),
                4 => BuildExtensionsRow(aas, column),
                _ => string.Empty
            };
        }

        private static string BuildIdRow(IAssetAdministrationShell aas, int column)
        {
            return column switch
            {
                0 => IdHeader,
                1 => $"{aas.Id}",
                2 => $" ==> {Base64UrlEncoder.Encode(aas.Id)}",
                _ => string.Empty
            };
        }

        private static string BuildAssetRow(IAssetAdministrationShell aas, int column)
        {
            return column switch
            {
                0 => AssetHeader,
                1 => aas.AssetInformation.GlobalAssetId,
                _ => string.Empty
            };
        }

        private static string BuildAssetIdRow(IAssetAdministrationShell aas, int column)
        {
            return column switch
            {
                0 => AssetIdHeader,
                1 => $"{aas.AssetInformation.GlobalAssetId}",
                2 => aas.AssetInformation.GlobalAssetId != null ? $" ==> {Base64UrlEncoder.Encode(aas.AssetInformation.GlobalAssetId)}" : string.Empty,
                _ => string.Empty
            };
        }

        private static string BuildAssetIdUrlEncodedRow(IAssetAdministrationShell aas, int column)
        {
            return column switch
            {
                0 => AssetIdUrlEncodedHeader,
                1 => WebUtility.UrlEncode(aas.AssetInformation.GlobalAssetId),
                _ => string.Empty
            };
        }

        private static string BuildExtensionsRow(IHasExtensions aas, int column)
        {
            return column switch
            {
                0 => ExtensionsHeader,
                1 => aas.Extensions != null ? string.Join("; ", aas.Extensions.Select(e => $"{e.Name} : {e.Value}")) : string.Empty,
                _ => string.Empty
            };
        }
    }
}