using System;
using Extensions;
using System.Text;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class BlobDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string ContentTypeHeader = "ContentType";
        private const string ValueHeader = "Value";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var blob = (Blob)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(blob, column),
                1 => BuildContentTypeRow(blob, column),
                2 => BuildValueRow(blob, column),
                3 => GetQualifiers(blob.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IHasSemantics blob, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => blob.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildContentTypeRow(IBlob blob, int column)
        {
            return column switch
            {
                0 => ContentTypeHeader,
                1 => $"{blob.ContentType}",
                _ => string.Empty
            };
        }

        private static string BuildValueRow(IBlob blob, int column)
        {
            return column switch
            {
                0 => ValueHeader,
                1 => Encoding.ASCII.GetString(blob.Value ?? Array.Empty<byte>()),
                _ => string.Empty
            };
        }
    }
}