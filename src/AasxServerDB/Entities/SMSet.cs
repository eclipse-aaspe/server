namespace AasxServerDB.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using AasCore.Aas3_0;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]
    [Index(nameof(EnvId))]
    [Index(nameof(AASId))]
    [Index(nameof(SemanticId))]
    [Index(nameof(Identifier))]
    [Index(nameof(TimeStampTree))]

    public class SMSet
    {
        // env
        [ForeignKey("EnvSet")]
        public         int      EnvId  { get; set; }
        public virtual EnvSet? EnvSet { get; set; }

        // aas
        [ForeignKey("AASSet")]
        public         int?    AASId  { get; set; }
        public virtual AASSet? AASSet { get; set; }

        // id
        public int Id { get; set; }

        // submodel
        [StringLength(128)]
        public string?                           IdShort                 { get; set; }
        public List<ILangStringNameType>?        DisplayName             { get; set; }
        [StringLength(128)]
        public string?                           Category                { get; set; }
        public List<ILangStringTextType>?        Description             { get; set; }
        public List<IExtension>?                 Extensions              { get; set; }
        [MaxLength(2000)]
        public string?                           Identifier              { get; set; }
        public IAdministrativeInformation?       Administration          { get; set; }
        [StringLength(8)]
        public ModellingKind?                    Kind                    { get; set; }
        [MaxLength(2000)]
        public string?                           SemanticId              { get; set; } // change to save the rest of the reference
        public List<IReference>?                 SupplementalSemanticIds { get; set; }
        public List<IQualifier>?                 Qualifiers              { get; set; }
        public List<IEmbeddedDataSpecification>? DataSpecifications      { get; set; }

        // time stamp
        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }
        public DateTime TimeStampDelete { get; set; }

        // sme
        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();
    }
}