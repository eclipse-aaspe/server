namespace AasxServerDB.Entities
{
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]

    public class AASXSet
    {
        // id
        public int Id { get; set; }

        // path
        public string? AASX { get; set; }

        // aas
        public virtual ICollection<AASSet> AASSets { get; } = new List<AASSet>();

        // sm
        public virtual ICollection<SMSet?> SMSets  { get; } = new List<SMSet?>();
    }
}