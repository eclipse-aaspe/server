using System.Collections.Generic;

namespace AasxServerBlazor.TreeVisualisation;

public static class TreePath
{
    public static TreeItem Find(IReadOnlyList<string> path,IReadOnlyList<TreeItem> items)
    {
        if (path != null && path.Count > 0)
        {
            TreeItem found = null;
            int k = 0;
            while (k < items.Count)
            {
                var i = items[k];
                if (i.Text != path[0])
                {
                    k++;
                    continue;
                }

                var j = 0;
                found = i;
                while (++j < path.Count)
                {
                    if (i.Childs != null)
                    {
                        found = null;
                        foreach (var c in i.Childs)
                        {
                            if (c.Text == path[j])
                            {
                                found = c;
                                break;
                            }
                        }
                    }

                    if (found == null)
                    {
                        return null;
                    }

                    i = found;
                }

                if (found != null)
                    return found;
            }
        }

        return null;
    }

}