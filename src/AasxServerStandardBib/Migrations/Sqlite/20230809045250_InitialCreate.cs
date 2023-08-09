using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AasSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASXNum = table.Column<long>(type: "INTEGER", nullable: false),
                    AasNum = table.Column<long>(type: "INTEGER", nullable: false),
                    AasId = table.Column<string>(type: "TEXT", nullable: true),
                    Idshort = table.Column<string>(type: "TEXT", nullable: true),
                    AssetId = table.Column<string>(type: "TEXT", nullable: true),
                    AssetKind = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AasSets", x => x.Id);
                });

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
                    SMECount = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbConfigSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMESets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SubmodelNum = table.Column<long>(type: "INTEGER", nullable: false),
                    ParentSMENum = table.Column<long>(type: "INTEGER", nullable: false),
                    SMENum = table.Column<long>(type: "INTEGER", nullable: false),
                    SMEType = table.Column<string>(type: "TEXT", nullable: true),
                    Idshort = table.Column<string>(type: "TEXT", nullable: true),
                    SemanticId = table.Column<string>(type: "TEXT", nullable: true),
                    ValueType = table.Column<string>(type: "TEXT", nullable: true),
                    SValue = table.Column<string>(type: "TEXT", nullable: true),
                    IValue = table.Column<long>(type: "INTEGER", nullable: false),
                    FValue = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubmodelSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASXNum = table.Column<long>(type: "INTEGER", nullable: false),
                    AasNum = table.Column<long>(type: "INTEGER", nullable: false),
                    SubmodelNum = table.Column<long>(type: "INTEGER", nullable: false),
                    SubmodelId = table.Column<string>(type: "TEXT", nullable: true),
                    Idshort = table.Column<string>(type: "TEXT", nullable: true),
                    SemanticId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmodelSets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AasSets_AasNum",
                table: "AasSets",
                column: "AasNum");

            migrationBuilder.CreateIndex(
                name: "IX_AASXSets_AASXNum",
                table: "AASXSets",
                column: "AASXNum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMENum",
                table: "SMESets",
                column: "SMENum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SubmodelNum",
                table: "SMESets",
                column: "SubmodelNum");

            migrationBuilder.CreateIndex(
                name: "IX_SubmodelSets_SubmodelNum",
                table: "SubmodelSets",
                column: "SubmodelNum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AasSets");

            migrationBuilder.DropTable(
                name: "AASXSets");

            migrationBuilder.DropTable(
                name: "DbConfigSets");

            migrationBuilder.DropTable(
                name: "SMESets");

            migrationBuilder.DropTable(
                name: "SubmodelSets");
        }
    }
}
