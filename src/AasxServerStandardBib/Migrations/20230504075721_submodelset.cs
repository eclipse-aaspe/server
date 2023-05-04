using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerStandardBib.Migrations
{
    /// <inheritdoc />
    public partial class submodelset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmodelSet_AasSets_AasSetId",
                table: "SubmodelSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubmodelSet",
                table: "SubmodelSet");

            migrationBuilder.RenameTable(
                name: "SubmodelSet",
                newName: "SubmodelSets");

            migrationBuilder.RenameIndex(
                name: "IX_SubmodelSet_AasSetId",
                table: "SubmodelSets",
                newName: "IX_SubmodelSets_AasSetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubmodelSets",
                table: "SubmodelSets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmodelSets_AasSets_AasSetId",
                table: "SubmodelSets",
                column: "AasSetId",
                principalTable: "AasSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubmodelSets_AasSets_AasSetId",
                table: "SubmodelSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubmodelSets",
                table: "SubmodelSets");

            migrationBuilder.RenameTable(
                name: "SubmodelSets",
                newName: "SubmodelSet");

            migrationBuilder.RenameIndex(
                name: "IX_SubmodelSets_AasSetId",
                table: "SubmodelSet",
                newName: "IX_SubmodelSet_AasSetId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubmodelSet",
                table: "SubmodelSet",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubmodelSet_AasSets_AasSetId",
                table: "SubmodelSet",
                column: "AasSetId",
                principalTable: "AasSets",
                principalColumn: "Id");
        }
    }
}
