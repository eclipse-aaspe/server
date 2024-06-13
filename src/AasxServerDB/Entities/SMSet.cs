﻿using System.ComponentModel.DataAnnotations.Schema;

namespace AasxServerDB.Entities
{
    public class SMSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get; set; }
        public virtual AASXSet AASXSet { get; set; }

        [ForeignKey("AASSet")]
        public int? AASId { get; set; }
        public virtual AASSet? AASSet { get; set; }

        public string? SemanticId { get; set; }
        public string? Identifier { get; set; }
        public string? IdShort { get; set; }

        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();
    }
}