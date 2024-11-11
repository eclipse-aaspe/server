using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddEnvCDSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CDSets_EnvSets_EnvId",
                table: "CDSets");

            migrationBuilder.DropIndex(
                name: "IX_CDSets_EnvId",
                table: "CDSets");

            migrationBuilder.DropColumn(
                name: "EnvId",
                table: "CDSets");

            migrationBuilder.CreateTable(
                name: "EnvCDSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnvId = table.Column<int>(type: "integer", nullable: false),
                    CDId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvCDSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvCDSets_CDSets_CDId",
                        column: x => x.CDId,
                        principalTable: "CDSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnvCDSets_EnvSets_EnvId",
                        column: x => x.EnvId,
                        principalTable: "EnvSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvCDSets_CDId",
                table: "EnvCDSets",
                column: "CDId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvCDSets_EnvId",
                table: "EnvCDSets",
                column: "EnvId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvCDSets");

            migrationBuilder.AddColumn<int>(
                name: "EnvId",
                table: "CDSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CDSets_EnvId",
                table: "CDSets",
                column: "EnvId");

            migrationBuilder.AddForeignKey(
                name: "FK_CDSets_EnvSets_EnvId",
                table: "CDSets",
                column: "EnvId",
                principalTable: "EnvSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
