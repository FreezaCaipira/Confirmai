using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddServerRegistrationRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TibiaVersion",
                table: "Servers",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServerRegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RequestedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TibiaVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RequestedLogoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequestNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequesterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ApprovedServerId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerRegistrationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerRegistrationRequests_AspNetUsers_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServerRegistrationRequests_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServerRegistrationRequests_Servers_ApprovedServerId",
                        column: x => x.ApprovedServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServerRegistrationRequests_ApprovedServerId",
                table: "ServerRegistrationRequests",
                column: "ApprovedServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerRegistrationRequests_RequesterUserId",
                table: "ServerRegistrationRequests",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerRegistrationRequests_ReviewerUserId",
                table: "ServerRegistrationRequests",
                column: "ReviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerRegistrationRequests_Status_CreatedAt",
                table: "ServerRegistrationRequests",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServerRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "TibiaVersion",
                table: "Servers");
        }
    }
}
