using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryVerificationToOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InventoryLastVerifiedAt",
                table: "ItemOffers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InventoryVerifiedQty",
                table: "ItemOffers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryLastVerifiedAt",
                table: "ItemOffers");

            migrationBuilder.DropColumn(
                name: "InventoryVerifiedQty",
                table: "ItemOffers");
        }
    }
}
