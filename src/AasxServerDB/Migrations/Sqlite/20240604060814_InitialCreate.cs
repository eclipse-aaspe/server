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

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASX = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASXSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AASSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASXId = table.Column<int>(type: "INTEGER", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", nullable: true),
                    IdShort = table.Column<string>(type: "TEXT", nullable: true),
                    AssetKind = table.Column<string>(type: "TEXT", nullable: true),
                    GlobalAssetId = table.Column<string>(type: "TEXT", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASXId = table.Column<int>(type: "INTEGER", nullable: false),
                    AASId = table.Column<int>(type: "INTEGER", nullable: true),
                    SemanticId = table.Column<string>(type: "TEXT", nullable: true),
                    Identifier = table.Column<string>(type: "TEXT", nullable: true),
                    IdShort = table.Column<string>(type: "TEXT", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SMId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentSMEId = table.Column<int>(type: "INTEGER", nullable: true),
                    SMEType = table.Column<string>(type: "TEXT", nullable: true),
                    ValueType = table.Column<string>(type: "TEXT", nullable: true),
                    SemanticId = table.Column<string>(type: "TEXT", nullable: true),
                    IdShort = table.Column<string>(type: "TEXT", nullable: true),
                    TimeStampCreate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeStampTree = table.Column<DateTime>(type: "TEXT", nullable: false)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SMEId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: true),
                    Annotation = table.Column<string>(type: "TEXT", nullable: true)
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SMEId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: true),
                    Annotation = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "SValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SMEId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    Annotation = table.Column<string>(type: "TEXT", nullable: true)
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
                name: "IX_IValueSets_SMEId",
                table: "IValueSets",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

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
