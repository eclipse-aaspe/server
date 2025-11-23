using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddSearchIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SMSetIdResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_DTValue",
                table: "ValueSets",
                column: "DTValue");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_NValue",
                table: "ValueSets",
                column: "NValue");

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_SValue",
                table: "ValueSets",
                column: "SValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SMSetIdResult");

            migrationBuilder.DropIndex(
                name: "IX_ValueSets_DTValue",
                table: "ValueSets");

            migrationBuilder.DropIndex(
                name: "IX_ValueSets_NValue",
                table: "ValueSets");

            migrationBuilder.DropIndex(
                name: "IX_ValueSets_SValue",
                table: "ValueSets");
        }
    }
}
