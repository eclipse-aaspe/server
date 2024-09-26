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

using System.ComponentModel.DataAnnotations.Schema;

namespace AasxServerDB.Entities
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
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
        public string? IdShort                    { get; set; }
        public string? DisplayName                { get; set; }
        [StringLength(128)]
        public string? Category                   { get; set; }
        public string? Description                { get; set; }
        public string? Extensions                 { get; set; }
        [MaxLength(2000)]
        public string? Identifier                 { get; set; }
        [StringLength(8)]
        public string? Kind                       { get; set; }
        [MaxLength(2000)]
        public string? SemanticId                 { get; set; } // change to save the rest of the reference
        public string? SupplementalSemanticIds    { get; set; }
        public string? Qualifiers                 { get; set; }
        public string? EmbeddedDataSpecifications { get; set; }

        // administration
        [StringLength(4)]
        public string? Version                     { get; set; }
        [StringLength(4)]
        public string? Revision                    { get; set; }
        public string? Creator                     { get; set; }
        [MaxLength(2000)]
        public string? TemplateId                  { get; set; }
        public string? AEmbeddedDataSpecifications { get; set; }

        // time stamp
        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp       { get; set; }
        public DateTime TimeStampTree   { get; set; }
        public DateTime TimeStampDelete { get; set; }

        // sme
        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();
    }
}