using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddIdShortPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BuiltIdShortPath",
                table: "SMESets",
                newName: "IdShortPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IdShortPath",
                table: "SMESets",
                newName: "BuiltIdShortPath");
        }
    }
}
