using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ChangeIdNameAASNum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AASSets_AASNum",
                table: "AASSets");

            migrationBuilder.DropColumn(
                name: "AASNum",
                table: "AASSets");

            migrationBuilder.RenameColumn(
                name: "AASNum",
                table: "SMSets",
                newName: "AASId");

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_Id",
                table: "AASSets",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AASSets_Id",
                table: "AASSets");

            migrationBuilder.RenameColumn(
                name: "AASId",
                table: "SMSets",
                newName: "AASNum");

            migrationBuilder.AddColumn<long>(
                name: "AASNum",
                table: "AASSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_AASNum",
                table: "AASSets",
                column: "AASNum");
        }
    }
}
