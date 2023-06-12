using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class valuesAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloatValueSets");

            migrationBuilder.DropTable(
                name: "IntValueSets");

            migrationBuilder.DropTable(
                name: "StringValueSets");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "SMESets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValueType",
                table: "SMESets",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "ValueType",
                table: "SMESets");

            migrationBuilder.CreateTable(
                name: "FloatValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntValueNum = table.Column<long>(type: "INTEGER", nullable: false),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloatValueSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntValueNum = table.Column<long>(type: "INTEGER", nullable: false),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntValueSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StringValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntValueNum = table.Column<long>(type: "INTEGER", nullable: false),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StringValueSets", x => x.Id);
                });
        }
    }
}
