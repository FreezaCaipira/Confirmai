using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SellerUserId = table.Column<string>(type: "text", nullable: false),
                    RecommenderUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_AspNetUsers_RecommenderUserId",
                        column: x => x.RecommenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_AspNetUsers_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_RecommenderUserId",
                table: "SellerRecommendations",
                column: "RecommenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_SellerUserId",
                table: "SellerRecommendations",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_ServerId_ItemKey_SellerUserId_Recomme~",
                table: "SellerRecommendations",
                columns: new[] { "ServerId", "ItemKey", "SellerUserId", "RecommenderUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerRecommendations");
        }
    }
}
