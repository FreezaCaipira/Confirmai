using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUniqueGroupStartsAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Keep the oldest event per (GroupId, StartsAt) and remove colliding duplicates.
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT
                        ""Id"",
                        ROW_NUMBER() OVER (
                            PARTITION BY ""GroupId"", ""StartsAt""
                            ORDER BY ""Id""
                        ) AS rn
                    FROM ""Events""
                )
                DELETE FROM ""Events"" e
                USING ranked r
                WHERE e.""Id"" = r.""Id""
                  AND r.rn > 1;
            ");

            migrationBuilder.DropIndex(
                name: "IX_Events_GroupId_StartsAt",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupId_StartsAt",
                table: "Events",
                columns: new[] { "GroupId", "StartsAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_GroupId_StartsAt",
                table: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GroupId_StartsAt",
                table: "Events",
                columns: new[] { "GroupId", "StartsAt" });
        }
    }
}
