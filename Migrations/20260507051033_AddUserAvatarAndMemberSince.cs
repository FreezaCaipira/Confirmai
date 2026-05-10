using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarAndMemberSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarPath",
                table: "AspNetUsers",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MemberSince",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarPath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MemberSince",
                table: "AspNetUsers");
        }
    }
}
