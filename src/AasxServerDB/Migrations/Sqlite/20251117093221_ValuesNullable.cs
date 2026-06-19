using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ValuesNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "TValue",
                table: "ValueSets",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<double>(
                name: "DValue",
                table: "ValueSets",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DTValue",
                table: "ValueSets",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<TimeOnly>(
                name: "TValue",
                table: "ValueSets",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "DValue",
                table: "ValueSets",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DTValue",
                table: "ValueSets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
