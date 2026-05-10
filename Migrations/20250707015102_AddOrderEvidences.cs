using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderEvidences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BuyerEvidenceAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerEvidenceComment",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerEvidencePath",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerEvidenceAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerEvidenceComment",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellerEvidencePath",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerEvidenceAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BuyerEvidenceComment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BuyerEvidencePath",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerEvidenceAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerEvidenceComment",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerEvidencePath",
                table: "Orders");
        }
    }
}


