using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class EnvId_nullable_in_aas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_EnvSets_EnvId",
                table: "AASSets");

            migrationBuilder.AlterColumn<int>(
                name: "EnvId",
                table: "AASSets",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_EnvSets_EnvId",
                table: "AASSets",
                column: "EnvId",
                principalTable: "EnvSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AASSets_EnvSets_EnvId",
                table: "AASSets");

            migrationBuilder.AlterColumn<int>(
                name: "EnvId",
                table: "AASSets",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AASSets_EnvSets_EnvId",
                table: "AASSets",
                column: "EnvId",
                principalTable: "EnvSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
