namespace AasxServerDB.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using AasCore.Aas3_0;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]

    public class AASSet
    {
        // env
        [ForeignKey("EnvSet")]
        public         int      EnvId  { get; set; }
        public virtual EnvSet? EnvSet { get; set; }

        // aas
        public int Id { get; set; }

        // asset administration shell
        [StringLength(128)]
        public string?                           IdShort            { get; set; }
        public List<ILangStringNameType>?        DisplayName        { get; set; }
        [StringLength(128)]
        public string?                           Category           { get; set; }
        public List<ILangStringTextType>?        Description        { get; set; }
        public List<IExtension>?                 Extensions         { get; set; }
        [MaxLength(2000)]
        public string?                           Identifier         { get; set; }
        public IAdministrativeInformation?       Administration     { get; set; }
        public List<IEmbeddedDataSpecification>? DataSpecifications { get; set; }
        public IReference?                       DerivedFrom        { get; set; }

        // asset information
        public AssetKind?              AssetKind          { get; set; }
        public string?                 GlobalAssetId      { get; set; }
        public string?                 AssetType          { get; set; }
        public List<ISpecificAssetId>? SpecificAssetIds   { get; set; }
        public IResource?              DefaultThumbnail   { get; set; }

        // time stamp
        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }
        public DateTime TimeStampDelete { get; set; }

        // sm
        public virtual ICollection<SMSet> SMSets { get; } = new List<SMSet>();
    }
}