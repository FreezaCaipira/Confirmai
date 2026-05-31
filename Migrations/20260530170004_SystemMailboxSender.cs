using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class SystemMailboxSender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages");

            migrationBuilder.AlterColumn<string>(
                name: "SenderUserId",
                table: "UserMailboxMessages",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages",
                column: "SenderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages");

            migrationBuilder.AlterColumn<string>(
                name: "SenderUserId",
                table: "UserMailboxMessages",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages",
                column: "SenderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
