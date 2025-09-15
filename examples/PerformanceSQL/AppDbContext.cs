/********************************************************************************
* Copyright (c) 2025 Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the MIT License which is available at
* https://mit-license.org/
*
* SPDX-License-Identifier: MIT
********************************************************************************/

using Microsoft.EntityFrameworkCore;
using Npgsql;
using SMDataGenerator.Models;

namespace SMDataGenerator.Data;

public class AppDbContext : DbContext
{
    public DbSet<SM> SMs => Set<SM>();
    public DbSet<SME> SMEs => Set<SME>();
    public DbSet<Value> Values => Set<Value>();

    static bool init = true;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var withPostgres = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POSTGRES"));

        if (!withPostgres)
        {
            Console.WriteLine("SQLITE");
            optionsBuilder.UseSqlite("Data Source=smdata.db");
        }
        else
        {
            Console.WriteLine("POSTGRES");
            if (init)
            {
                init = false;

                var masterConnectionString = "Host=localhost;Username=postgres;Password=postres;Database=postgres;Port=5432";

                using var connection = new NpgsqlConnection(masterConnectionString);
                connection.Open();

                using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = 'AAS'", connection);
                var exists = command.ExecuteScalar() != null;

                if (!exists)
                {
                    using var createCommand = new NpgsqlCommand("CREATE DATABASE \"AAS\"", connection);
                    createCommand.ExecuteNonQuery();
                    Console.WriteLine("DB AAS created.");
                }
                else
                {
                    Console.WriteLine("DB AAS already exists.");
                }
            }

            var connectionString = "Host=localhost;Database=AAS;Username=postgres;Password=postres;Include Error Detail=true;Port=5432";
            optionsBuilder.UseNpgsql(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SM>()
            .Property(sm => sm.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<SME>()
            .Property(sme => sme.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Value>()
            .Property(v => v.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<SME>()
            .HasOne(sme => sme.ParentSME)
            .WithMany(sme => sme.Children)
            .HasForeignKey(sme => sme.ParentSMEId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Value>()
            .HasOne(v => v.SME)
            .WithMany(sme => sme.Values)
            .HasForeignKey(v => v.SMEId);

        modelBuilder.Entity<Value>()
            .HasOne(v => v.SM)
            .WithMany()
            .HasForeignKey(v => v.SMId);

        // Index for SME: IdShortPath + SMId
        modelBuilder.Entity<SME>()
            .HasIndex(sme => new { sme.IdShortPath, sme.SMId })
            .HasDatabaseName("idx_sme_idshortpath_smId");

        // Index for Value: value + SMId
        modelBuilder.Entity<Value>()
            .HasIndex(v => new { v.value, v.SMId })
            .HasDatabaseName("idx_value_value_smId");
    }
}
