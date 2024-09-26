/********************************************************************************
* Copyright (c) {2019 - 2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
********************************************************************************/

namespace AasxServerDB.Entities
{
    using Microsoft.EntityFrameworkCore;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.IdentityModel.Tokens;
    using System.ComponentModel.DataAnnotations;

    // indexes
    [Index(nameof(Id))]
    [Index(nameof(SMId))]
    [Index(nameof(ParentSMEId))]
    [Index(nameof(SemanticId))]
    [Index(nameof(IdShort))]
    [Index(nameof(TimeStamp))]

    public class SMESet
    {
        // sm
        [ForeignKey("SMSet")]
        public         int    SMId  { get; set; }
        public virtual SMSet? SMSet { get; set; }

        // parent sme
        public         int?    ParentSMEId       { get; set; }
        public virtual SMESet? ParentSME { get; set; }

        // id
        public int Id { get; set; }

        // sme type
        [Required]
        [StringLength(9)]
        public string? SMEType { get; set; }

        // submodel element
        [StringLength(128)]
        public string? IdShort                    { get; set; }
        public string? DisplayName                { get; set; }
        [StringLength(128)]
        public string? Category                   { get; set; }
        public string? Description                { get; set; }
        public string? Extensions                 { get; set; }
        [MaxLength(2000)]
        public string? SemanticId                 { get; set; } // change to save the rest of the reference
        public string? SupplementalSemanticIds    { get; set; }
        public string? Qualifiers                 { get; set; }
        public string? EmbeddedDataSpecifications { get; set; }

        // value
        [Column(TypeName = "char(1)")]
        public string? TValue { get; set; }
        public virtual ICollection<IValueSet> IValueSets { get; } = new List<IValueSet>();
        public virtual ICollection<DValueSet> DValueSets { get; } = new List<DValueSet>();
        public virtual ICollection<SValueSet> SValueSets { get; } = new List<SValueSet>();
        public List<string[]> GetValue()
        {
            if (TValue == null)
                return [[string.Empty, string.Empty]];

            var list = new List<string[]>();
            switch (TValue)
            {
                case "S":
                    list = new AasContext().SValueSets.Where(s => s.SMEId == Id).ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value ?? string.Empty, valueDB.Annotation ?? string.Empty]);
                    break;
                case "I":
                    list = new AasContext().IValueSets.Where(s => s.SMEId == Id).ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                    break;
                case "D":
                    list = new AasContext().DValueSets.Where(s => s.SMEId == Id).ToList()
                        .ConvertAll<string[]>(valueDB => [valueDB.Value == null ? string.Empty : valueDB.Value.ToString(), valueDB.Annotation ?? string.Empty]);
                    break;
            }

            if (list.Count > 0 || (!SMEType.IsNullOrEmpty() && SMEType.Equals("MLP")))
                return list;

            return [[string.Empty, string.Empty]];
        }

        // object value // additional attributes
        public virtual ICollection<OValueSet> OValueSets { get; } = new List<OValueSet>();
        public Dictionary<string, string> GetOValue()
        {
            var dic = new AasContext().OValueSets.Where(s => s.SMEId == Id).ToList().ToDictionary(valueDB => valueDB.Attribute, valueDB => valueDB.Value);
            if (dic != null)
            {
                return dic;
            }
            return new Dictionary<string, string>();
        }

        // time stamp
        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }
        public DateTime TimeStampDelete { get; set; }
    }
}