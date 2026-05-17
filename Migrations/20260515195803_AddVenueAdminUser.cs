using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VenueAdminUserId",
                table: "Venues",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_VenueAdminUserId",
                table: "Venues",
                column: "VenueAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_AspNetUsers_VenueAdminUserId",
                table: "Venues",
                column: "VenueAdminUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venues_AspNetUsers_VenueAdminUserId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_VenueAdminUserId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "VenueAdminUserId",
                table: "Venues");
        }
    }
}
