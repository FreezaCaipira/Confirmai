using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    EventType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ActorPlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TargetPlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ItemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryAuditLogs_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeliveryAuditLogs_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAuditLogs_OrderId",
                table: "DeliveryAuditLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAuditLogs_ServerId_OccurredAtUtc",
                table: "DeliveryAuditLogs",
                columns: new[] { "ServerId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryAuditLogs");
        }
    }
}
