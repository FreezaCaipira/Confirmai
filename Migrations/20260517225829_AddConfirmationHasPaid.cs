using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmationHasPaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPaid",
                table: "EventConfirmations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPaid",
                table: "EventConfirmations");
        }
    }
}
