using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AasxServerStandardBib.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AASSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXNum = table.Column<long>(type: "bigint", nullable: false),
                    AASNum = table.Column<long>(type: "bigint", nullable: false),
                    AASId = table.Column<string>(type: "text", nullable: true),
                    IdShort = table.Column<string>(type: "text", nullable: true),
                    GlobalAssetId = table.Column<string>(type: "text", nullable: true),
                    AssetKind = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AasSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AASXSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXNum = table.Column<long>(type: "bigint", nullable: false),
                    AASX = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AASXSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DBConfigSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASCount = table.Column<long>(type: "bigint", nullable: false),
                    SubmodelCount = table.Column<long>(type: "bigint", nullable: false),
                    AASXCount = table.Column<long>(type: "bigint", nullable: false),
                    SMECount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbConfigSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentSMENum = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DValueSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentSMENum = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IValueSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMESets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SMNum = table.Column<long>(type: "bigint", nullable: false),
                    ParentSMENum = table.Column<long>(type: "bigint", nullable: false),
                    SMENum = table.Column<long>(type: "bigint", nullable: false),
                    SMEType = table.Column<string>(type: "text", nullable: true),
                    IdShort = table.Column<string>(type: "text", nullable: true),
                    SemanticId = table.Column<string>(type: "text", nullable: true),
                    ValueType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SMSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXNum = table.Column<long>(type: "bigint", nullable: false),
                    AASNum = table.Column<long>(type: "bigint", nullable: false),
                    SMNum = table.Column<long>(type: "bigint", nullable: false),
                    SMId = table.Column<string>(type: "text", nullable: true),
                    IdShort = table.Column<string>(type: "text", nullable: true),
                    SemanticId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmodelSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SValueSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParentSMENum = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Annotation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SValueSets", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AasSets_AasNum",
                table: "AASSets",
                column: "AASNum");

            migrationBuilder.CreateIndex(
                name: "IX_AASXSets_AASXNum",
                table: "AASXSets",
                column: "AASXNum");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_ParentSMENum",
                table: "DValueSets",
                column: "ParentSMENum");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_ParentSMENum",
                table: "IValueSets",
                column: "ParentSMENum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMENum",
                table: "SMESets",
                column: "SMENum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SubmodelNum",
                table: "SMESets",
                column: "SMNum");

            migrationBuilder.CreateIndex(
                name: "IX_SubmodelSets_SubmodelNum",
                table: "SMSets",
                column: "SMNum");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_ParentSMENum",
                table: "SValueSets",
                column: "ParentSMENum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AASSets");

            migrationBuilder.DropTable(
                name: "AASXSets");

            migrationBuilder.DropTable(
                name: "DBConfigSets");

            migrationBuilder.DropTable(
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

            migrationBuilder.DropTable(
                name: "SMESets");

            migrationBuilder.DropTable(
                name: "SMSets");

            migrationBuilder.DropTable(
                name: "SValueSets");
        }
    }
}
