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

namespace AasxServerDB.Context
{

    using Microsoft.EntityFrameworkCore;

    public class PostgreAasContext : AasContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var connectionString = GetConnectionString();
            options.UseNpgsql(connectionString);
        }
    }
}
