using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MakeEnvIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_EnvSets_EnvId",
                table: "SMSets");

            migrationBuilder.AlterColumn<int>(
                name: "EnvId",
                table: "SMSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_EnvSets_EnvId",
                table: "SMSets",
                column: "EnvId",
                principalTable: "EnvSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SMSets_EnvSets_EnvId",
                table: "SMSets");

            migrationBuilder.AlterColumn<int>(
                name: "EnvId",
                table: "SMSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SMSets_EnvSets_EnvId",
                table: "SMSets",
                column: "EnvId",
                principalTable: "EnvSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
