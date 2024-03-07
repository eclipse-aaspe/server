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
                name: "AasSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXNum = table.Column<long>(type: "bigint", nullable: false),
                    AasNum = table.Column<long>(type: "bigint", nullable: false),
                    AasId = table.Column<string>(type: "text", nullable: true),
                    Idshort = table.Column<string>(type: "text", nullable: true),
                    AssetId = table.Column<string>(type: "text", nullable: true),
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
                name: "DbConfigSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AasCount = table.Column<long>(type: "bigint", nullable: false),
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
                    SubmodelNum = table.Column<long>(type: "bigint", nullable: false),
                    ParentSMENum = table.Column<long>(type: "bigint", nullable: false),
                    SMENum = table.Column<long>(type: "bigint", nullable: false),
                    SMEType = table.Column<string>(type: "text", nullable: true),
                    Idshort = table.Column<string>(type: "text", nullable: true),
                    SemanticId = table.Column<string>(type: "text", nullable: true),
                    ValueType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMESets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubmodelSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AASXNum = table.Column<long>(type: "bigint", nullable: false),
                    AasNum = table.Column<long>(type: "bigint", nullable: false),
                    SubmodelNum = table.Column<long>(type: "bigint", nullable: false),
                    SubmodelId = table.Column<string>(type: "text", nullable: true),
                    Idshort = table.Column<string>(type: "text", nullable: true),
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
                table: "AasSets",
                column: "AasNum");

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
                column: "SubmodelNum");

            migrationBuilder.CreateIndex(
                name: "IX_SubmodelSets_SubmodelNum",
                table: "SubmodelSets",
                column: "SubmodelNum");

            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_ParentSMENum",
                table: "SValueSets",
                column: "ParentSMENum");
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
                name: "DValueSets");

            migrationBuilder.DropTable(
                name: "IValueSets");

            migrationBuilder.DropTable(
                name: "SMESets");

            migrationBuilder.DropTable(
                name: "SubmodelSets");

            migrationBuilder.DropTable(
                name: "SValueSets");
        }
    }
}
