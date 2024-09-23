namespace AasxServerDB.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]
    [Index(nameof(SMEId))]
    [Index(nameof(Value))]

    public class OValueSet
    {
        // sme
        [ForeignKey("SMESet")]
        public         int     SMEId  { get; set; }
        public virtual SMESet? SMESet { get; set; }

        // id
        public int Id { get; set; }

        // object value // additional attributes
        public string Attribute { get; set; }
        public string Value     { get; set; }
    }
}
