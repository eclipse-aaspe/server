using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RemoveSmForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValueSets_SMSets_SMId",
                table: "ValueSets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_ValueSets_SMSets_SMId",
                table: "ValueSets",
                column: "SMId",
                principalTable: "SMSets",
                principalColumn: "Id");
        }
    }
}
