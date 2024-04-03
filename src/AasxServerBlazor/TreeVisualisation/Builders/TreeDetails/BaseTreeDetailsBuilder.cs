using System.Collections.Generic;
using System.Linq;

namespace AasxServerBlazor.TreeVisualisation.Builders.TreeDetails;

public abstract class BaseTreeDetailsBuilder : ITreeDetailsBuilder
{
    protected const string NullValueName = "NULL";

    public abstract string Build(TreeItem treeItem, int line, int column);

    protected static string GetQualifiers(IReadOnlyList<IQualifier> qualifiers)
    {
        return qualifiers != null && qualifiers.Any()
            ? "Qualifiers, " + string.Join(", ", qualifiers.Select(q => $"{q.Type} {(q.Value != null ? $"= {q.Value}" : "")}"))
            : string.Empty;
    }
}