namespace AasxServerDB.Entities
{
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]

    public class EnvSet
    {
        // id
        public int Id { get; set; }

        // path
        public string? Path { get; set; }

        // cd, aas, sm
        public virtual ICollection<CDSet?> CDSets  { get; } = new List<CDSet?>();
        public virtual ICollection<AASSet> AASSets { get; } = new List<AASSet>();
        public virtual ICollection<SMSet?> SMSets  { get; } = new List<SMSet?>();
    }
}