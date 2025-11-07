using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SingleValueSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

            migrationBuilder.DropTable(
                name: "SValueSets");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSets_AASId",
                table: "SMRefSets",
                newName: "IX_SMRefSet_AASId");

            migrationBuilder.CreateTable(
                name: "ValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    SMId = table.Column<int>(type: "integer", nullable: false),
                    Attribute = table.Column<string>(type: "text", nullable: false),
                    SValue = table.Column<string>(type: "text", nullable: false),
                    DValue = table.Column<double>(type: "double precision", nullable: false),
                    TValue = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DTValue = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValueSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ValueSets_SMESets_SMEId",
                        column: x => x.SMEId,
                        principalTable: "SMESets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ValueSets_SMSets_SMId",
                        column: x => x.SMId,
                        principalTable: "SMSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SMSet_IdShort",
                table: "SMSets",
                column: "IdShort");

            migrationBuilder.CreateIndex(
                name: "IX_AASSet_GlobalAssetId",
                table: "AASSets",
                column: "GlobalAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_Id",
                table: "ValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_SMEId",
                table: "ValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_SMId",
                table: "ValueSets",
                column: "SMId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValueSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSet_IdShort",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSet_GlobalAssetId",
                table: "AASSets");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSet_AASId",
                table: "SMRefSets",
                newName: "IX_SMRefSets_AASId");

            migrationBuilder.CreateTable(
                name: "DValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<double>(type: "double precision", nullable: true)
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
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<long>(type: "bigint", nullable: true)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMEId = table.Column<int>(type: "integer", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true),
                    Value = table.Column<string>(type: "text", nullable: true)
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
                name: "IX_DValueSets_SMEId",
                table: "DValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_Value",
                table: "DValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_Id",
                table: "IValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_SMEId",
                table: "IValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_Value",
                table: "IValueSets",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_Id",
                table: "SValueSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_SMEId",
                table: "SValueSets",
                column: "SMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_Value",
                table: "SValueSets",
                column: "Value");
        }
    }
}
