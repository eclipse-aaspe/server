using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class aasdescriptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AasDescriptor",
                table: "AasSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssetKind",
                table: "AasSets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AasDescriptor",
                table: "AasSets");

            migrationBuilder.DropColumn(
                name: "AssetKind",
                table: "AasSets");
        }
    }
}
