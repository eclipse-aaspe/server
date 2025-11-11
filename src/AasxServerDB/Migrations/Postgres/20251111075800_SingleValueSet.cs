using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class SingleValueSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValueSets_SMSets_SMId",
                table: "ValueSets");

            migrationBuilder.DropColumn(
                name: "Attribute",
                table: "ValueSets");

            migrationBuilder.AlterColumn<int>(
                name: "SMId",
                table: "ValueSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_ValueSets_SMSets_SMId",
                table: "ValueSets",
                column: "SMId",
                principalTable: "SMSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValueSets_SMSets_SMId",
                table: "ValueSets");

            migrationBuilder.AlterColumn<int>(
                name: "SMId",
                table: "ValueSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Attribute",
                table: "ValueSets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_ValueSets_SMSets_SMId",
                table: "ValueSets",
                column: "SMId",
                principalTable: "SMSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
