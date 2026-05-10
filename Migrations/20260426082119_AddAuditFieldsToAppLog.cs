using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToAppLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "Logs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityId",
                table: "Logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "Logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Logs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Logs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Logs_EntityType_EntityId",
                table: "Logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Logs_EventType",
                table: "Logs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_Timestamp",
                table: "Logs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Logs_EntityType_EntityId",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_EventType",
                table: "Logs");

            migrationBuilder.DropIndex(
                name: "IX_Logs_Timestamp",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Logs");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Logs");
        }
    }
}
