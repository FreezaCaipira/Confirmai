using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddServerMembersAndSalesTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServerId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServerMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerMembers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServerId",
                table: "Orders",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerMembers_ServerId_UserId",
                table: "ServerMembers",
                columns: new[] { "ServerId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerMembers_UserId",
                table: "ServerMembers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Servers_ServerId",
                table: "Orders",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Servers_ServerId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ServerMembers");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "Orders");
        }
    }
}
