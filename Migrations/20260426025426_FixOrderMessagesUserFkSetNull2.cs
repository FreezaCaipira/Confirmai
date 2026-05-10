using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderMessagesUserFkSetNull2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderMessages_AspNetUsers_UserId",
                table: "OrderMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderMessages_AspNetUsers_UserId",
                table: "OrderMessages",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderMessages_AspNetUsers_UserId",
                table: "OrderMessages");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderMessages_AspNetUsers_UserId",
                table: "OrderMessages",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
