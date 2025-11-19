using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RenameValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TValue",
                table: "ValueSets");

            migrationBuilder.RenameColumn(
                name: "DValue",
                table: "ValueSets",
                newName: "NValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NValue",
                table: "ValueSets",
                newName: "DValue");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "TValue",
                table: "ValueSets",
                type: "TEXT",
                nullable: true);
        }
    }
}
