using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInventorySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerInventorySnapshots");

            migrationBuilder.DropColumn(
                name: "ConfirmationSource",
                table: "ItemOffers");

            migrationBuilder.DropColumn(
                name: "InventoryConfirmed",
                table: "ItemOffers");

            migrationBuilder.DropColumn(
                name: "InventoryConfirmedAt",
                table: "ItemOffers");

            migrationBuilder.AddColumn<int>(
                name: "FailedDeliveryCount",
                table: "ServerMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedDeliveryCount",
                table: "ServerMembers");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationSource",
                table: "ItemOffers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "InventoryConfirmed",
                table: "ItemOffers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "InventoryConfirmedAt",
                table: "ItemOffers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayerInventorySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    PlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventorySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerInventorySnapshots_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventorySnapshots_ServerId",
                table: "PlayerInventorySnapshots",
                column: "ServerId");
        }
    }
}
