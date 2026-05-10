using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Confirmai.Data;

#nullable disable

namespace Confirmai.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260421090000_AddInGamePlayerNameToPayments")]
    public partial class AddInGamePlayerNameToPayments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InGamePlayerName",
                table: "Payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InGamePlayerName",
                table: "Payments");
        }
    }
}