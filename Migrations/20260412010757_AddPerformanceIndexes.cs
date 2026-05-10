using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Products_Category",
                table: "Products",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Address",
                table: "Payments",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentId",
                table: "Payments",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Category",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Address",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");
        }
    }
}
