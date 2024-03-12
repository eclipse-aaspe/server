using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ChangeIdIdentififer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SMId",
                table: "SMSets",
                newName: "IdIdentifier");

            migrationBuilder.RenameColumn(
                name: "AASId",
                table: "AASSets",
                newName: "IdIdentifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdIdentifier",
                table: "SMSets",
                newName: "SMId");

            migrationBuilder.RenameColumn(
                name: "IdIdentifier",
                table: "AASSets",
                newName: "AASId");
        }
    }
}
