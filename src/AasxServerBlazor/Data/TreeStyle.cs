
namespace AasxServerBlazor.Data;

public class TreeStyle
{
    public static readonly TreeStyle Bootstrap = new()
    {
        ExpandNodeIconClass = "far fa-plus-square cursor-pointer",
        CollapseNodeIconClass = "far fa-minus-square cursor-pointer",
        NodeTitleClass = "p-1 cursor-pointer",
        NodeTitleSelectedClass = "selected-node"
    };

    public string ExpandNodeIconClass { get; private init; }
    public string CollapseNodeIconClass { get; private init; }
    public string NodeTitleClass { get; private init; }
    public string NodeTitleSelectedClass { get; private init; }
}