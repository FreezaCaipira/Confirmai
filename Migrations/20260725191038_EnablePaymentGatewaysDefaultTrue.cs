using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class EnablePaymentGatewaysDefaultTrue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default true applies only to NEW groups. Existing groups keep their
            // current value on purpose: flipping them on would break payment for any
            // group without a configured Pix payout key (fee is enabled + checkout
            // blocks charges without a GroupPayoutAccount). Decision: Robson, opcao 1.
            migrationBuilder.Sql("ALTER TABLE \"Groups\" ALTER COLUMN \"EnablePaymentGateways\" SET DEFAULT true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Groups\" ALTER COLUMN \"EnablePaymentGateways\" SET DEFAULT false;");
        }
    }
}
