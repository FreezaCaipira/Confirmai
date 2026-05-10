using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddMailboxArchiveFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedByRecipientAt",
                table: "UserMailboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedBySenderAt",
                table: "UserMailboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchivedByRecipient",
                table: "UserMailboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchivedBySender",
                table: "UserMailboxMessages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedByRecipientAt",
                table: "UserMailboxMessages");

            migrationBuilder.DropColumn(
                name: "ArchivedBySenderAt",
                table: "UserMailboxMessages");

            migrationBuilder.DropColumn(
                name: "IsArchivedByRecipient",
                table: "UserMailboxMessages");

            migrationBuilder.DropColumn(
                name: "IsArchivedBySender",
                table: "UserMailboxMessages");
        }
    }
}
