using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchSchedulesToGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupId1",
                table: "RachaSchedules",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RachaSchedules_GroupId1",
                table: "RachaSchedules",
                column: "GroupId1");

            migrationBuilder.AddForeignKey(
                name: "FK_RachaSchedules_Groups_GroupId1",
                table: "RachaSchedules",
                column: "GroupId1",
                principalTable: "Groups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RachaSchedules_Groups_GroupId1",
                table: "RachaSchedules");

            migrationBuilder.DropIndex(
                name: "IX_RachaSchedules_GroupId1",
                table: "RachaSchedules");

            migrationBuilder.DropColumn(
                name: "GroupId1",
                table: "RachaSchedules");
        }
    }
}
