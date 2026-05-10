using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRecordSellerIdFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerId",
                table: "Payments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SellerId",
                table: "Payments",
                column: "SellerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_AspNetUsers_SellerId",
                table: "Payments",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_AspNetUsers_SellerId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_SellerId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SellerId",
                table: "Payments");
        }
    }
}
