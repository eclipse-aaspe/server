using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AasxServerDB.Entities
{
    [Index(nameof(Id))]
    [Index(nameof(AASXId))]
    [Index(nameof(AASId))]
    [Index(nameof(SemanticId))]
    [Index(nameof(Identifier))]
    [Index(nameof(TimeStampTree))]

    public class SMSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get; set; }
        public virtual AASXSet? AASXSet { get; set; }

        [ForeignKey("AASSet")]
        public int? AASId { get; set; }
        public virtual AASSet? AASSet { get; set; }

        public string? SemanticId { get; set; }
        public string? Identifier { get; set; }
        public string? IdShort { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }
        public DateTime TimeStampDelete { get; set; }

        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();
    }
}