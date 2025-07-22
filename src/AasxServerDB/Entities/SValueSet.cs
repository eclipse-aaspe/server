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
    using System;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(Id))]
    [Index(nameof(SMEId))]
    [Index(nameof(Value))]

    public class SValueSet
    {
        // sme
        [ForeignKey("SMESet")]
        public         int     SMEId  { get; set; }
        public virtual SMESet? SMESet { get; set; }

        // id
        public int Id { get; set; }

        // string value
        public string? Value      { get; set; }
        public string? Annotation { get; set; }
    }
}
