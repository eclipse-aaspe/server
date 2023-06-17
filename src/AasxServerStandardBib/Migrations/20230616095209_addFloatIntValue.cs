using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class addFloatIntValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "SMESets",
                newName: "SValue");

            migrationBuilder.AddColumn<double>(
                name: "FValue",
                table: "SMESets",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<long>(
                name: "IValue",
                table: "SMESets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FValue",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "IValue",
                table: "SMESets");

            migrationBuilder.RenameColumn(
                name: "SValue",
                table: "SMESets",
                newName: "Value");
        }
    }
}
