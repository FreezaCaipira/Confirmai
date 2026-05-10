using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddItemOffersStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    UseSiteIntermediary = table.Column<bool>(type: "boolean", nullable: false),
                    SellerUserId = table.Column<string>(type: "text", nullable: false),
                    InventoryConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmationSource = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    InventoryConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AccentColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemOffers_AspNetUsers_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemOffers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemOffers_SellerUserId",
                table: "ItemOffers",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemOffers_ServerId_ItemKey_IsActive_CreatedAt",
                table: "ItemOffers",
                columns: new[] { "ServerId", "ItemKey", "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemOffers_ServerId_SellerUserId",
                table: "ItemOffers",
                columns: new[] { "ServerId", "SellerUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemOffers");
        }
    }
}
