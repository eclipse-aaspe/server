using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RenameSMRefSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMRefSet_AASSets_AASId",
                table: "SMRefSet");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SMRefSet",
                table: "SMRefSet");

            migrationBuilder.RenameTable(
                name: "SMRefSet",
                newName: "SMRefSets");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSet_Identifier",
                table: "SMRefSets",
                newName: "IX_SMRefSets_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSet_Id",
                table: "SMRefSets",
                newName: "IX_SMRefSets_Id");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSet_AASId",
                table: "SMRefSets",
                newName: "IX_SMRefSets_AASId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SMRefSets",
                table: "SMRefSets",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SMRefSets_AASSets_AASId",
                table: "SMRefSets",
                column: "AASId",
                principalTable: "AASSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMRefSets_AASSets_AASId",
                table: "SMRefSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SMRefSets",
                table: "SMRefSets");

            migrationBuilder.RenameTable(
                name: "SMRefSets",
                newName: "SMRefSet");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSets_Identifier",
                table: "SMRefSet",
                newName: "IX_SMRefSet_Identifier");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSets_Id",
                table: "SMRefSet",
                newName: "IX_SMRefSet_Id");

            migrationBuilder.RenameIndex(
                name: "IX_SMRefSets_AASId",
                table: "SMRefSet",
                newName: "IX_SMRefSet_AASId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SMRefSet",
                table: "SMRefSet",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SMRefSet_AASSets_AASId",
                table: "SMRefSet",
                column: "AASId",
                principalTable: "AASSets",
                principalColumn: "Id");
        }
    }
}
