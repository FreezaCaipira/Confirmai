using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class DefaultEnablePaymentGatewaysFalse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Flip the column default from true to false so NEW groups start in
            // manual payment mode (V1). Existing groups are NOT touched -- they
            // keep whatever value they already have (lesson from Ciclo 19).
            migrationBuilder.Sql("ALTER TABLE \"Groups\" ALTER COLUMN \"EnablePaymentGateways\" SET DEFAULT false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Groups\" ALTER COLUMN \"EnablePaymentGateways\" SET DEFAULT true;");
        }
    }
}
