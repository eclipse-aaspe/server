using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class addIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SubmodelSets_SubmodelNum",
                table: "SubmodelSets",
                column: "SubmodelNum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMENum",
                table: "SMESets",
                column: "SMENum");

            migrationBuilder.CreateIndex(
                name: "IX_AASXSets_AASXNum",
                table: "AASXSets",
                column: "AASXNum");

            migrationBuilder.CreateIndex(
                name: "IX_AasSets_AasNum",
                table: "AasSets",
                column: "AasNum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SubmodelSets_SubmodelNum",
                table: "SubmodelSets");

            migrationBuilder.DropIndex(
                name: "IX_SMESets_SMENum",
                table: "SMESets");

            migrationBuilder.DropIndex(
                name: "IX_AASXSets_AASXNum",
                table: "AASXSets");

            migrationBuilder.DropIndex(
                name: "IX_AasSets_AasNum",
                table: "AasSets");
        }
    }
}
