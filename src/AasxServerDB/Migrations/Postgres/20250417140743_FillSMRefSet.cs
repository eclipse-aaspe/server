using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AasxServerDB.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class FillSMRefSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Copy data from SMSet to SMRefSet
            migrationBuilder.Sql("INSERT INTO SMRefSet (AASId, Identifier) " +
                "SELECT AASId, Identifier " +
                "FROM SMSets " +
                "WHERE AASId IS NOT NULL AND Identifier IS NOT NULL;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optionally, delete the copied data if you roll back the migration
            migrationBuilder.Sql(@"
                DELETE FROM SMRefSet;
            ");
        }
    }
}
