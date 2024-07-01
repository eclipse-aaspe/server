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
    public class AASSet
    {
        public int Id { get; set; }

        [ForeignKey("AASXSet")] public int      AASXId  { get; set; }
        public virtual                 AASXSet? AASXSet { get; set; }

        public string? Identifier    { get; set; }
        public string? IdShort       { get; set; }
        public string? AssetKind     { get; set; }
        public string? GlobalAssetId { get; set; }

        public DateTime TimeStampCreate { get; set; }
        public DateTime TimeStamp { get; set; }
        public DateTime TimeStampTree { get; set; }

        public virtual ICollection<SMSet> SMSets { get; } = new List<SMSet>();
    }
}