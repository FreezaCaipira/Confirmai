using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPixProofToEventConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PixProofContentType",
                table: "EventConfirmations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PixProofImageData",
                table: "EventConfirmations",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PixProofUploadedAt",
                table: "EventConfirmations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PixProofContentType",
                table: "EventConfirmations");

            migrationBuilder.DropColumn(
                name: "PixProofImageData",
                table: "EventConfirmations");

            migrationBuilder.DropColumn(
                name: "PixProofUploadedAt",
                table: "EventConfirmations");
        }
    }
}
