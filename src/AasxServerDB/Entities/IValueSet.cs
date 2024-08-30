using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace AasxServerDB.Entities
{
    [Index(nameof(Value))]
    public class IValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public         int     SMEId  { get; set; }
        public virtual SMESet? SMESet { get; set; }

        public long?   Value { get; set; }
        public string? Annotation { get; set; }
    }
}