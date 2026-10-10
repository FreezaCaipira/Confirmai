using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class Ciclo39C_PartnerFeeShare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PartnerFeeSharePercent",
                table: "Groups",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PlayerFeeAmount",
                table: "EventConfirmations",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartnerFeeSharePercent",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PlayerFeeAmount",
                table: "EventConfirmations");
        }
    }
}
