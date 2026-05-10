using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddUserContactHandles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BinanceAddress",
                table: "AspNetUsers",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscordHandle",
                table: "AspNetUsers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramHandle",
                table: "AspNetUsers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaypalAddress",
                table: "AspNetUsers",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XHandle",
                table: "AspNetUsers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BinanceAddress",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DiscordHandle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "InstagramHandle",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaypalAddress",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "XHandle",
                table: "AspNetUsers");
        }
    }
}
