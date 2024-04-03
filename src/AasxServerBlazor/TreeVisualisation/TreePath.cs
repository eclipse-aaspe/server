using System.Collections.Generic;
using System.Linq;

namespace AasxServerBlazor.TreeVisualisation;

public static class TreePath
{
    public static TreeItem Find(IReadOnlyList<string> path, IReadOnlyList<TreeItem> items)
    {
        var isPathNonExisting = !(path?.Any() ?? false);
        var isItemsNonExisting = !(items?.Any() ?? false);
        
        if (isPathNonExisting || isItemsNonExisting)
        {
            return null;
        }

        return FindRecursive(path, items);
    }

    private static TreeItem FindRecursive(IReadOnlyList<string> path, IEnumerable<TreeItem> items)
    {
        var item = items.FirstOrDefault(i => i.Text == path[0]);
        if (item == null)
        {
            return null;
        }

        return path.Count == 1
            ? item
            : FindRecursive(path.Skip(1).ToList(), item.Childs);
    }
}