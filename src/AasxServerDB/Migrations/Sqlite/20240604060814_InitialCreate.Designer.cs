﻿// <auto-generated />
using System;
using AasxServerDB.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    [DbContext(typeof(SqliteAasContext))]
    [Migration("20240604060814_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("AasxServerDB.AASSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AASXId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("AssetKind")
                        .HasColumnType("TEXT");

                    b.Property<string>("GlobalAssetId")
                        .HasColumnType("TEXT");

                    b.Property<string>("IdShort")
                        .HasColumnType("TEXT");

                    b.Property<string>("Identifier")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AASXId");

                    b.ToTable("AASSets");
                });

            modelBuilder.Entity("AasxServerDB.AASXSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("AASX")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("AASXSets");
                });

            modelBuilder.Entity("AasxServerDB.DValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotation")
                        .HasColumnType("TEXT");

                    b.Property<int>("SMEId")
                        .HasColumnType("INTEGER");

                    b.Property<double?>("Value")
                        .HasColumnType("REAL");

                    b.HasKey("Id");

                    b.HasIndex("SMEId");

                    b.ToTable("DValueSets");
                });

            modelBuilder.Entity("AasxServerDB.IValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotation")
                        .HasColumnType("TEXT");

                    b.Property<int>("SMEId")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("Value")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("SMEId");

                    b.ToTable("IValueSets");
                });

            modelBuilder.Entity("AasxServerDB.SMESet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("IdShort")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ParentSMEId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SMEType")
                        .HasColumnType("TEXT");

                    b.Property<int>("SMId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SemanticId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("TEXT");

                    b.Property<string>("ValueType")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ParentSMEId");

                    b.HasIndex("SMId");

                    b.ToTable("SMESets");
                });

            modelBuilder.Entity("AasxServerDB.SMSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("AASId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AASXId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IdShort")
                        .HasColumnType("TEXT");

                    b.Property<string>("Identifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("SemanticId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStamp")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStampCreate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("TimeStampTree")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("AASId");

                    b.HasIndex("AASXId");

                    b.ToTable("SMSets");
                });

            modelBuilder.Entity("AasxServerDB.SValueSet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Annotation")
                        .HasColumnType("TEXT");

                    b.Property<int>("SMEId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

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
