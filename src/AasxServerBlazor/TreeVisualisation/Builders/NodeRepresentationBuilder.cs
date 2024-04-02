using System;
using System.Text;

namespace AasxServerBlazor.TreeVisualisation.Builders
{
    /// <summary>
    /// Used to build the <see cref="TreeItem"/> node information
    /// </summary>
    internal static class NodeRepresentationBuilder
    {
        private const string SubmodelElementNodeTypeIdentifier = "SML";
        private const string SubmodelElementCollectionIdentifier = "Coll";
        private const string SubmodelPropertyIdentifier = "Prop";

        /// <summary>
        /// Appends node type string if the provided tag object matches the specified type.
        /// </summary>
        /// <param name="tagObject">The tag object to check.</param>
        /// <param name="type">The type to match.</param>
        /// <param name="appendString">The string to append if the type matches.</param>
        /// <returns>The appended string if the type matches; otherwise, an empty string.</returns>
        public static string AppendNodeTypeIfMatchesType(object tagObject, Type type, string appendString)
        {
            return tagObject is not null && tagObject.GetType() == type ? appendString : string.Empty;
        }

        /// <summary>
        /// Appends node type based on the submodel element type.
        /// </summary>
        /// <param name="tagObject">The tag object representing a submodel element.</param>
        public static string AppendSubmodelElementNodeType(object tagObject)
        {
            if (tagObject is not ISubmodelElement submodelElement)
                return string.Empty;

            return submodelElement switch
            {
                SubmodelElementList => SubmodelElementNodeTypeIdentifier,
                SubmodelElementCollection => SubmodelElementCollectionIdentifier,
                Property => SubmodelPropertyIdentifier,
                _ => string.Empty
            };
        }
    }
}