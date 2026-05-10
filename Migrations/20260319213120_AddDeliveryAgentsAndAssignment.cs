using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAgentsAndAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAgentId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDeliveryDays",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAgentId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDeliveryDays",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryAgents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Contact = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    EstimatedBusinessDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAgents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DeliveryAgentId",
                table: "Payments",
                column: "DeliveryAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryAgentId",
                table: "Orders",
                column: "DeliveryAgentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryAgents_DeliveryAgentId",
                table: "Orders",
                column: "DeliveryAgentId",
                principalTable: "DeliveryAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DeliveryAgents_DeliveryAgentId",
                table: "Payments",
                column: "DeliveryAgentId",
                principalTable: "DeliveryAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryAgents_DeliveryAgentId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DeliveryAgents_DeliveryAgentId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "DeliveryAgents");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DeliveryAgentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryAgentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAgentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDays",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeliveryAgentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDays",
                table: "Orders");
        }
    }
}


