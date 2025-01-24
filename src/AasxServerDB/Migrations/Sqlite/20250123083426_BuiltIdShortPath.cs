using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class BuiltIdShortPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DValueSets_Id",
                table: "DValueSets");

            migrationBuilder.AddColumn<string>(
                name: "BuiltIdShortPath",
                table: "SMESets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuiltIdShortPath",
                table: "SMESets");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_Id",
                table: "DValueSets",
                column: "Id");
        }
    }
}
