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
            migrationBuilder.Sql("UPDATE \"Groups\" SET \"EnablePaymentGateways\" = true WHERE \"EnablePaymentGateways\" = false;");
            migrationBuilder.Sql("ALTER TABLE \"Groups\" ALTER COLUMN \"EnablePaymentGateways\" SET DEFAULT true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Groups\" ALTER COLUMN \"EnablePaymentGateways\" SET DEFAULT false;");
        }
    }
}
