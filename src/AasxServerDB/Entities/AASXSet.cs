namespace AasxServerDB.Entities
{
    public class AASXSet
    {
        public int Id { get; set; }

        public string? AASX { get; set; }

        public virtual ICollection<AASSet> AASSets { get; } = new List<AASSet>();
        public virtual ICollection<SMSet?> SMSets  { get; } = new List<SMSet?>();
    }
}