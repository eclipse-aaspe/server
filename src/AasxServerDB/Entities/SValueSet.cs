using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AasxServerDB.Entities
{
    [Index(nameof(Value))]
    public class SValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get; set; }
        public virtual SMESet? SMESet { get; set; }

        public string? Value { get; set; }
        public string? Annotation { get; set; }
    }
}