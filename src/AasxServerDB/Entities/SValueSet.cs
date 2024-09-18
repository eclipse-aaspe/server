namespace AasxServerDB.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]
    [Index(nameof(SMEId))]
    [Index(nameof(Value))]

    public class SValueSet
    {
        // sme
        [ForeignKey("SMESet")]
        public         int     SMEId  { get; set; }
        public virtual SMESet? SMESet { get; set; }

        // id
        public int Id { get; set; }

        // attributes
        public string? Value      { get; set; }
        public string? Annotation { get; set; }
    }
}