using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ChangeIdNameAASXNum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AASXNum",
                table: "SMSets",
                newName: "AASXId");

            migrationBuilder.RenameColumn(
                name: "AASXNum",
                table: "AASSets",
                newName: "AASXId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AASXId",
                table: "SMSets",
                newName: "AASXNum");

            migrationBuilder.RenameColumn(
                name: "AASXId",
                table: "AASSets",
                newName: "AASXNum");
        }
    }
}
