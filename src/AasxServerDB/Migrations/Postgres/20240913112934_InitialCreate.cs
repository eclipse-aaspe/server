﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AASXSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASX = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASXSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AASSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXId = table.Column<int>(type: "integer", nullable: false),
                    Identifier = table.Column<string>(type: "text", nullable: true),
                    IdShort = table.Column<string>(type: "text", nullable: true),
                    AssetKind = table.Column<string>(type: "text", nullable: true),
                    GlobalAssetId = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AASSets_AASXSets_AASXId",
                        column: x => x.AASXId,
                        principalTable: "AASXSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SMSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXId = table.Column<int>(type: "integer", nullable: false),
                    AASId = table.Column<int>(type: "integer", nullable: true),
                    SemanticId = table.Column<string>(type: "text", nullable: true),
                    Identifier = table.Column<string>(type: "text", nullable: true),
                    IdShort = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMSets_AASSets_AASId",
                        column: x => x.AASId,
                        principalTable: "AASSets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SMSets_AASXSets_AASXId",
                        column: x => x.AASXId,
                        principalTable: "AASXSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SMESets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMId = table.Column<int>(type: "integer", nullable: false),
                    ParentSMEId = table.Column<int>(type: "integer", nullable: true),
                    SMEType = table.Column<string>(type: "text", nullable: true),
                    TValue = table.Column<string>(type: "text", nullable: true),
                    SemanticId = table.Column<string>(type: "text", nullable: true),
                    IdShort = table.Column<string>(type: "text", nullable: true),
                    Extensions = table.Column<string>(type: "text", nullable: true),
                    Qualifiers = table.Column<string>(type: "text", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeStampDelete = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMESets_SMESets_ParentSMEId",
                        column: x => x.ParentSMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SMESets_SMSets_SMId",
                        column: x => x.SMId,
                        principalTable: "SMSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_AASXId",
                table: "AASSets",
                column: "AASXId");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_SMEId",
                table: "DValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_Value",
                table: "DValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_SMEId",
                table: "IValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_Value",
                table: "IValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_OValueSets_SMEId",
                table: "OValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_ParentSMEId",
                table: "SMESets",
                column: "ParentSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMId",
                table: "SMESets",
                column: "SMId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASId",
                table: "SMSets",
                column: "AASId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_AASXId",
                table: "SMSets",
                column: "AASXId");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_SMEId",
                table: "SValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_Value",
                table: "SValueSets",
                column: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

            migrationBuilder.DropTable(
                name: "OValueSets");

            migrationBuilder.DropTable(
                name: "SValueSets");

            migrationBuilder.DropTable(
                name: "SMESets");

            migrationBuilder.DropTable(
                name: "SMSets");

            migrationBuilder.DropTable(
                name: "AASSets");

            migrationBuilder.DropTable(
                name: "AASXSets");
        }
    }
}
