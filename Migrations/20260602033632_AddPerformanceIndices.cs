using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Índice em EventConfirmations.UserId - otimiza filtros por usuário
            migrationBuilder.CreateIndex(
                name: "idx_eventconfirmations_userid",
                table: "EventConfirmations",
                column: "UserId");

            // Índice em EventConfirmations.PaymentStatus - otimiza filtros de pagamento
            migrationBuilder.CreateIndex(
                name: "idx_eventconfirmations_paymentstatus",
                table: "EventConfirmations",
                column: "PaymentStatus");

            // Índice composto para queries que filtram por ambos UserId e PaymentStatus
            migrationBuilder.CreateIndex(
                name: "idx_eventconfirmations_userid_paymentstatus",
                table: "EventConfirmations",
                columns: new[] { "UserId", "PaymentStatus" });

            // Índice em Events.GroupId - otimiza listagem de eventos por grupo
            migrationBuilder.CreateIndex(
                name: "idx_events_groupid",
                table: "Events",
                column: "GroupId");

            // Índice em PaymentRecords.BuyerId - otimiza histórico de compras
            migrationBuilder.CreateIndex(
                name: "idx_paymentrecords_buyerid",
                table: "PaymentRecords",
                column: "BuyerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove índices em reverso
            migrationBuilder.DropIndex(
                name: "idx_eventconfirmations_userid",
                table: "EventConfirmations");

            migrationBuilder.DropIndex(
                name: "idx_eventconfirmations_paymentstatus",
                table: "EventConfirmations");

            migrationBuilder.DropIndex(
                name: "idx_eventconfirmations_userid_paymentstatus",
                table: "EventConfirmations");

            migrationBuilder.DropIndex(
                name: "idx_events_groupid",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "idx_paymentrecords_buyerid",
                table: "PaymentRecords");
        }
    }
}
