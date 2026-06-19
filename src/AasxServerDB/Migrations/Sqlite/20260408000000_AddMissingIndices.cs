using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddMissingIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AASSets_IdShort",
                table: "AASSets",
                column: "IdShort");

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_Identifier",
                table: "AASSets",
                column: "Identifier");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_Identifier_IdShort",
                table: "SMSets",
                columns: new[] { "Identifier", "IdShort" });

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMId_IdShort_IdShortPath",
                table: "SMESets",
                columns: new[] { "SMId", "IdShort", "IdShortPath" });

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_SValue_SMEId",
                table: "ValueSets",
                columns: new[] { "SValue", "SMEId" });

            migrationBuilder.CreateIndex(
                name: "IX_ValueSets_NValue_SMEId",
                table: "ValueSets",
                columns: new[] { "NValue", "SMEId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AASSets_IdShort",
                table: "AASSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSets_Identifier",
                table: "AASSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_Identifier_IdShort",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMESets_SMId_IdShort_IdShortPath",
                table: "SMESets");

            migrationBuilder.DropIndex(
                name: "IX_ValueSets_SValue_SMEId",
                table: "ValueSets");

            migrationBuilder.DropIndex(
                name: "IX_ValueSets_NValue_SMEId",
                table: "ValueSets");
        }
    }
}
