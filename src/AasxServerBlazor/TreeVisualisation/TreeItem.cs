using System.Collections.Generic;

namespace AasxServerBlazor.TreeVisualisation;

public class TreeItem
{
    public string Text { get; set; }
    public IEnumerable<TreeItem> Childs { get; set; }
    public object Parent { get; set; }
    public string Type { get; set; }
    public object Tag { get; set; }
    public int EnvironmentIndex { get; set; }
}