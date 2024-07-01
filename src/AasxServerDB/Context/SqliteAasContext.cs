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

using Microsoft.EntityFrameworkCore;

/*
 * https://learn.microsoft.com/en-us/ef/core/get-started/overview/first-app?tabs=netcore-cli
 * 
 * Initial Migration
 * Add-Migration InitialCreate -Context SqliteAasContext -OutputDir Migrations\Sqlite
 * Add-Migration InitialCreate -Context PostgreAasContext -OutputDir Migrations\Postgres
 * 
 * Change database
 * Add-Migration XXX -Context SqliteAasContext
 * Add-Migration XXX -Context PostgreAasContext
 * Update-Database -Context SqliteAasContext
 * Update-Database -Context PostgreAasContext
 */

namespace AasxServerDB.Context
{
    public class SqliteAasContext : AasContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (_con == null)
            {
                options.UseSqlite("");
            }
            else
            {
                var connectionString = _con["DatabaseConnection:ConnectionString"];
                if (connectionString != null && connectionString.Contains("$DATAPATH"))
                    connectionString = connectionString.Replace("$DATAPATH", _dataPath);
                options.UseSqlite(connectionString);
            }
        }
    }
}