﻿// <auto-generated />
using System;
using AasxServerDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    [DbContext(typeof(PostgreAasContext))]
    partial class PostgreAasContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.5")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AasxServerDB.AASSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("AASXId")
                        .HasColumnType("integer");

                    b.Property<string>("AssetKind")
                        .HasColumnType("text");

                    b.Property<string>("GlobalAssetId")
                        .HasColumnType("text");

                    b.Property<string>("IdShort")
                        .HasColumnType("text");

                    b.Property<string>("Identifier")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AASXId");

                    b.ToTable("AASSets");
                });

            modelBuilder.Entity("AasxServerDB.AASXSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AASX")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("AASXSets");
                });

            modelBuilder.Entity("AasxServerDB.DValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Annotation")
                        .HasColumnType("text");

                    b.Property<int>("SMEId")
                        .HasColumnType("integer");

                    b.Property<double?>("Value")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("SMEId");

                    b.ToTable("DValueSets");
                });

            modelBuilder.Entity("AasxServerDB.IValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Annotation")
                        .HasColumnType("text");

                    b.Property<int>("SMEId")
                        .HasColumnType("integer");

                    b.Property<long?>("Value")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("SMEId");

                    b.ToTable("IValueSets");
                });

            modelBuilder.Entity("AasxServerDB.SMESet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("IdShort")
                        .HasColumnType("text");

                    b.Property<int?>("ParentSMEId")
                        .HasColumnType("integer");

                    b.Property<string>("SMEType")
                        .HasColumnType("text");

                    b.Property<int>("SMId")
                        .HasColumnType("integer");

                    b.Property<string>("SemanticId")
                        .HasColumnType("text");

                    b.Property<string>("ValueType")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMEId");

                    b.HasIndex("SMId");

                    b.ToTable("SMESets");
                });

            modelBuilder.Entity("AasxServerDB.SMSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("AASId")
                        .HasColumnType("integer");

                    b.Property<int>("AASXId")
                        .HasColumnType("integer");

                    b.Property<string>("IdShort")
                        .HasColumnType("text");

                    b.Property<string>("Identifier")
                        .HasColumnType("text");

                    b.Property<string>("SemanticId")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("AASId");

                    b.HasIndex("AASXId");

                    b.ToTable("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.SValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Annotation")
                        .HasColumnType("text");

                    b.Property<int>("SMEId")
                        .HasColumnType("integer");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("SMEId");

                    b.ToTable("SValueSets");
                });

            modelBuilder.Entity("AasxServerDB.AASSet", b =>
                {
                    b.HasOne("AasxServerDB.AASXSet", "AASXSet")
                        .WithMany("AASSets")
                        .HasForeignKey("AASXId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AASXSet");
                });

            modelBuilder.Entity("AasxServerDB.DValueSet", b =>
                {
                    b.HasOne("AasxServerDB.SMESet", "SMESet")
                        .WithMany("DValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.IValueSet", b =>
                {
                    b.HasOne("AasxServerDB.SMESet", "SMESet")
                        .WithMany("IValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.SMESet", b =>
                {
                    b.HasOne("AasxServerDB.SMESet", "ParentSME")
                        .WithMany()
                        .HasForeignKey("ParentSMEId");

                    b.HasOne("AasxServerDB.SMSet", "SMSet")
                        .WithMany("SMESets")
                        .HasForeignKey("SMId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentSME");

                    b.Navigation("SMSet");
                });

            modelBuilder.Entity("AasxServerDB.SMSet", b =>
                {
                    b.HasOne("AasxServerDB.AASSet", "AASSet")
                        .WithMany("SMSets")
                        .HasForeignKey("AASId");

                    b.HasOne("AasxServerDB.AASXSet", "AASXSet")
                        .WithMany("SMSets")
                        .HasForeignKey("AASXId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AASSet");

                    b.Navigation("AASXSet");
                });

            modelBuilder.Entity("AasxServerDB.SValueSet", b =>
                {
                    b.HasOne("AasxServerDB.SMESet", "SMESet")
                        .WithMany("SValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.AASSet", b =>
                {
                    b.Navigation("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.AASXSet", b =>
                {
                    b.Navigation("AASSets");

                    b.Navigation("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.SMESet", b =>
                {
                    b.Navigation("DValueSets");

                    b.Navigation("IValueSets");

                    b.Navigation("SValueSets");
                });

            modelBuilder.Entity("AasxServerDB.SMSet", b =>
                {
                    b.Navigation("SMESets");
                });
#pragma warning restore 612, 618
        }
    }
}
