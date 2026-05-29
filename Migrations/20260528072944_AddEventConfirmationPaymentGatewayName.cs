using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddEventConfirmationPaymentGatewayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentGatewayName",
                table: "EventConfirmations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventConfirmations_PixTxId",
                table: "EventConfirmations",
                column: "PixTxId",
                unique: true,
                filter: "\"PixTxId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventConfirmations_PixTxId",
                table: "EventConfirmations");

            migrationBuilder.DropColumn(
                name: "PaymentGatewayName",
                table: "EventConfirmations");
        }
    }
}
