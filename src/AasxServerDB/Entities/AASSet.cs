using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Nodes;

namespace AasxServerDB.Entities
{
    public class AASSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public         int      AASXId  { get; set; }
        public virtual AASXSet? AASXSet { get; set; }

        public string? Identifier       { get; set; }
        public string? IdShort          { get; set; }
        public string? AssetKind        { get; set; }
        public string? GlobalAssetId    { get; set; }
        public JsonArray? Extensions { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }
        public DateTime TimeStampDelete { get; set; }

        public virtual ICollection<SMSet> SMSets { get; } = new List<SMSet>();
    }
}