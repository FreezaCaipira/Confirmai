using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayersPerSideToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayersPerSide",
                table: "Events",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlayersPerSide",
                table: "Events");
        }
    }
}
