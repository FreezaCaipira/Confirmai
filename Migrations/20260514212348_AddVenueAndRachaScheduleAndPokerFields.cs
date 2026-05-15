using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Confirmai.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueAndRachaScheduleAndPokerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_DeliveryAgents_DeliveryAgentId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Servers_ServerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_DeliveryAgents_DeliveryAgentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "DeliveryAgents");

            migrationBuilder.DropTable(
                name: "DeliveryAuditLogs");

            migrationBuilder.DropTable(
                name: "GameLoginTokens");

            migrationBuilder.DropTable(
                name: "ItemOffers");

            migrationBuilder.DropTable(
                name: "ProductServers");

            migrationBuilder.DropTable(
                name: "SellerRecommendations");

            migrationBuilder.DropTable(
                name: "ServerApiKeys");

            migrationBuilder.DropTable(
                name: "ServerMembers");

            migrationBuilder.DropTable(
                name: "ServerRegistrationRequests");

            migrationBuilder.DropTable(
                name: "Servers");

            migrationBuilder.DropIndex(
                name: "IX_Payments_DeliveryAgentId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ItemOfferId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DeliveryAgentId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ItemOfferId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServerId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryAgentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InGamePlayerName",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ItemOfferId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "DeliveryAgentId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ItemOfferId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "Orders");

            migrationBuilder.AddColumn<decimal>(
                name: "AddonAmount",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AddonDoubleAmount",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuyInAmount",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashIncludes",
                table: "Events",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashMaxBuyIn",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashMinBuyIn",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationMinutes",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GTD",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeGameCode",
                table: "Events",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InitialBlindBB",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LateRegEndsAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocalName",
                table: "Events",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Modality",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PokerEventType",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PokerHouseName",
                table: "Events",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RachaScheduleId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RebuyAmount",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RebuyDoubleAmount",
                table: "Events",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartingStack",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VenueId",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    City = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RachaSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    VenueId = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    TimeOfDay = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    LocalName = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RachaSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RachaSchedules_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RachaSchedules_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_HomeGameCode",
                table: "Events",
                column: "HomeGameCode",
                unique: true,
                filter: "\"HomeGameCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Events_RachaScheduleId",
                table: "Events",
                column: "RachaScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_VenueId",
                table: "Events",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_RachaSchedules_GroupId_DayOfWeek_TimeOfDay",
                table: "RachaSchedules",
                columns: new[] { "GroupId", "DayOfWeek", "TimeOfDay" });

            migrationBuilder.CreateIndex(
                name: "IX_RachaSchedules_VenueId",
                table: "RachaSchedules",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_City_StateCode_IsActive",
                table: "Venues",
                columns: new[] { "City", "StateCode", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Events_RachaSchedules_RachaScheduleId",
                table: "Events",
                column: "RachaScheduleId",
                principalTable: "RachaSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Venues_VenueId",
                table: "Events",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_RachaSchedules_RachaScheduleId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Venues_VenueId",
                table: "Events");

            migrationBuilder.DropTable(
                name: "RachaSchedules");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Events_HomeGameCode",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_RachaScheduleId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_VenueId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AddonAmount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AddonDoubleAmount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BuyInAmount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CashIncludes",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CashMaxBuyIn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CashMinBuyIn",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DurationMinutes",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "GTD",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "HomeGameCode",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "InitialBlindBB",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LateRegEndsAt",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "LocalName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Modality",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PokerEventType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "PokerHouseName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RachaScheduleId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RebuyAmount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RebuyDoubleAmount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "StartingStack",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "Events");

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAgentId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InGamePlayerName",
                table: "Payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemOfferId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryAgentId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemOfferId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryAgents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Contact = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedBusinessDays = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAgents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LogoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PrimaryGameMasterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Region = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    TibiaVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    ActorPlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Detail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EventType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ItemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    TargetPlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
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

            migrationBuilder.CreateTable(
                name: "GameLoginTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    ConsumedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsumedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLoginTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameLoginTokens_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemOffers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SellerUserId = table.Column<string>(type: "text", nullable: false),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    AccentColor = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InventoryLastVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InventoryVerifiedQty = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PricingGateway = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    UseSiteIntermediary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemOffers_AspNetUsers_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemOffers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductServers",
                columns: table => new
                {
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ServerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductServers", x => new { x.ProductId, x.ServerId });
                    table.ForeignKey(
                        name: "FK_ProductServers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductServers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellerRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RecommenderUserId = table.Column<string>(type: "text", nullable: false),
                    SellerUserId = table.Column<string>(type: "text", nullable: false),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_AspNetUsers_RecommenderUserId",
                        column: x => x.RecommenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_AspNetUsers_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SellerRecommendations_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerApiKeys_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FailedDeliveryCount = table.Column<int>(type: "integer", nullable: false),
                    InGamePlayerName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServerMembers_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServerRegistrationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApprovedServerId = table.Column<int>(type: "integer", nullable: true),
                    RequesterUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ReviewerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RequestedLogoPath = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    RequestedName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReviewNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TibiaVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerRegistrationRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServerRegistrationRequests_AspNetUsers_RequesterUserId",
                        column: x => x.RequesterUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServerRegistrationRequests_AspNetUsers_ReviewerUserId",
                        column: x => x.ReviewerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ServerRegistrationRequests_Servers_ApprovedServerId",
                        column: x => x.ApprovedServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_DeliveryAgentId",
                table: "Payments",
                column: "DeliveryAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ItemOfferId",
                table: "Payments",
                column: "ItemOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DeliveryAgentId",
                table: "Orders",
                column: "DeliveryAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ItemOfferId",
                table: "Orders",
                column: "ItemOfferId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServerId",
                table: "Orders",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAuditLogs_OrderId",
                table: "DeliveryAuditLogs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryAuditLogs_ServerId_OccurredAtUtc",
                table: "DeliveryAuditLogs",
                columns: new[] { "ServerId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GameLoginTokens_ServerId",
                table: "GameLoginTokens",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemOffers_SellerUserId",
                table: "ItemOffers",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemOffers_ServerId_ItemKey_IsActive_CreatedAt",
                table: "ItemOffers",
                columns: new[] { "ServerId", "ItemKey", "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemOffers_ServerId_SellerUserId",
                table: "ItemOffers",
                columns: new[] { "ServerId", "SellerUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductServers_ServerId",
                table: "ProductServers",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_RecommenderUserId",
                table: "SellerRecommendations",
                column: "RecommenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_SellerUserId",
                table: "SellerRecommendations",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerRecommendations_ServerId_ItemKey_SellerUserId_Recomme~",
                table: "SellerRecommendations",
                columns: new[] { "ServerId", "ItemKey", "SellerUserId", "RecommenderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerApiKeys_KeyHash",
                table: "ServerApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerApiKeys_KeyPrefix",
                table: "ServerApiKeys",
                column: "KeyPrefix");

            migrationBuilder.CreateIndex(
                name: "IX_ServerApiKeys_ServerId",
                table: "ServerApiKeys",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_ServerMembers_ServerId_UserId",
                table: "ServerMembers",
                columns: new[] { "ServerId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServerMembers_UserId",
                table: "ServerMembers",
                column: "UserId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_DeliveryAgents_DeliveryAgentId",
                table: "Orders",
                column: "DeliveryAgentId",
                principalTable: "DeliveryAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ItemOffers_ItemOfferId",
                table: "Orders",
                column: "ItemOfferId",
                principalTable: "ItemOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Servers_ServerId",
                table: "Orders",
                column: "ServerId",
                principalTable: "Servers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_DeliveryAgents_DeliveryAgentId",
                table: "Payments",
                column: "DeliveryAgentId",
                principalTable: "DeliveryAgents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ItemOffers_ItemOfferId",
                table: "Payments",
                column: "ItemOfferId",
                principalTable: "ItemOffers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
