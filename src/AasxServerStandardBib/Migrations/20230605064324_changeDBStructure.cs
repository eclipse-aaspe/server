using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class changeDBStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmodelSets_AasSets_AasSetId",
                table: "SubmodelSets");

            migrationBuilder.DropIndex(
                name: "IX_SubmodelSets_AasSetId",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "AasId",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "AasSetId",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "Aasx",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "AllIdshort",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "AllSemanticId",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "Aasx",
                table: "AasSets");

            migrationBuilder.AddColumn<long>(
                name: "AASXNum",
                table: "SubmodelSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AasNum",
                table: "SubmodelSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "SubmodelNum",
                table: "SubmodelSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AASXNum",
                table: "AasSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "AasNum",
                table: "AasSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "AASXSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASXNum = table.Column<long>(type: "INTEGER", nullable: false),
                    AASX = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASXSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbConfigSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AasCount = table.Column<long>(type: "INTEGER", nullable: false),
                    SubmodelCount = table.Column<long>(type: "INTEGER", nullable: false),
                    AASXCount = table.Column<long>(type: "INTEGER", nullable: false),
                    SMECount = table.Column<long>(type: "INTEGER", nullable: false),
                    IntValueCount = table.Column<long>(type: "INTEGER", nullable: false),
                    FloatValueCount = table.Column<long>(type: "INTEGER", nullable: false),
                    StringValueCount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbConfigSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FloatValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntValueNum = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false)
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
                    Value = table.Column<long>(type: "INTEGER", nullable: false),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntValueSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMESets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false),
                    SMEType = table.Column<string>(type: "TEXT", nullable: true),
                    SemanticId = table.Column<string>(type: "TEXT", nullable: true),
                    Idshort = table.Column<string>(type: "TEXT", nullable: true),
                    SubmodelNum = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentSMENum = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StringValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IntValueNum = table.Column<long>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StringValueSets", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AASXSets");

            migrationBuilder.DropTable(
                name: "DbConfigSets");

            migrationBuilder.DropTable(
                name: "FloatValueSets");

            migrationBuilder.DropTable(
                name: "IntValueSets");

            migrationBuilder.DropTable(
                name: "SMESets");

            migrationBuilder.DropTable(
                name: "StringValueSets");

            migrationBuilder.DropColumn(
                name: "AASXNum",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "AasNum",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "SubmodelNum",
                table: "SubmodelSets");

            migrationBuilder.DropColumn(
                name: "AASXNum",
                table: "AasSets");

            migrationBuilder.DropColumn(
                name: "AasNum",
                table: "AasSets");

            migrationBuilder.AddColumn<string>(
                name: "AasId",
                table: "SubmodelSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AasSetId",
                table: "SubmodelSets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Aasx",
                table: "SubmodelSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllIdshort",
                table: "SubmodelSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AllSemanticId",
                table: "SubmodelSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Aasx",
                table: "AasSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmodelSets_AasSetId",
                table: "SubmodelSets",
                column: "AasSetId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmodelSets_AasSets_AasSetId",
                table: "SubmodelSets",
                column: "AasSetId",
                principalTable: "AasSets",
                principalColumn: "Id");
        }
    }
}
