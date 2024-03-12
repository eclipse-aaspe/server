using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class ChangeNum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SMSets_SMNum",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMESets_SMENum",
                table: "SMESets");

            migrationBuilder.DropIndex(
                name: "IX_SMESets_SMNum",
                table: "SMESets");

            migrationBuilder.DropColumn(
                name: "SMNum",
                table: "SMSets");

            migrationBuilder.DropColumn(
                name: "ParentSMENum",
                table: "SMESets");

            migrationBuilder.RenameColumn(
                name: "ParentSMENum",
                table: "SValueSets",
                newName: "ParentSMEId");

            migrationBuilder.RenameIndex(
                name: "IX_SValueSets_ParentSMENum",
                table: "SValueSets",
                newName: "IX_SValueSets_ParentSMEId");

            migrationBuilder.RenameColumn(
                name: "SMNum",
                table: "SMESets",
                newName: "SMId");

            migrationBuilder.RenameColumn(
                name: "SMENum",
                table: "SMESets",
                newName: "ParentSMEId");

            migrationBuilder.RenameColumn(
                name: "ParentSMENum",
                table: "IValueSets",
                newName: "ParentSMEId");

            migrationBuilder.RenameIndex(
                name: "IX_IValueSets_ParentSMENum",
                table: "IValueSets",
                newName: "IX_IValueSets_ParentSMEId");

            migrationBuilder.RenameColumn(
                name: "ParentSMENum",
                table: "DValueSets",
                newName: "ParentSMEId");

            migrationBuilder.RenameIndex(
                name: "IX_DValueSets_ParentSMENum",
                table: "DValueSets",
                newName: "IX_DValueSets_ParentSMEId");

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_Id",
                table: "SMSets",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_Id",
                table: "SMESets",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SMSets_Id",
                table: "SMSets");

            migrationBuilder.DropIndex(
                name: "IX_SMESets_Id",
                table: "SMESets");

            migrationBuilder.RenameColumn(
                name: "ParentSMEId",
                table: "SValueSets",
                newName: "ParentSMENum");

            migrationBuilder.RenameIndex(
                name: "IX_SValueSets_ParentSMEId",
                table: "SValueSets",
                newName: "IX_SValueSets_ParentSMENum");

            migrationBuilder.RenameColumn(
                name: "SMId",
                table: "SMESets",
                newName: "SMNum");

            migrationBuilder.RenameColumn(
                name: "ParentSMEId",
                table: "SMESets",
                newName: "SMENum");

            migrationBuilder.RenameColumn(
                name: "ParentSMEId",
                table: "IValueSets",
                newName: "ParentSMENum");

            migrationBuilder.RenameIndex(
                name: "IX_IValueSets_ParentSMEId",
                table: "IValueSets",
                newName: "IX_IValueSets_ParentSMENum");

            migrationBuilder.RenameColumn(
                name: "ParentSMEId",
                table: "DValueSets",
                newName: "ParentSMENum");

            migrationBuilder.RenameIndex(
                name: "IX_DValueSets_ParentSMEId",
                table: "DValueSets",
                newName: "IX_DValueSets_ParentSMENum");

            migrationBuilder.AddColumn<long>(
                name: "SMNum",
                table: "SMSets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ParentSMENum",
                table: "SMESets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_SMSets_SMNum",
                table: "SMSets",
                column: "SMNum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMENum",
                table: "SMESets",
                column: "SMENum");

            migrationBuilder.CreateIndex(
                name: "IX_SMESets_SMNum",
                table: "SMESets",
                column: "SMNum");
        }
    }
}
