using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentSellerFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_SellerId",
                table: "Payments");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_SellerId",
                table: "Payments",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_SellerId",
                table: "Payments");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_SellerId",
                table: "Payments",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
