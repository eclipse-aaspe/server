using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Range = AasCore.Aas3_0.Range;

namespace AasxServerBlazor.TreeVisualisation.Builders
{
    internal static class NodeDescriptionBuilder
    {
        private const string QualifiersTag = " @QUALIFIERS";
        private const int MaxPropertyLength = 100;
        public static void AppendSubmodelInfo(IQualifiable submodel, StringBuilder nodeInfoBuilder)
        {
            if (HasQualifiers(submodel))
            {
                nodeInfoBuilder.Append(QualifiersTag);
            }
        }

        public static void AppendCollectionInfo(ISubmodelElementCollection collection, StringBuilder nodeInfoBuilder)
        {
            if (collection.Value?.Any() == true)
            {
                nodeInfoBuilder.Append($" #{collection.Value.Count}");
            }

            if (HasQualifiers(collection))
            {
                nodeInfoBuilder.Append(QualifiersTag);
            }
        }

        public static void AppendPropertyInfo(IProperty property, StringBuilder nodeInfoBuilder)
        {
            if (!string.IsNullOrEmpty(property.Value))
            {
                var value = TruncatePropertyValue(property.Value);
                nodeInfoBuilder.Append($" = {value}");
            }

            if (HasQualifiers(property))
            {
                nodeInfoBuilder.Append(QualifiersTag);
            }
        }

        private static string TruncatePropertyValue(string value)
        {
            return value.Length > MaxPropertyLength 
                ? string.Concat(value.AsSpan(0, MaxPropertyLength), " ..")
                : value;
        }

        public static void AppendFileInfo(IFile file, StringBuilder nodeInfoBuilder)
        {
            if (file.Value != null)
            {
                nodeInfoBuilder.Append($" = {file.Value}");
            }

            if (HasQualifiers(file))
            {
                nodeInfoBuilder.Append(QualifiersTag);
            }
        }

        public static void AppendAdditionalInfo(StringBuilder nodeInfoBuilder, object tag)
        {
            switch (tag)
            {
                case Range rangeObject:
                    AppendRangeInfo(rangeObject, nodeInfoBuilder);
                    break;
                case MultiLanguageProperty multiLanguageProperty:
                    AppendMultiLanguagePropertyInfo(multiLanguageProperty, nodeInfoBuilder);
                    break;
            }
        }

        private static void AppendRangeInfo(IRange rangeObject, StringBuilder nodeInfoBuilder)
        {
            if (rangeObject.Min == null || rangeObject.Max == null)
            {
                return;
            }

            nodeInfoBuilder.Append($" = {rangeObject.Min} .. {rangeObject.Max}");
            AppendQualifiersInfo(rangeObject.Qualifiers, nodeInfoBuilder);
        }

        private static void AppendMultiLanguagePropertyInfo(IMultiLanguageProperty multiLanguageProperty, StringBuilder nodeInfoBuilder)
        {
            var langStringTextTypes = multiLanguageProperty.Value;
            if (langStringTextTypes != null)
            {
                nodeInfoBuilder.Append(" = ");
                for (var i = 0; i < langStringTextTypes.Count; i++)
                {
                    nodeInfoBuilder.Append($"{langStringTextTypes[i].Language} ");
                    if (i == 0)
                    {
                        nodeInfoBuilder.Append($"{langStringTextTypes[i].Text} ");
                    }
                }
            }

            AppendQualifiersInfo(multiLanguageProperty.Qualifiers, nodeInfoBuilder);
        }
        
        private static void AppendQualifiersInfo(IEnumerable<IQualifier> qualifiers, StringBuilder nodeInfoBuilder)
        {
            if (HasQualifiers(qualifiers))
            {
                nodeInfoBuilder.Append(QualifiersTag);
            }
        }

        private static bool HasQualifiers(IQualifiable qualifiable) => qualifiable.Qualifiers != null && qualifiable.Qualifiers.Any();
        private static bool HasQualifiers(IEnumerable<IQualifier> qualifiers) => qualifiers != null && qualifiers.Any();
    }
}
