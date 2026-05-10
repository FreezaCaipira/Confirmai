using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMailboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserMailboxMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SenderUserId = table.Column<string>(type: "text", nullable: false),
                    RecipientUserId = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    RelatedServerId = table.Column<int>(type: "integer", nullable: true),
                    RelatedItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMailboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMailboxMessages_AspNetUsers_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserMailboxMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMailboxMessages_RecipientUserId_CreatedAt",
                table: "UserMailboxMessages",
                columns: new[] { "RecipientUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserMailboxMessages_SenderUserId_CreatedAt",
                table: "UserMailboxMessages",
                columns: new[] { "SenderUserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMailboxMessages");
        }
    }
}
