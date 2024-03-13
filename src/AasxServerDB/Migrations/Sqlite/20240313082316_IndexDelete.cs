using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class IndexDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SValueSets_ParentSMEId",
                table: "SValueSets");

            migrationBuilder.DropIndex(
                name: "IX_SMSets_Id",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMESets_Id",
                table: "SMESets");

            migrationBuilder.DropIndex(
                name: "IX_IValueSets_ParentSMEId",
                table: "IValueSets");

            migrationBuilder.DropIndex(
                name: "IX_DValueSets_ParentSMEId",
                table: "DValueSets");

            migrationBuilder.DropIndex(
                name: "IX_AASXSets_Id",
                table: "AASXSets");

            migrationBuilder.DropIndex(
                name: "IX_AASSets_Id",
                table: "AASSets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SValueSets_ParentSMEId",
                table: "SValueSets",
                column: "ParentSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_Id",
                table: "SMSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_Id",
                table: "SMESets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_IValueSets_ParentSMEId",
                table: "IValueSets",
                column: "ParentSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_DValueSets_ParentSMEId",
                table: "DValueSets",
                column: "ParentSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_AASXSets_Id",
                table: "AASXSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AASSets_Id",
                table: "AASSets",
                column: "Id");
        }
    }
}
