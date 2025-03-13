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
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("AasxServerDB.Entities.AASSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AEmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<string>("AssetKind")
                        .HasColumnType("text");

                    b.Property<string>("AssetType")
                        .HasColumnType("text");

                    b.Property<string>("Category")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Creator")
                        .HasColumnType("text");

                    b.Property<string>("DefaultThumbnailContentType")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("DefaultThumbnailPath")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("DerivedFrom")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("EmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<int>("EnvId")
                        .HasColumnType("integer");

                    b.Property<string>("Extensions")
                        .HasColumnType("text");

                    b.Property<string>("GlobalAssetId")
                        .HasColumnType("text");

                    b.Property<string>("IdShort")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Identifier")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("Revision")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.Property<string>("SpecificAssetIds")
                        .HasColumnType("text");

                    b.Property<string>("TemplateId")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampDelete")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Version")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.HasKey("Id");

                    b.HasIndex("EnvId");

                    b.HasIndex("Id");

                    b.ToTable("AASSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.CDSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("AEmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<string>("Category")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Creator")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("EmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<string>("Extensions")
                        .HasColumnType("text");

                    b.Property<string>("IdShort")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Identifier")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("IsCaseOf")
                        .HasColumnType("text");

                    b.Property<string>("Revision")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.Property<string>("TemplateId")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampDelete")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Version")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("CDSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.DValueSet", b =>
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

                    b.HasIndex("Value");

                    b.ToTable("DValueSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.EnvCDSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int>("CDId")
                        .HasColumnType("integer");

                    b.Property<int>("EnvId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CDId");

                    b.HasIndex("EnvId");

                    b.ToTable("EnvCDSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.EnvSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Path")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.ToTable("EnvSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.IValueSet", b =>
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

                    b.HasIndex("Id");

                    b.HasIndex("SMEId");

                    b.HasIndex("Value");

                    b.ToTable("IValueSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.OValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Attribute")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("SMEId")
                        .HasColumnType("integer");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("SMEId");

                    b.HasIndex("Value");

                    b.ToTable("OValueSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SMESet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<string>("Category")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("EmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<string>("Extensions")
                        .HasColumnType("text");

                    b.Property<string>("IdShort")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("IdShortPath")
                        .HasColumnType("text");

                    b.Property<int?>("ParentSMEId")
                        .HasColumnType("integer");

                    b.Property<string>("Qualifiers")
                        .HasColumnType("text");

                    b.Property<string>("SMEType")
                        .IsRequired()
                        .HasMaxLength(9)
                        .HasColumnType("character varying(9)");

                    b.Property<int>("SMId")
                        .HasColumnType("integer");

                    b.Property<string>("SemanticId")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("SupplementalSemanticIds")
                        .HasColumnType("text");

                    b.Property<string>("TValue")
                        .HasColumnType("char(1)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampDelete")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("Id");

                    b.HasIndex("IdShort");

                    b.HasIndex("ParentSMEId");

                    b.HasIndex("SMId");

                    b.HasIndex("SemanticId");

                    b.HasIndex("TimeStamp");

                    b.ToTable("SMESets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SMSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<int?>("AASId")
                        .HasColumnType("integer");

                    b.Property<string>("AEmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<string>("Category")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Creator")
                        .HasColumnType("text");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("DisplayName")
                        .HasColumnType("text");

                    b.Property<string>("EmbeddedDataSpecifications")
                        .HasColumnType("text");

                    b.Property<int?>("EnvId")
                        .HasColumnType("integer");

                    b.Property<string>("Extensions")
                        .HasColumnType("text");

                    b.Property<string>("IdShort")
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<string>("Identifier")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("Kind")
                        .HasMaxLength(8)
                        .HasColumnType("character varying(8)");

                    b.Property<string>("Qualifiers")
                        .HasColumnType("text");

                    b.Property<string>("Revision")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.Property<string>("SemanticId")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<string>("SupplementalSemanticIds")
                        .HasColumnType("text");

                    b.Property<string>("TemplateId")
                        .HasMaxLength(2000)
                        .HasColumnType("character varying(2000)");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampDelete")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Version")
                        .HasMaxLength(4)
                        .HasColumnType("character varying(4)");

                    b.HasKey("Id");

                    b.HasIndex("AASId");

                    b.HasIndex("EnvId");

                    b.HasIndex("Id");

                    b.HasIndex("Identifier");

                    b.HasIndex("SemanticId");

                    b.HasIndex("TimeStampTree");

                    b.ToTable("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SValueSet", b =>
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

                    b.HasIndex("Id");

                    b.HasIndex("SMEId");

                    b.HasIndex("Value");

                    b.ToTable("SValueSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.AASSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.EnvSet", "EnvSet")
                        .WithMany("AASSets")
                        .HasForeignKey("EnvId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("EnvSet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.DValueSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.SMESet", "SMESet")
                        .WithMany("DValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.EnvCDSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.CDSet", "CDSet")
                        .WithMany("EnvCDSets")
                        .HasForeignKey("CDId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("AasxServerDB.Entities.EnvSet", "EnvSet")
                        .WithMany("EnvCDSets")
                        .HasForeignKey("EnvId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CDSet");

                    b.Navigation("EnvSet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.IValueSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.SMESet", "SMESet")
                        .WithMany("IValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.OValueSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.SMESet", "SMESet")
                        .WithMany("OValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SMESet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.SMESet", "ParentSME")
                        .WithMany()
                        .HasForeignKey("ParentSMEId");

                    b.HasOne("AasxServerDB.Entities.SMSet", "SMSet")
                        .WithMany("SMESets")
                        .HasForeignKey("SMId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentSME");

                    b.Navigation("SMSet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SMSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.AASSet", "AASSet")
                        .WithMany("SMSets")
                        .HasForeignKey("AASId");

                    b.HasOne("AasxServerDB.Entities.EnvSet", "EnvSet")
                        .WithMany("SMSets")
                        .HasForeignKey("EnvId");

                    b.Navigation("AASSet");

                    b.Navigation("EnvSet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SValueSet", b =>
                {
                    b.HasOne("AasxServerDB.Entities.SMESet", "SMESet")
                        .WithMany("SValueSets")
                        .HasForeignKey("SMEId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SMESet");
                });

            modelBuilder.Entity("AasxServerDB.Entities.AASSet", b =>
                {
                    b.Navigation("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.CDSet", b =>
                {
                    b.Navigation("EnvCDSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.EnvSet", b =>
                {
                    b.Navigation("AASSets");

                    b.Navigation("EnvCDSets");

                    b.Navigation("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SMESet", b =>
                {
                    b.Navigation("DValueSets");

                    b.Navigation("IValueSets");

                    b.Navigation("OValueSets");

                    b.Navigation("SValueSets");
                });

            modelBuilder.Entity("AasxServerDB.Entities.SMSet", b =>
                {
                    b.Navigation("SMESets");
                });
#pragma warning restore 612, 618
        }
    }
}
