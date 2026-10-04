using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class WhatsAppDispatchNullableEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppDispatches_Events_EventId",
                table: "WhatsAppDispatches");

            migrationBuilder.AlterColumn<int>(
                name: "EventId",
                table: "WhatsAppDispatches",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppDispatches_Events_EventId",
                table: "WhatsAppDispatches",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppDispatches_Events_EventId",
                table: "WhatsAppDispatches");

            migrationBuilder.AlterColumn<int>(
                name: "EventId",
                table: "WhatsAppDispatches",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppDispatches_Events_EventId",
                table: "WhatsAppDispatches",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
