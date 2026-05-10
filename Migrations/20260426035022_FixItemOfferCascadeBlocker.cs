using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class FixItemOfferCascadeBlocker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders",
                column: "ItemOfferId",
                principalTable: "ItemOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments",
                column: "ItemOfferId",
                principalTable: "ItemOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders",
                column: "ItemOfferId",
                principalTable: "ItemOffers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments",
                column: "ItemOfferId",
                principalTable: "ItemOffers",
                principalColumn: "Id");
        }
    }
}
