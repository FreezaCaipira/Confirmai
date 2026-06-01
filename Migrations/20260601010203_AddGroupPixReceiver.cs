using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupPixReceiver : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PixReceiverUserId",
                table: "Groups",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PixReceiverUserId",
                table: "Groups",
                column: "PixReceiverUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_AspNetUsers_PixReceiverUserId",
                table: "Groups",
                column: "PixReceiverUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_AspNetUsers_PixReceiverUserId",
                table: "Groups");

            migrationBuilder.DropIndex(
                name: "IX_Groups_PixReceiverUserId",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "PixReceiverUserId",
                table: "Groups");
        }
    }
}
