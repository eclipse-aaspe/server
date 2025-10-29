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
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    // indexes
    [Index(nameof(EnvId))]
    [Index(nameof(CDId))]

    public class EnvCDSet
    {
        // id
        public int Id { get; set; }

        // env
        [ForeignKey("EnvSet")]
        public int EnvId { get; set; }
        public virtual EnvSet? EnvSet { get; set; }

        // cd
        [ForeignKey("CDSet")]
        public int CDId { get; set; }
        public virtual CDSet? CDSet { get; set; }
    }
}
