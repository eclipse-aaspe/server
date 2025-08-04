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
using SMDataGenerator.Models;

namespace SMDataGenerator.Data;

public class AppDbContext : DbContext
{
    public DbSet<SM> SMs => Set<SM>();
    public DbSet<SME> SMEs => Set<SME>();
    public DbSet<Value> Values => Set<Value>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=smdata.db");
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
