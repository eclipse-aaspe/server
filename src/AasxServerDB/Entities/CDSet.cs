/********************************************************************************
* Copyright (c) {2019 - 2025} Contributors to the Eclipse Foundation
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
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]

    public class CDSet
    {
        // env
        public virtual ICollection<EnvCDSet?> EnvCDSets { get; } = new List<EnvCDSet?>();

        // id
        public int Id { get; set; }

        // concept description
        [StringLength(128)]
        public string? IdShort                    { get; set; }
        public string? DisplayName                { get; set; }
        [StringLength(128)]
        public string? Category                   { get; set; }
        public string? Description                { get; set; }
        public string? Extensions                 { get; set; }
        [MaxLength(2000)]
        public string? Identifier                 { get; set; }
        public string? IsCaseOf                   { get; set; }
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
    }
}
