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
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]
    [Index(nameof(Identifier))]

    public class SMRefSet
    {
        // aas
        [ForeignKey("AASSet")]
        public int? AASId { get; set; }
        public virtual AASSet? AASSet { get; set; }

        // id
        public int Id { get; set; }

        // submodel
        [StringLength(128)]
        public string? Identifier { get; set; }
    }
}
