using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class FixPlayersPerSideIdempotent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent fix: ensure PlayersPerSide column exists on Events table.
            // This handles the case where migration 20260520000001 was recorded in
            // __EFMigrationsHistory but the ALTER TABLE was never actually executed.
            migrationBuilder.Sql(
                "ALTER TABLE \"Events\" ADD COLUMN IF NOT EXISTS \"PlayersPerSide\" integer;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
