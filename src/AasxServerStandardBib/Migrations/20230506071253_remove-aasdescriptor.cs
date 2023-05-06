using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class removeaasdescriptor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AasDescriptor",
                table: "AasSets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AasDescriptor",
                table: "AasSets",
                type: "TEXT",
                nullable: true);
        }
    }
}
