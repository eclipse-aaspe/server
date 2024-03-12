using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ChangeIdNameAASX : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AASXSets_AASXNum",
                table: "AASXSets");

            migrationBuilder.DropColumn(
                name: "AASXNum",
                table: "AASXSets");

            migrationBuilder.CreateIndex(
                name: "IX_AASXSets_Id",
                table: "AASXSets",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AASXSets_Id",
                table: "AASXSets");

            migrationBuilder.AddColumn<long>(
                name: "AASXNum",
                table: "AASXSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AASXSets_AASXNum",
                table: "AASXSets",
                column: "AASXNum");
        }
    }
}
