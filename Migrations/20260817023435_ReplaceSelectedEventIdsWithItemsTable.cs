using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSelectedEventIdsWithItemsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the new items table first so we can backfill from the
            //    legacy SelectedEventIds CSV column before dropping it.
            migrationBuilder.CreateTable(
                name: "PlatformFeeSettlementItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SettlementId = table.Column<int>(type: "integer", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    FeeAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformFeeSettlementItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformFeeSettlementItems_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformFeeSettlementItems_PlatformFeeSettlements_Settlemen~",
                        column: x => x.SettlementId,
                        principalTable: "PlatformFeeSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeeSettlementItems_EventId",
                table: "PlatformFeeSettlementItems",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeeSettlementItems_SettlementId_EventId",
                table: "PlatformFeeSettlementItems",
                columns: new[] { "SettlementId", "EventId" },
                unique: true);

            // 2. Backfill: for each settlement that still has a SelectedEventIds CSV,
            //    explode it into one row per event id. The FeeAmount snapshot is the
            //    sum of PlatformFeeAmount on the confirmations of that event — the
            //    same value SubmitSettlementAsync would have stored at submission time.
            //    Using unnest/string_to_array keeps it all in SQL (PostgreSQL only).
            migrationBuilder.Sql(@"
INSERT INTO ""PlatformFeeSettlementItems"" (""SettlementId"", ""EventId"", ""FeeAmount"")
SELECT s.""Id"", eid::int, COALESCE(fee.""FeeAmount"", 0)
FROM ""PlatformFeeSettlements"" s
CROSS JOIN LATERAL unnest(string_to_array(s.""SelectedEventIds"", ',')) AS eid
LEFT JOIN LATERAL (
    SELECT COALESCE(SUM(ec.""PlatformFeeAmount""), 0) AS ""FeeAmount""
    FROM ""EventConfirmations"" ec
    WHERE ec.""EventId"" = eid::int
) fee ON true
WHERE s.""SelectedEventIds"" IS NOT NULL
  AND trim(s.""SelectedEventIds"") <> '';
");

            // 3. Drop the legacy CSV column now that the data is preserved.
            migrationBuilder.DropColumn(
                name: "SelectedEventIds",
                table: "PlatformFeeSettlements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedEventIds",
                table: "PlatformFeeSettlements",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            // Reconstruct the CSV from the items table so the downgrade is reversible.
            migrationBuilder.Sql(@"
UPDATE ""PlatformFeeSettlements"" s
SET ""SelectedEventIds"" = sub.""csv""
FROM (
    SELECT ""SettlementId"", string_agg(""EventId""::text, ',' ORDER BY ""EventId"") AS ""csv""
    FROM ""PlatformFeeSettlementItems""
    GROUP BY ""SettlementId""
) sub
WHERE sub.""SettlementId"" = s.""Id"";
");

            migrationBuilder.DropTable(
                name: "PlatformFeeSettlementItems");
        }
    }
}
