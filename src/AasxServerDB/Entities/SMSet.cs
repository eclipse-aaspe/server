/********************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
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
    public class SMSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")]
        public int AASXId { get;               set; }
        public virtual AASXSet? AASXSet { get; set; }

        [ForeignKey("AASSet")]
        public int? AASId { get; set; }
        public virtual AASSet? AASSet { get; set; }

        public string? SemanticId { get; set; }
        public string? Identifier { get; set; }
        public string? IdShort    { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<SMESet> SMESets { get; } = new List<SMESet>();
    }
}