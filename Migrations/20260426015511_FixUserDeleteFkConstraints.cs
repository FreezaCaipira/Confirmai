using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class FixUserDeleteFkConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_BuyerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_SellerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_RequesterUserId",
                table: "ServerRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_ReviewerUserId",
                table: "ServerRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_RecipientUserId",
                table: "UserMailboxMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages");

            migrationBuilder.AlterColumn<string>(
                name: "RequesterUserId",
                table: "ServerRegistrationRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerId",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_BuyerId",
                table: "Orders",
                column: "BuyerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_SellerId",
                table: "Orders",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_RequesterUserId",
                table: "ServerRegistrationRequests",
                column: "RequesterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_ReviewerUserId",
                table: "ServerRegistrationRequests",
                column: "ReviewerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_RecipientUserId",
                table: "UserMailboxMessages",
                column: "RecipientUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages",
                column: "SenderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_BuyerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_AspNetUsers_SellerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_RequesterUserId",
                table: "ServerRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_ReviewerUserId",
                table: "ServerRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_RecipientUserId",
                table: "UserMailboxMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages");

            migrationBuilder.AlterColumn<string>(
                name: "RequesterUserId",
                table: "ServerRegistrationRequests",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BuyerId",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_BuyerId",
                table: "Orders",
                column: "BuyerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_AspNetUsers_SellerId",
                table: "Orders",
                column: "SellerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_RequesterUserId",
                table: "ServerRegistrationRequests",
                column: "RequesterUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServerRegistrationRequests_AspNetUsers_ReviewerUserId",
                table: "ServerRegistrationRequests",
                column: "ReviewerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_RecipientUserId",
                table: "UserMailboxMessages",
                column: "RecipientUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                table: "UserMailboxMessages",
                column: "SenderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
