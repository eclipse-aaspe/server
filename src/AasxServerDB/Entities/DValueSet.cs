using System.ComponentModel.DataAnnotations.Schema;

namespace AasxServerDB.Entities
{
    public class DValueSet
    {
        public int Id { get; set; }

        [ForeignKey("SMESet")]
        public int SMEId { get;              set; }
        public virtual SMESet? SMESet { get; set; }

        public double? Value { get; set; }
        public string? Annotation { get; set; }
    }
}