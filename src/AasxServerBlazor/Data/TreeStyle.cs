
namespace MW.Blazor
{
    public class TreeStyle
    {
        public static readonly TreeStyle Bootstrap = new()
        {
            ExpandNodeIconClass = "far fa-plus-square cursor-pointer",
            CollapseNodeIconClass = "far fa-minus-square cursor-pointer",
            NodeTitleClass = "p-1 cursor-pointer",
            NodeTitleSelectedClass = "bg-primary text-white"
        };

        public string ExpandNodeIconClass { get; set; }
        public string CollapseNodeIconClass { get; set; }
        public string NodeTitleClass { get; set; }
        public string NodeTitleSelectedClass { get; set; }
    }
}
