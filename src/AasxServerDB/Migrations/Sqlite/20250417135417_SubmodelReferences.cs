using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class SubmodelReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SMRefSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AASId = table.Column<int>(type: "INTEGER", nullable: true),
                    Identifier = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMRefSet", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMRefSet_AASSets_AASId",
                        column: x => x.AASId,
                        principalTable: "AASSets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SMRefSet_AASId",
                table: "SMRefSet",
                column: "AASId");

            migrationBuilder.CreateIndex(
                name: "IX_SMRefSet_Id",
                table: "SMRefSet",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SMRefSet_Identifier",
                table: "SMRefSet",
                column: "Identifier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SMRefSet");
        }
    }
}
