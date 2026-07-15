using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutFieldsToPaymentRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "Payments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "Payments",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PayoutConfirmedAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutEndToEndId",
                table: "Payments",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutErrorMessage",
                table: "Payments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PayoutPixKey",
                table: "Payments",
                type: "character varying(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayoutRetryCount",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PayoutSentAt",
                table: "Payments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayoutStatus",
                table: "Payments",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutConfirmedAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutEndToEndId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutErrorMessage",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutPixKey",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutRetryCount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutSentAt",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayoutStatus",
                table: "Payments");
        }
    }
}
