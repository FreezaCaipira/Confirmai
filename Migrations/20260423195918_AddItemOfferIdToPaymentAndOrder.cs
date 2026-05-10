using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddItemOfferIdToPaymentAndOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemOfferId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemOfferId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ItemOfferId",
                table: "Payments",
                column: "ItemOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ItemOfferId",
                table: "Orders",
                column: "ItemOfferId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ItemOfferId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ItemOfferId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ItemOfferId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ItemOfferId",
                table: "Orders");
        }
    }
}
