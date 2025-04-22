using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SubmodelReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SMRefSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASId = table.Column<int>(type: "integer", nullable: true),
                    Identifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMRefSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMRefSet_AASSets_AASId",
                        column: x => x.AASId,
                        principalTable: "AASSets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SMRefSet_AASId",
                table: "SMRefSet",
                column: "AASId");

            migrationBuilder.CreateIndex(
                name: "IX_SMRefSet_Id",
                table: "SMRefSet",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SMRefSet_Identifier",
                table: "SMRefSet",
                column: "Identifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SMRefSet");
        }
    }
}
