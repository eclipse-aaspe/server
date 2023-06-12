using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class resortTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FloatValueCount",
                table: "DbConfigSets");

            migrationBuilder.DropColumn(
                name: "IntValueCount",
                table: "DbConfigSets");

            migrationBuilder.DropColumn(
                name: "StringValueCount",
                table: "DbConfigSets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FloatValueCount",
                table: "DbConfigSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "IntValueCount",
                table: "DbConfigSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "StringValueCount",
                table: "DbConfigSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
