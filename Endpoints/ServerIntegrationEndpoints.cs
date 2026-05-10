using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Confirmai.Services;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Endpoints;

public static class ServerIntegrationEndpoints
{
    public static void MapServerIntegrationApi(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/server")
            .RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(ApiKeyAuthDefaults.AuthenticationScheme)
                      .RequireAuthenticatedUser())
            .RequireRateLimiting("integration");

        group.MapGet("/ping", HandlePing)
            .WithName("ServerPing");

        group.MapGet("/trades/pending", HandleGetPendingTrades)
            .WithName("GetPendingTrades");

        group.MapPost("/trades/confirm", HandleConfirmTrade)
            .WithName("ConfirmTrade");

        group.MapPost("/trades/reject", HandleRejectTrade)
            .WithName("RejectTrade");

        group.MapGet("/offers", HandleGetOffers)
            .WithName("GetServerOffers");

        group.MapPost("/generate-login-link", HandleGenerateLoginLink)
            .WithName("GenerateLoginLink");

        group.MapPost("/delivery/log", HandleDeliveryLog)
            .WithName("LogDeliveryEvent");

        group.MapPost("/offers/inventory-report", HandleInventoryReport)
            .WithName("ReportOfferInventory");
    }

    private static IResult HandlePing(ClaimsPrincipal user)
    {
        var serverId = user.FindFirstValue("ServerId");
        var serverName = user.FindFirstValue("ServerName");

        return Results.Ok(new
        {
            status = "ok",
            serverId,
            serverName,
            timestamp = DateTime.UtcNow
        });
    }

    private static async Task<IResult> HandleGetPendingTrades(ClaimsPrincipal user, TibiaServerService tibiaServerService, AdminSettingsService adminSettingsService)
    {
        if (!await adminSettingsService.GetLuaDeliveryEnabledAsync())
            return Results.Json(new { error = "Entrega automatica via Lua esta desabilitada neste marketplace." }, statusCode: 503);

        if (!TryGetServerId(user, out var serverId))
        {
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });
        }

        var trades = await tibiaServerService.GetPendingTradesAsync(serverId);

        return Results.Ok(new
        {
            serverId,
            trades,
            message = trades.Count == 0
                ? "Nenhuma trade pendente no momento."
                : $"{trades.Count} trade(s) pendente(s) encontrada(s)."
        });
    }

    private static async Task<IResult> HandleConfirmTrade(
        ClaimsPrincipal user,
        [FromBody] ConfirmTradeRequest? request,
        TibiaServerService tibiaServerService,
        AppDbContext db,
        LogService logService,
        AdminSettingsService adminSettingsService)
    {
        if (!await adminSettingsService.GetLuaDeliveryEnabledAsync())
            return Results.Json(new { error = "Entrega automatica via Lua esta desabilitada neste marketplace." }, statusCode: 503);

        if (!TryGetServerId(user, out var serverId))
        {
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });
        }

        if (request == null || request.TradeId <= 0)
        {
            return Results.BadRequest(new { error = "TradeId é obrigatório." });
        }

        var result = await tibiaServerService.ConfirmPendingTradeAsync(serverId, request.TradeId);

        if (!result.Success)
        {
            if (result.Reason == "TradeNotFound")
            {
                await logService.LogAsync(
                    $"Confirmacao de trade recusada: trade nao encontrada (ServerId: {serverId}, TradeId: {request.TradeId}).",
                    source: AdminAuditSources.ServerIntegration,
                    level: "Warning");

                return Results.NotFound(new { error = "Trade nao encontrada para este servidor." });
            }

            await logService.LogAsync(
                $"Confirmacao de trade recusada por status invalido (ServerId: {serverId}, TradeId: {request.TradeId}, StatusAtual: {result.Order?.Status}).",
                source: AdminAuditSources.ServerIntegration,
                level: "Warning");

            return Results.BadRequest(new { error = "Trade nao esta aguardando entrega in-game." });
        }

        await logService.LogAsync(
            $"Trade confirmada via API do servidor (ServerId: {serverId}, TradeId: {request.TradeId}, StatusNovo: {result.Order!.Status}).",
            source: AdminAuditSources.ServerIntegration,
            level: "Info");

        // Write structured delivery audit entry
        var order = result.Order!;
        var auditEntry = new DeliveryAuditLog
        {
            ServerId  = serverId,
            OrderId   = order.Id,
            EventType = "ConfirmTrade",
            Success   = true,
            Detail    = $"Trade confirmada via API. StatusNovo: {order.Status}"
        };

        // Best-effort enrichment — query members and offer directly
        if (!string.IsNullOrWhiteSpace(order.SellerId))
        {
            var sellerMember = await db.ServerMembers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == order.SellerId);
            auditEntry.ActorPlayerName = sellerMember?.InGamePlayerName;
        }
        if (!string.IsNullOrWhiteSpace(order.BuyerId))
        {
            var buyerMember = await db.ServerMembers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == order.BuyerId);
            auditEntry.TargetPlayerName = buyerMember?.InGamePlayerName;
        }
        if (order.ItemOfferId.HasValue)
        {
            var offer = await db.ItemOffers.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == order.ItemOfferId.Value);
            if (offer is not null)
            {
                auditEntry.ItemKey  = offer.ItemKey;
                auditEntry.ItemName = offer.ItemName;
                auditEntry.Quantity = offer.Quantity;
            }
        }

        db.DeliveryAuditLogs.Add(auditEntry);
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            serverId,
            tradeId = request.TradeId,
            confirmed = true,
            status = result.Order!.Status.ToString(),
            deliveredAtUtc = result.Order.DeliveredAt,
            message = "Trade confirmada e enviada para revisao administrativa."
        });
    }

    private static async Task<IResult> HandleRejectTrade(
        ClaimsPrincipal user,
        [FromBody] RejectTradeRequest? request,
        TibiaServerService tibiaServerService,
        AppDbContext db,
        LogService logService,
        AdminSettingsService adminSettingsService)
    {
        if (!await adminSettingsService.GetLuaDeliveryEnabledAsync())
            return Results.Json(new { error = "Entrega automatica via Lua esta desabilitada neste marketplace." }, statusCode: 503);

        if (!TryGetServerId(user, out var serverId))
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });

        if (request == null || request.TradeId <= 0)
            return Results.BadRequest(new { error = "TradeId é obrigatório." });

        var result = await tibiaServerService.RejectPendingTradeAsync(serverId, request.TradeId, request.Reason);

        if (!result.Success)
        {
            if (result.Reason == "TradeNotFound")
            {
                await logService.LogAsync(
                    $"Rejeicao de trade recusada: trade nao encontrada (ServerId: {serverId}, TradeId: {request.TradeId}).",
                    source: AdminAuditSources.ServerIntegration,
                    level: "Warning");
                return Results.NotFound(new { error = "Trade nao encontrada para este servidor." });
            }

            await logService.LogAsync(
                $"Rejeicao de trade recusada por status invalido (ServerId: {serverId}, TradeId: {request.TradeId}, StatusAtual: {result.Order?.Status}).",
                source: AdminAuditSources.ServerIntegration,
                level: "Warning");
            return Results.BadRequest(new { error = "Trade nao esta aguardando entrega in-game." });
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? "sem motivo" : request.Reason;
        await logService.LogAsync(
            $"Trade rejeitada via API do servidor (ServerId: {serverId}, TradeId: {request.TradeId}, Motivo: {reason}).",
            source: AdminAuditSources.ServerIntegration,
            level: "Info");

        var order = result.Order!;
        var auditEntry = new DeliveryAuditLog
        {
            ServerId  = serverId,
            OrderId   = order.Id,
            EventType = "RejectTrade",
            Success   = false,
            Detail    = string.IsNullOrWhiteSpace(request.Reason)
                ? "Trade rejeitada via API."
                : $"Trade rejeitada via API. Motivo: {request.Reason}"
        };

        if (!string.IsNullOrWhiteSpace(order.SellerId))
        {
            var sellerMember = await db.ServerMembers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == order.SellerId);
            auditEntry.ActorPlayerName = sellerMember?.InGamePlayerName;
        }
        if (!string.IsNullOrWhiteSpace(order.BuyerId))
        {
            var buyerMember = await db.ServerMembers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == order.BuyerId);
            auditEntry.TargetPlayerName = buyerMember?.InGamePlayerName;
        }
        if (order.ItemOfferId.HasValue)
        {
            var offer = await db.ItemOffers.AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == order.ItemOfferId.Value);
            if (offer is not null)
            {
                auditEntry.ItemKey  = offer.ItemKey;
                auditEntry.ItemName = offer.ItemName;
                auditEntry.Quantity = offer.Quantity;
            }
        }

        db.DeliveryAuditLogs.Add(auditEntry);
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            serverId,
            tradeId    = request.TradeId,
            rejected   = true,
            status     = result.Order!.Status.ToString(),
            message    = "Trade enviada para disputa."
        });
    }

    private static async Task<IResult> HandleGetOffers(
        ClaimsPrincipal user,
        [FromQuery] string? itemKey,
        TibiaServerService tibiaServerService)
    {
        if (!TryGetServerId(user, out var serverId))
        {
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });
        }

        if (string.IsNullOrWhiteSpace(itemKey))
        {
            return Results.BadRequest(new { error = "itemKey e obrigatorio." });
        }

        var offers = await tibiaServerService.GetActiveItemOffersAsync(serverId, itemKey, sortByPrice: true);

        return Results.Ok(new
        {
            serverId,
            itemKey,
            offers,
            message = offers.Count == 0
                ? "Nenhuma oferta ativa encontrada para este item."
                : $"{offers.Count} oferta(s) ativa(s) encontrada(s)."
        });
    }

    private static async Task<IResult> HandleGenerateLoginLink(
        ClaimsPrincipal user,
        [FromBody] GenerateLoginLinkRequest? request,
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor)
    {
        if (!TryGetServerId(user, out var serverId))
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });

        if (request == null || string.IsNullOrWhiteSpace(request.PlayerName))
            return Results.BadRequest(new { error = "PlayerName é obrigatório." });

        var playerName = request.PlayerName.Trim();

        // Invalidate any previous unused tokens for this player/server
        var stale = await db.GameLoginTokens
            .Where(t => t.ServerId == serverId
                && t.PlayerName == playerName
                && !t.IsUsed
                && t.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();
        db.GameLoginTokens.RemoveRange(stale);

        // Generate a cryptographically random 32-byte token
        var tokenBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        var tokenHex = Convert.ToHexString(tokenBytes).ToLowerInvariant();

        var record = new GameLoginToken
        {
            ServerId = serverId,
            PlayerName = playerName,
            Token = tokenHex,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        db.GameLoginTokens.Add(record);
        await db.SaveChangesAsync();

        // Build absolute URL using the current request host
        var httpContext = httpContextAccessor.HttpContext;
        var baseUrl = httpContext is not null
            ? $"{httpContext.Request.Scheme}://{httpContext.Request.Host}"
            : "https://localhost";

        var loginUrl = $"{baseUrl}/auth/game-login?token={tokenHex}";

        return Results.Ok(new
        {
            loginUrl,
            playerName,
            expiresInSeconds = 600
        });
    }

    private static async Task<IResult> HandleDeliveryLog(
        ClaimsPrincipal user,
        [FromBody] DeliveryLogRequest? request,
        AppDbContext db)
    {
        if (!TryGetServerId(user, out var serverId))
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });

        if (request == null || string.IsNullOrWhiteSpace(request.EventType))
            return Results.BadRequest(new { error = "EventType é obrigatório." });

        var entry = new DeliveryAuditLog
        {
            ServerId        = serverId,
            OrderId         = request.OrderId,
            EventType       = request.EventType.Trim()[..Math.Min(60, request.EventType.Trim().Length)],
            ActorPlayerName = request.ActorPlayerName?.Trim(),
            TargetPlayerName= request.TargetPlayerName?.Trim(),
            ItemKey         = request.ItemKey?.Trim().ToUpperInvariant(),
            ItemName        = request.ItemName?.Trim(),
            Quantity        = request.Quantity,
            Success         = request.Success,
            Detail          = request.Detail?.Trim(),
            OccurredAtUtc   = DateTime.UtcNow
        };

        db.DeliveryAuditLogs.Add(entry);
        await db.SaveChangesAsync();

        // If item check failed (seller had no item in Depot), increment FailedDeliveryCount
        if (!request.Success
            && string.Equals(request.EventType.Trim(), "CheckInbox", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(request.ActorPlayerName))
        {
            var actorName = request.ActorPlayerName.Trim();
            var member = await db.ServerMembers
                .FirstOrDefaultAsync(m => m.ServerId == serverId
                    && m.InGamePlayerName != null
                    && m.InGamePlayerName == actorName);
            if (member != null)
            {
                member.FailedDeliveryCount++;
                await db.SaveChangesAsync();
            }
        }

        return Results.Ok(new { logged = true, id = entry.Id });
    }

    private static async Task<IResult> HandleInventoryReport(
        ClaimsPrincipal user,
        [FromBody] InventoryReportRequest? request,
        AppDbContext db,
        LogService logService)
    {
        if (!TryGetServerId(user, out var serverId))
            return Results.BadRequest(new { error = "ServerId ausente ou invalido." });

        if (request == null || request.Items == null || request.Items.Count == 0)
            return Results.BadRequest(new { error = "Items e obrigatorio e nao pode estar vazio." });

        var now = DateTime.UtcNow;
        int updated = 0;

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SellerPlayerName) || string.IsNullOrWhiteSpace(item.ItemKey))
                continue;

            // Find active offers for this seller+item on this server
            var normalizedKey = item.ItemKey.Trim().ToUpperInvariant();
            var sellerMember = await db.ServerMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ServerId == serverId
                    && m.InGamePlayerName == item.SellerPlayerName);

            if (sellerMember == null) continue;

            var offers = await db.ItemOffers
                .Where(o => o.ServerId == serverId
                    && o.SellerUserId == sellerMember.UserId
                    && o.ItemKey == normalizedKey
                    && o.IsActive)
                .ToListAsync();

            foreach (var offer in offers)
            {
                offer.InventoryLastVerifiedAt = now;
                offer.InventoryVerifiedQty = item.VerifiedQty;
                updated++;
            }
        }

        await db.SaveChangesAsync();

        await logService.LogAsync(
            $"Inventario reportado pelo servidor (ServerId: {serverId}, itens: {request.Items.Count}, ofertas atualizadas: {updated}).",
            source: AdminAuditSources.ServerIntegration,
            level: "Info");

        return Results.Ok(new { serverId, updated, reportedAt = now });
    }

    public record GenerateLoginLinkRequest(string PlayerName);

    public record ConfirmTradeRequest(int TradeId);

    public record RejectTradeRequest(int TradeId, string? Reason = null);

    public record DeliveryLogRequest(
        string EventType,
        int? OrderId = null,
        string? ActorPlayerName = null,
        string? TargetPlayerName = null,
        string? ItemKey = null,
        string? ItemName = null,
        int? Quantity = null,
        bool Success = true,
        string? Detail = null
    );

    public record InventoryReportItem(
        string SellerPlayerName,
        string ItemKey,
        int VerifiedQty
    );

    public record InventoryReportRequest(
        List<InventoryReportItem> Items
    );

    private static bool TryGetServerId(ClaimsPrincipal user, out int serverId)
    {
        serverId = 0;
        var rawValue = user.FindFirstValue("ServerId");
        return int.TryParse(rawValue, out serverId) && serverId > 0;
    }
}
