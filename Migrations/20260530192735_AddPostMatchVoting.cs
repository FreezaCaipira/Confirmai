using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPostMatchVoting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScoreTeamA",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreTeamB",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PostMatchVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    VoterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    VotedForUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    VotedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMatchVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMatchVotes_AspNetUsers_VotedForUserId",
                        column: x => x.VotedForUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PostMatchVotes_AspNetUsers_VoterUserId",
                        column: x => x.VoterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostMatchVotes_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostMatchVotes_EventId_VoterUserId",
                table: "PostMatchVotes",
                columns: new[] { "EventId", "VoterUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostMatchVotes_VotedForUserId",
                table: "PostMatchVotes",
                column: "VotedForUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMatchVotes_VoterUserId",
                table: "PostMatchVotes",
                column: "VoterUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostMatchVotes");

            migrationBuilder.DropColumn(
                name: "ScoreTeamA",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ScoreTeamB",
                table: "Events");
        }
    }
}
