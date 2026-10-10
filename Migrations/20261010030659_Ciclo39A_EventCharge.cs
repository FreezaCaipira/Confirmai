using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class Ciclo39A_EventCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeePercent",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChargedPrice",
                table: "EventConfirmations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceOptionId",
                table: "EventConfirmations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventPriceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    PlatformFeePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPriceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventPriceOptions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventConfirmations_PriceOptionId",
                table: "EventConfirmations",
                column: "PriceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventPriceOptions_EventId",
                table: "EventPriceOptions",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventConfirmations_EventPriceOptions_PriceOptionId",
                table: "EventConfirmations",
                column: "PriceOptionId",
                principalTable: "EventPriceOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventConfirmations_EventPriceOptions_PriceOptionId",
                table: "EventConfirmations");

            migrationBuilder.DropTable(
                name: "EventPriceOptions");

            migrationBuilder.DropIndex(
                name: "IX_EventConfirmations_PriceOptionId",
                table: "EventConfirmations");

            migrationBuilder.DropColumn(
                name: "PlatformFeePercent",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ChargedPrice",
                table: "EventConfirmations");

            migrationBuilder.DropColumn(
                name: "PriceOptionId",
                table: "EventConfirmations");
        }
    }
}
