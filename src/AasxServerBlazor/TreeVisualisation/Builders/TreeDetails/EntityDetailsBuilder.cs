using System;
using System.Linq;
using Extensions;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails
{
    public class EntityDetailsBuilder : BaseTreeDetailsBuilder
    {
        private const string SemanticIdHeader = "Semantic ID";
        private const string EntityTypeHeader = "Entity Type";
        private const string AssetHeader = "Asset";

        public override string Build(TreeItem treeItem, int line, int column)
        {
            var entity = (Entity)treeItem.Tag;

            return line switch
            {
                0 => BuildSemanticIdRow(entity, column),
                1 => BuildEntityTypeRow(entity, column),
                2 when entity.EntityType == EntityType.SelfManagedEntity => BuildAssetRow(entity, column),
                3 => GetQualifiers(entity.Qualifiers),
                _ => string.Empty
            };
        }

        private static string BuildSemanticIdRow(IEntity entity, int column)
        {
            return column switch
            {
                0 => SemanticIdHeader,
                1 => entity.SemanticId?.GetAsExactlyOneKey()?.ToString() ?? NullValueName,
                _ => string.Empty
            };
        }

        private static string BuildEntityTypeRow(IEntity entity, int column)
        {
            return column switch
            {
                0 => EntityTypeHeader,
                1 => $"{entity.EntityType}",
                _ => string.Empty
            };
        }

        private static string BuildAssetRow(IEntity entity, int column)
        {
            return column switch
            {
                0 => AssetHeader,
                1 => entity.GlobalAssetId != null
                        ? $"[{string.Join(", ", entity.SpecificAssetIds?.Select(s => s.Value).Where(k => !string.IsNullOrEmpty(k)) ?? Array.Empty<string>())}]"
                        : string.Empty,
                _ => string.Empty
            };
        }
    }
}
