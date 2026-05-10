using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Caching.Memory;
using Confirmai.Data;
using Confirmai.Enums;
using Confirmai.Models;

namespace Confirmai.Services;

public class TibiaServerService
{
    public sealed class PendingTradeView
    {
        public int Id { get; init; }
        public int OrderId { get; init; }
        public int ServerId { get; init; }
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Quantity { get; init; } = 1;
        public decimal AmountBtc { get; init; }
        public string? BuyerUserId { get; init; }
        public string? SellerUserId { get; init; }
        public string? PlayerName { get; init; }
        public string? PaymentInvoiceId { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public string Status { get; init; } = string.Empty;
        public string? ItemKey { get; init; }
        public string? ItemName { get; init; }
        public string? SellerPlayerName { get; init; }
    }

    public sealed class ConfirmPendingTradeResult
    {
        public bool Success { get; init; }
        public string Reason { get; init; } = string.Empty;
        public OrderModel? Order { get; init; }
    }

    public sealed class RejectPendingTradeResult
    {
        public bool Success { get; init; }
        public string Reason { get; init; } = string.Empty;
        public OrderModel? Order { get; init; }
    }

    public sealed class CreateItemOfferRequest
    {
        public int ServerId { get; init; }
        public string ItemKey { get; init; } = string.Empty;
        public string? ItemName { get; init; }
        public string SellerUserId { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public string? PricingGateway { get; init; }
    }

    public sealed class CreateItemOfferResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public ItemOffer? Offer { get; init; }
    }

    public sealed class ItemOfferView
    {
        public int OfferId { get; init; }
        public int ProductId { get; init; }
        public string ItemKey { get; init; } = string.Empty;
        public string ItemName { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
        public string? PricingGateway { get; init; }
        public string SellerUserId { get; init; } = string.Empty;
        public string SellerUserName { get; init; } = string.Empty;
        public string? AccentColor { get; init; }
        public DateTime CreatedAt { get; init; }
        public int FailedDeliveryCount { get; init; }
    }

    public sealed class SellerRecommendationView
    {
        public string SellerUserId { get; init; } = string.Empty;
        public string RecommenderUserId { get; init; } = string.Empty;
        public string RecommenderUserName { get; init; } = string.Empty;
    }

    public sealed class ServerMemberView
    {
        public string UserId { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public ServerMemberRole Role { get; init; }
        public bool IsSystemAdmin { get; init; }
    }

    public sealed class ServerLinkedPlayerView
    {
        public string UserId { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public ServerMemberRole Role { get; init; } = ServerMemberRole.User;
        public bool IsSystemAdmin { get; init; }
        public bool IsServerMember { get; init; }
        public bool HasMarketplaceOffer { get; init; }
    }

    public sealed class SalesCounterItem
    {
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public int Count { get; init; }
    }

    public sealed class ServerSalesCounters
    {
        public List<SalesCounterItem> TopDay { get; init; } = new();
        public List<SalesCounterItem> TopMonth { get; init; } = new();
        public List<SalesCounterItem> TopAllTime { get; init; } = new();
    }

    public sealed class GlobalServerSummary
    {
        public int TotalServers { get; init; }
        public int ActiveServers { get; init; }
        public int ActiveOffers { get; init; }
        public int PaidOrders { get; init; }
        public decimal PaidVolumeBtc { get; init; }
    }

    public sealed class GlobalServerRankingItem
    {
        public int ServerId { get; init; }
        public string ServerName { get; init; } = string.Empty;
        public int PaidOrders { get; init; }
        public decimal PaidVolumeBtc { get; init; }
    }

    public enum AddServerMemberResult
    {
        Added,
        Updated,
        UserNotFound,
        ServerNotFound
    }

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IMemoryCache? _cache;
    private readonly LogService? _log;
    private readonly PaymentEventBus? _eventBus;

    private const string CacheKeyServersList = "tibiaservers:list:v1";
    private const string CacheKeyServerCardStats = "tibiaservers:cardstats:v1";
    private static readonly TimeSpan ServerListCacheTtl = TimeSpan.FromSeconds(60);

    public TibiaServerService(AppDbContext db, IWebHostEnvironment env, IMemoryCache? cache = null, LogService? log = null, PaymentEventBus? eventBus = null)
    {
        _db = db;
        _env = env;
        _cache = cache;
        _log = log;
        _eventBus = eventBus;
    }

    public async Task<List<TibiaServer>> GetAllAsync()
    {
        if (_cache != null && _cache.TryGetValue<List<TibiaServer>>(CacheKeyServersList, out var cached) && cached != null)
        {
            return cached;
        }

        var servers = await _db.Servers
            .AsNoTracking()
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync();

        _cache?.Set(CacheKeyServersList, servers, ServerListCacheTtl);
        return servers;
    }

    /// <summary>
    /// Returns the set of server ids where the given user is a ServerAdmin member.
    /// Cheap, per-user; not cached.
    /// </summary>
    public async Task<HashSet<int>> GetServerAdminIdsForUserAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new HashSet<int>();
        }

        var ids = await _db.ServerMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Role == ServerMemberRole.ServerAdmin)
            .Select(m => m.ServerId)
            .ToListAsync();

        return new HashSet<int>(ids);
    }

    public sealed record ServerCardStats(int CompletedSales, int ActiveOffers);

    public async Task<Dictionary<int, ServerCardStats>> GetAllServerCardStatsAsync()
    {
        if (_cache != null && _cache.TryGetValue<Dictionary<int, ServerCardStats>>(CacheKeyServerCardStats, out var cached) && cached != null)
        {
            return cached;
        }

        var salesByServer = await _db.Orders
            .AsNoTracking()
            .Where(o => o.ServerId != null && o.IsPaid)
            .GroupBy(o => o.ServerId!.Value)
            .Select(g => new { ServerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ServerId, x => x.Count);

        var offersByServer = await _db.ItemOffers
            .AsNoTracking()
            .Where(o => o.IsActive)
            .GroupBy(o => o.ServerId)
            .Select(g => new { ServerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ServerId, x => x.Count);

        var allIds = salesByServer.Keys.Union(offersByServer.Keys);
        var result = new Dictionary<int, ServerCardStats>();
        foreach (var id in allIds)
        {
            salesByServer.TryGetValue(id, out var sales);
            offersByServer.TryGetValue(id, out var offers);
            result[id] = new ServerCardStats(sales, offers);
        }

        _cache?.Set(CacheKeyServerCardStats, result, ServerListCacheTtl);
        return result;
    }

    /// <summary>
    /// Invalidates cached server list and aggregated card stats.
    /// Should be called after any mutation that affects the public servers grid.
    /// </summary>
    private void InvalidateServerListCache()
    {
        _cache?.Remove(CacheKeyServersList);
        _cache?.Remove(CacheKeyServerCardStats);
    }

    public async Task<TibiaServer?> GetByIdAsync(int id)
    {
        return await _db.Servers
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<PendingTradeView>> GetPendingTradesAsync(int serverId)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Include(order => order.Payment)
            .Include(order => order.Product)
            .Include(order => order.ItemOffer)
            .Where(order => order.ServerId == serverId && order.Status == PaymentStatus.AguardandoEntregaInGame)
            .OrderBy(order => order.CreatedAt)
            .ToListAsync();

        // Build map of sellerUserId -> in-game player name via ServerMember
        var sellerIds = orders
            .Where(o => o.SellerId != null)
            .Select(o => o.SellerId!)
            .Distinct()
            .ToList();

        var sellerPlayerNames = await _db.ServerMembers
            .AsNoTracking()
            .Where(m => m.ServerId == serverId && sellerIds.Contains(m.UserId) && m.InGamePlayerName != null)
            .ToDictionaryAsync(m => m.UserId, m => m.InGamePlayerName);

        return orders.Select(order => new PendingTradeView
        {
            Id = order.Id,
            OrderId = order.Id,
            ServerId = serverId,
            ProductId = order.ProductId,
            ProductName = order.Product != null ? order.Product.Name : string.Empty,
            Quantity = 1,
            AmountBtc = order.Amount,
            BuyerUserId = order.BuyerId,
            SellerUserId = order.SellerId,
            PlayerName = order.Payment != null ? order.Payment.InGamePlayerName : null,
            PaymentInvoiceId = order.Payment != null ? order.Payment.PaymentId : null,
            CreatedAtUtc = order.CreatedAt,
            Status = order.Status.ToString(),
            ItemKey = order.ItemOffer != null ? order.ItemOffer.ItemKey : null,
            ItemName = order.ItemOffer != null ? order.ItemOffer.ItemName : null,
            SellerPlayerName = order.SellerId != null && sellerPlayerNames.TryGetValue(order.SellerId, out var spn) ? spn : null
        }).ToList();
    }

    public async Task<ConfirmPendingTradeResult> ConfirmPendingTradeAsync(int serverId, int tradeId)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == tradeId && o.ServerId == serverId);

        if (order == null)
        {
            return new ConfirmPendingTradeResult
            {
                Reason = "TradeNotFound"
            };
        }

        if (order.Status != PaymentStatus.AguardandoEntregaInGame)
        {
            return new ConfirmPendingTradeResult
            {
                Reason = "InvalidStatus",
                Order = order
            };
        }

        if (order.BuyerId == null || order.SellerId == null)
        {
            return new ConfirmPendingTradeResult { Reason = "ParticipantDeleted", Order = order };
        }

        order.IsDelivered = true;
        order.DeliveredAt = DateTime.UtcNow;
        order.DeliveryPendingApproval = true;
        order.Status = PaymentStatus.AguardandoRevisaoAdm;

        await _db.SaveChangesAsync();

        _eventBus?.NotifyOrderEnteredReview(order.Id);

        return new ConfirmPendingTradeResult
        {
            Success = true,
            Reason = "Confirmed",
            Order = order
        };
    }

    public async Task<RejectPendingTradeResult> RejectPendingTradeAsync(int serverId, int tradeId, string? rejectReason)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == tradeId && o.ServerId == serverId);

        if (order == null)
        {
            return new RejectPendingTradeResult { Reason = "TradeNotFound" };
        }

        if (order.Status != PaymentStatus.AguardandoEntregaInGame)
        {
            return new RejectPendingTradeResult { Reason = "InvalidStatus", Order = order };
        }

        if (order.BuyerId == null || order.SellerId == null)
        {
            return new RejectPendingTradeResult { Reason = "ParticipantDeleted", Order = order };
        }

        order.Status = PaymentStatus.Disputa;

        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.OrderDisputed,
                AuditEntities.Order,
                order.Id.ToString(),
                $"Servidor rejeitou entrega do pedido #{order.Id} (serverId={serverId}). Razão: {rejectReason ?? "não informada"}. Pedido movido para Disputa.",
                source: "ServerIntegration"
            );

        return new RejectPendingTradeResult { Success = true, Reason = "Rejected", Order = order };
    }

    public async Task<int> CountAsync()
    {
        return await _db.Servers.CountAsync();
    }

    public async Task AddAsync(TibiaServer server, string creatorUserId, string? primaryGameMasterUserId = null, IBrowserFile? logoFile = null)
    {
        if (logoFile != null)
        {
            server.LogoPath = await SaveLogoAsync(logoFile);
        }

        server.CreatedByUserId = creatorUserId;
        server.PrimaryGameMasterUserId = string.IsNullOrWhiteSpace(primaryGameMasterUserId)
            ? null
            : primaryGameMasterUserId.Trim();

        _db.Servers.Add(server);
        await _db.SaveChangesAsync();

        await EnsureMemberAsync(server.Id, creatorUserId, ServerMemberRole.ServerAdmin);
        if (!string.IsNullOrWhiteSpace(primaryGameMasterUserId))
        {
            await EnsureMemberAsync(server.Id, primaryGameMasterUserId, ServerMemberRole.ServerAdmin);
        }

        InvalidateServerListCache();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ServerCreated,
                AuditEntities.Server,
                server.Id.ToString(),
                $"Servidor criado: '{server.Name}' (versao {server.TibiaVersion}).",
                actorUserId: creatorUserId,
                source: AdminAuditSources.Servers,
                metadata: new { server.Id, server.Name, server.TibiaVersion, server.Region, primaryGameMasterUserId });
        }
    }

    public async Task<bool> UpdateAsync(int serverId, TibiaServer updated, string? ownerUserId, string? primaryGameMasterUserId, IBrowserFile? logoFile = null)
    {
        var existing = await _db.Servers.FirstOrDefaultAsync(s => s.Id == serverId);
        if (existing == null)
        {
            return false;
        }

        existing.Name = updated.Name;
        existing.Region = updated.Region;
        existing.TibiaVersion = updated.TibiaVersion;
        existing.WebsiteUrl = updated.WebsiteUrl;
        existing.IsActive = updated.IsActive;

        if (!string.IsNullOrWhiteSpace(ownerUserId))
        {
            existing.CreatedByUserId = ownerUserId.Trim();
        }

        existing.PrimaryGameMasterUserId = string.IsNullOrWhiteSpace(primaryGameMasterUserId)
            ? null
            : primaryGameMasterUserId.Trim();

        if (logoFile != null)
        {
            existing.LogoPath = await SaveLogoAsync(logoFile);
        }

        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(ownerUserId))
        {
            await EnsureMemberAsync(serverId, ownerUserId, ServerMemberRole.ServerAdmin);
        }
        if (!string.IsNullOrWhiteSpace(primaryGameMasterUserId))
        {
            await EnsureMemberAsync(serverId, primaryGameMasterUserId, ServerMemberRole.ServerAdmin);
        }

        InvalidateServerListCache();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ServerUpdated,
                AuditEntities.Server,
                serverId.ToString(),
                $"Servidor atualizado: '{existing.Name}'.",
                actorUserId: ownerUserId,
                source: AdminAuditSources.Servers,
                metadata: new { existing.Id, existing.Name, existing.TibiaVersion, existing.Region, existing.IsActive, primaryGameMasterUserId });
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int serverId)
    {
        var existing = await _db.Servers.FirstOrDefaultAsync(s => s.Id == serverId);
        if (existing == null)
        {
            return false;
        }

        var logoPath = existing.LogoPath;
        var snapshot = new { existing.Id, existing.Name, existing.TibiaVersion };

        _db.Servers.Remove(existing);
        await _db.SaveChangesAsync();

        DeleteLogoIfExists(logoPath);
        InvalidateServerListCache();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ServerDeleted,
                AuditEntities.Server,
                serverId.ToString(),
                $"Servidor removido: '{snapshot.Name}'.",
                source: AdminAuditSources.Servers,
                level: "Warning",
                metadata: snapshot);
        }

        return true;
    }

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    private async Task<string> SaveLogoAsync(IBrowserFile logoFile)
    {
        var ext = Path.GetExtension(logoFile.Name);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
            throw new InvalidOperationException($"Tipo de arquivo não permitido: {ext}");

        if (!AllowedContentTypes.Contains(logoFile.ContentType))
            throw new InvalidOperationException($"Tipo de conteúdo não permitido: {logoFile.ContentType}");

        var uploads = Path.Combine(_env.WebRootPath, "uploads", "servers");
        if (!Directory.Exists(uploads))
        {
            Directory.CreateDirectory(uploads);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploads, fileName);

        await using var stream = File.Create(filePath);
        await logoFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(stream);

        return $"/uploads/servers/{fileName}";
    }

    private void DeleteLogoIfExists(string? logoPath)
    {
        if (string.IsNullOrWhiteSpace(logoPath) || !logoPath.StartsWith("/uploads/servers/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var relative = logoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_env.WebRootPath, relative);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }
    }

    public async Task<string?> TryGetUserIdByEmailAsync(string? email)
    {
        var normalized = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return await _db.Users
            .Where(u => u.Email != null && u.Email.ToLower() == normalized.ToLower())
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<(string? UserName, string? Email)> GetUserContactByIdAsync(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return (null, null);
        }

        var user = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.UserName, u.Email })
            .FirstOrDefaultAsync();

        return user == null ? (null, null) : (user.UserName, user.Email);
    }

    public async Task<List<ServerMemberView>> GetMembersAsync(int serverId)
    {
        var systemAdminIds = await GetSystemAdminUserIdsAsync();

        return await _db.ServerMembers
            .Where(m => m.ServerId == serverId)
            .Include(m => m.User)
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.User.UserName)
            .Select(m => new ServerMemberView
            {
                UserId = m.UserId,
                UserName = m.User.UserName ?? string.Empty,
                Email = m.User.Email ?? string.Empty,
                Role = m.Role,
                IsSystemAdmin = systemAdminIds.Contains(m.UserId)
            })
            .ToListAsync();
    }

    public async Task<List<ServerLinkedPlayerView>> GetLinkedPlayersAsync(int serverId)
    {
        var systemAdminIds = await GetSystemAdminUserIdsAsync();

        var memberRows = await _db.ServerMembers
            .AsNoTracking()
            .Where(m => m.ServerId == serverId)
            .Select(m => new { m.UserId, m.Role })
            .ToListAsync();

        var marketplaceSellerIds = await _db.ItemOffers
            .AsNoTracking()
            .Where(o => o.ServerId == serverId && o.IsActive)
            .Select(o => o.SellerUserId)
            .Distinct()
            .ToListAsync();

        var linkedUserIds = memberRows
            .Select(m => m.UserId)
            .Union(marketplaceSellerIds)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!linkedUserIds.Any())
        {
            return new List<ServerLinkedPlayerView>();
        }

        var users = await _db.Users
            .AsNoTracking()
            .Where(u => linkedUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName, u.Email })
            .ToListAsync();

        var membersByUserId = memberRows
            .GroupBy(m => m.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Role).First().Role, StringComparer.OrdinalIgnoreCase);

        var sellerSet = marketplaceSellerIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return users
            .Select(u =>
            {
                var isMember = membersByUserId.TryGetValue(u.Id, out var role);

                return new ServerLinkedPlayerView
                {
                    UserId = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    Role = isMember ? role : ServerMemberRole.User,
                    IsSystemAdmin = systemAdminIds.Contains(u.Id),
                    IsServerMember = isMember,
                    HasMarketplaceOffer = sellerSet.Contains(u.Id)
                };
            })
            .OrderByDescending(p => p.HasMarketplaceOffer)
            .ThenByDescending(p => p.IsServerMember)
            .ThenBy(p => p.UserName)
            .ToList();
    }

    public async Task<AddServerMemberResult> AddOrUpdateMemberByEmailAsync(int serverId, string? email, ServerMemberRole role)
    {
        var normalized = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return AddServerMemberResult.UserNotFound;
        }

        var serverExists = await _db.Servers.AnyAsync(s => s.Id == serverId);
        if (!serverExists)
        {
            return AddServerMemberResult.ServerNotFound;
        }

        var userId = await TryGetUserIdByEmailAsync(normalized);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return AddServerMemberResult.UserNotFound;
        }

        var existing = await _db.ServerMembers
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId);

        if (existing == null)
        {
            _db.ServerMembers.Add(new ServerMember
            {
                ServerId = serverId,
                UserId = userId,
                Role = role,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            if (_log != null)
                await _log.AuditAsync(
                    AuditEvents.ServerMemberAdded,
                    AuditEntities.Server,
                    serverId.ToString(),
                    $"Membro adicionado ao servidor {serverId}: userId={userId}, role={role}.",
                    source: AdminAuditSources.ServerMembers,
                    metadata: new { serverId, userId, role = role.ToString() });
            return AddServerMemberResult.Added;
        }

        if (existing.Role != role)
        {
            existing.Role = role;
            await _db.SaveChangesAsync();
            if (_log != null)
                await _log.AuditAsync(
                    AuditEvents.ServerMemberUpdated,
                    AuditEntities.Server,
                    serverId.ToString(),
                    $"Role do membro atualizada no servidor {serverId}: userId={userId}, nova role={role}.",
                    source: AdminAuditSources.ServerMembers,
                    metadata: new { serverId, userId, role = role.ToString() });
            return AddServerMemberResult.Updated;
        }

        return AddServerMemberResult.Updated;
    }

    public async Task<bool> RemoveMemberAsync(int serverId, string userId)
    {
        var existing = await _db.ServerMembers
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId);
        if (existing == null)
        {
            return false;
        }

        _db.ServerMembers.Remove(existing);
        await _db.SaveChangesAsync();
        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.ServerMemberRemoved,
                AuditEntities.Server,
                serverId.ToString(),
                $"Membro removido do servidor {serverId}: userId={userId}.",
                source: AdminAuditSources.ServerMembers,
                level: "Warning",
                metadata: new { serverId, userId });
        return true;
    }

    public async Task<List<Product>> GetItemsForServerAsync(int serverId)
    {
        var serverExists = await _db.Servers.AnyAsync(s => s.Id == serverId && s.IsActive);
        if (!serverExists)
        {
            return new List<Product>();
        }

        var linkedProductIds = await _db.ProductServers
            .Where(ps => ps.ServerId == serverId)
            .Select(ps => ps.ProductId)
            .ToListAsync();

        return await _db.Products
            .Include(p => p.User)
            .Where(p => linkedProductIds.Contains(p.Id))
            .Where(p => p.Category == null || (p.Category.ToLower() != "legacy-archived" && p.Category.ToLower() != "deleted-archived"))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<CreateItemOfferResult> CreateItemOfferAsync(CreateItemOfferRequest request)
    {
        if (request.ServerId <= 0)
        {
            return new CreateItemOfferResult { Message = "Servidor invalido." };
        }

        var normalizedItemKey = NormalizeItemKey(request.ItemKey);
        if (string.IsNullOrWhiteSpace(normalizedItemKey))
        {
            return new CreateItemOfferResult { Message = "Item invalido." };
        }

        if (string.IsNullOrWhiteSpace(request.SellerUserId))
        {
            return new CreateItemOfferResult { Message = "Usuario invalido para criar oferta." };
        }

        if (request.Quantity <= 0)
        {
            return new CreateItemOfferResult { Message = "Quantidade deve ser maior que zero." };
        }

        if (request.UnitPrice <= 0)
        {
            return new CreateItemOfferResult { Message = "Preco deve ser maior que zero." };
        }

        var normalizedPricingGateway = NormalizePricingGateway(request.PricingGateway);
        if (string.IsNullOrWhiteSpace(normalizedPricingGateway))
        {
            return new CreateItemOfferResult { Message = "Selecione um gateway de precificacao." };
        }

        var serverExists = await _db.Servers.AnyAsync(s => s.Id == request.ServerId && s.IsActive);
        if (!serverExists)
        {
            return new CreateItemOfferResult { Message = "Servidor nao encontrado ou inativo." };
        }

        var template = await _db.Products
            .AsNoTracking()
            .Where(p => p.Name != null && p.Name.Trim().ToUpper() == normalizedItemKey)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.Name, p.AccentColor })
            .FirstOrDefaultAsync();

        var itemName = string.IsNullOrWhiteSpace(request.ItemName)
            ? (template?.Name ?? normalizedItemKey)
            : request.ItemName.Trim();

        var offer = new ItemOffer
        {
            ServerId = request.ServerId,
            ItemKey = normalizedItemKey,
            ItemName = itemName,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            PricingGateway = normalizedPricingGateway,
            UseSiteIntermediary = true,
            SellerUserId = request.SellerUserId,
            AccentColor = template?.AccentColor,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.ItemOffers.Add(offer);
        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.ItemOfferCreated,
                AuditEntities.ItemOffer,
                offer.Id.ToString(),
                $"Oferta criada: '{offer.ItemName}' x{offer.Quantity} por {offer.UnitPrice} ({offer.PricingGateway}) no servidor {offer.ServerId}.",
                actorUserId: offer.SellerUserId,
                source: AdminAuditSources.ItemOffers,
                metadata: new { offer.Id, offer.ServerId, offer.ItemKey, offer.Quantity, offer.UnitPrice, offer.PricingGateway });

        return new CreateItemOfferResult
        {
            Success = true,
            Message = "Oferta criada com sucesso.",
            Offer = offer
        };
    }

    public async Task<List<ItemOfferView>> GetActiveItemOffersAsync(int serverId, string itemKey, bool sortByPrice = false)
    {
        var normalizedItemKey = NormalizeItemKey(itemKey);

        var baseQuery = _db.ItemOffers
            .AsNoTracking()
            .Include(o => o.SellerUser)
            .Where(o => o.ServerId == serverId
                && o.ItemKey == normalizedItemKey
                && o.IsActive);

        var orderedQuery = sortByPrice
            ? baseQuery.OrderBy(o => o.UnitPrice).ThenBy(o => o.CreatedAt)
            : baseQuery.OrderByDescending(o => o.CreatedAt);

        return await orderedQuery
            .Select(o => new ItemOfferView
            {
                OfferId = o.Id,
                ProductId = _db.Products
                    .Where(p => p.Name != null && p.Name.Trim().ToUpper() == o.ItemKey)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Id)
                    .FirstOrDefault(),
                ItemKey = o.ItemKey,
                ItemName = o.ItemName,
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                PricingGateway = o.PricingGateway,
                SellerUserId = o.SellerUserId,
                SellerUserName = string.IsNullOrWhiteSpace(o.SellerUser!.UserName)
                    ? o.SellerUserId
                    : o.SellerUser.UserName!,
                AccentColor = o.AccentColor,
                CreatedAt = o.CreatedAt,
                FailedDeliveryCount = _db.ServerMembers
                    .Where(m => m.ServerId == serverId && m.UserId == o.SellerUserId)
                    .Select(m => m.FailedDeliveryCount)
                    .FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<int> DeactivateOwnItemOffersAsync(int serverId, string itemKey, string sellerUserId)
    {
        var normalizedItemKey = NormalizeItemKey(itemKey);
        if (serverId <= 0 || string.IsNullOrWhiteSpace(normalizedItemKey) || string.IsNullOrWhiteSpace(sellerUserId))
        {
            return 0;
        }

        var offers = await _db.ItemOffers
            .Where(o => o.ServerId == serverId
                && o.ItemKey == normalizedItemKey
                && o.SellerUserId == sellerUserId
                && o.IsActive)
            .ToListAsync();

        if (!offers.Any())
        {
            return 0;
        }

        foreach (var offer in offers)
        {
            offer.IsActive = false;
        }

        await _db.SaveChangesAsync();

        if (_log != null)
        {
            foreach (var offer in offers)
                await _log.AuditAsync(
                    AuditEvents.ItemOfferCancelled,
                    AuditEntities.ItemOffer,
                    offer.Id.ToString(),
                    $"Oferta cancelada: '{offer.ItemName}' x{offer.Quantity} no servidor {offer.ServerId}.",
                    actorUserId: sellerUserId,
                    source: AdminAuditSources.ItemOffers);
        }

        return offers.Count;
    }

    public async Task<Dictionary<string, int>> GetActiveOfferCountsByItemAsync(int serverId)
    {
        return await _db.ItemOffers
            .AsNoTracking()
            .Where(o => o.ServerId == serverId && o.IsActive)
            .GroupBy(o => o.ItemKey)
            .Select(g => new { ItemKey = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ItemKey, x => x.Count);
    }

    public async Task<ServerSalesCounters> GetSalesCountersAsync(int serverId, int take = 5)
    {
        var now = DateTime.UtcNow;
        var startDay = now.Date;
        var startMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var paidOrders = _db.Orders
            .AsNoTracking()
            .Include(o => o.Product)
            .Where(o => o.ServerId == serverId && o.IsPaid);

        return new ServerSalesCounters
        {
            TopDay = await BuildTopAsync(paidOrders.Where(o => o.CreatedAt >= startDay), take),
            TopMonth = await BuildTopAsync(paidOrders.Where(o => o.CreatedAt >= startMonth), take),
            TopAllTime = await BuildTopAsync(paidOrders, take)
        };
    }

    public async Task<Dictionary<int, int>> GetPaidOrderCountsByProductAsync(int serverId)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.ServerId == serverId && o.IsPaid)
            .GroupBy(o => o.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count);
    }

    public async Task<Dictionary<string, int>> GetPaidOrderCountsBySellerForItemAsync(int serverId, string itemKey)
    {
        var normalizedItemKey = NormalizeItemKey(itemKey);

        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.ServerId == serverId && o.IsPaid && o.SellerId != null && o.Product != null)
            .Where(o => o.Product!.Name != null && o.Product.Name.Trim().ToUpper() == normalizedItemKey)
            .GroupBy(o => o.SellerId!)
            .Select(g => new { SellerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SellerId, x => x.Count);
    }

    public async Task<List<SellerRecommendationView>> GetSellerRecommendationsForItemAsync(int serverId, string itemKey)
    {
        var normalizedItemKey = NormalizeItemKey(itemKey);

        return await _db.SellerRecommendations
            .AsNoTracking()
            .Include(r => r.RecommenderUser)
            .Where(r => r.ServerId == serverId && r.ItemKey == normalizedItemKey)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new SellerRecommendationView
            {
                SellerUserId = r.SellerUserId,
                RecommenderUserId = r.RecommenderUserId,
                RecommenderUserName = string.IsNullOrWhiteSpace(r.RecommenderUser!.UserName)
                    ? r.RecommenderUserId
                    : r.RecommenderUser.UserName!
            })
            .ToListAsync();
    }

    public async Task ToggleSellerRecommendationAsync(int serverId, string itemKey, string sellerUserId, string recommenderUserId)
    {
        if (string.IsNullOrWhiteSpace(sellerUserId) || string.IsNullOrWhiteSpace(recommenderUserId))
        {
            return;
        }

        if (string.Equals(sellerUserId, recommenderUserId, StringComparison.Ordinal))
        {
            return;
        }

        var normalizedItemKey = NormalizeItemKey(itemKey);

        var existing = await _db.SellerRecommendations.FirstOrDefaultAsync(r =>
            r.ServerId == serverId
            && r.ItemKey == normalizedItemKey
            && r.SellerUserId == sellerUserId
            && r.RecommenderUserId == recommenderUserId);

        if (existing != null)
        {
            _db.SellerRecommendations.Remove(existing);
        }
        else
        {
            _db.SellerRecommendations.Add(new SellerRecommendation
            {
                ServerId = serverId,
                ItemKey = normalizedItemKey,
                SellerUserId = sellerUserId,
                RecommenderUserId = recommenderUserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<GlobalServerSummary> GetGlobalSummaryAsync()
    {
        var totalServers = await _db.Servers.CountAsync();
        var activeServers = await _db.Servers.CountAsync(s => s.IsActive);
        var activeOffers = await _db.Products
            .CountAsync(p => p.Category == null || (p.Category.ToLower() != "legacy-archived" && p.Category.ToLower() != "deleted-archived"));

        var paidOrdersQuery = _db.Orders
            .AsNoTracking()
            .Where(o => o.IsPaid);

        var paidOrders = await paidOrdersQuery.CountAsync();
        var paidVolume = await paidOrdersQuery.SumAsync(o => (decimal?)o.Amount) ?? 0m;

        return new GlobalServerSummary
        {
            TotalServers = totalServers,
            ActiveServers = activeServers,
            ActiveOffers = activeOffers,
            PaidOrders = paidOrders,
            PaidVolumeBtc = paidVolume
        };
    }

    public async Task<List<GlobalServerRankingItem>> GetTopServersBySalesAsync(int take = 5)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.IsPaid && o.ServerId.HasValue)
            .Join(
                _db.Servers.AsNoTracking(),
                o => o.ServerId!.Value,
                s => s.Id,
                (o, s) => new { Order = o, Server = s })
            .GroupBy(x => new { x.Server.Id, x.Server.Name })
            .Select(g => new GlobalServerRankingItem
            {
                ServerId = g.Key.Id,
                ServerName = g.Key.Name,
                PaidOrders = g.Count(),
                PaidVolumeBtc = g.Sum(x => x.Order.Amount)
            })
            .OrderByDescending(x => x.PaidOrders)
            .ThenByDescending(x => x.PaidVolumeBtc)
            .ThenBy(x => x.ServerName)
            .Take(take)
            .ToListAsync();
    }

    private static async Task<List<SalesCounterItem>> BuildTopAsync(IQueryable<OrderModel> query, int take)
    {
        return await query
            .GroupBy(o => new { o.ProductId, ProductName = o.Product != null ? o.Product.Name : "Item removido" })
            .Select(g => new SalesCounterItem
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.ProductName)
            .Take(take)
            .ToListAsync();
    }

    private static string NormalizeItemKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return name.Trim().ToUpperInvariant();
    }

    private static string? NormalizePricingGateway(string? gateway)
    {
        if (string.IsNullOrWhiteSpace(gateway))
        {
            return null;
        }

        return gateway.Trim();
    }

    public Task<HashSet<string>> GetSiteAdminUserIdsAsync()
        => GetSystemAdminUserIdsAsync();

    private async Task<HashSet<string>> GetSystemAdminUserIdsAsync()
    {
        var adminRoleId = await _db.Roles
            .Where(r => r.Name != null && r.Name.ToLower() == "admin")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(adminRoleId))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var userIds = await _db.UserRoles
            .Where(ur => ur.RoleId == adminRoleId)
            .Select(ur => ur.UserId)
            .ToListAsync();

        return userIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private async Task EnsureMemberAsync(int serverId, string userId, ServerMemberRole role)
    {
        var existing = await _db.ServerMembers
            .FirstOrDefaultAsync(m => m.ServerId == serverId && m.UserId == userId);

        if (existing == null)
        {
            _db.ServerMembers.Add(new ServerMember
            {
                ServerId = serverId,
                UserId = userId,
                Role = role,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return;
        }

        if (existing.Role != role)
        {
            existing.Role = role;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> CanManageServerAsync(int serverId, string? userId, bool isSiteAdmin)
    {
        if (isSiteAdmin)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            return false;
        }

        return await _db.ServerMembers.AnyAsync(m =>
            m.ServerId == serverId &&
            m.UserId == userId &&
            m.Role == ServerMemberRole.ServerAdmin);
    }

    public Task<bool> CanManageServerCatalogAsync(int serverId, string? userId, bool isSiteAdmin)
        => CanManageServerAsync(serverId, userId, isSiteAdmin);

    public async Task<List<int>> GetLinkedProductIdsAsync(int serverId)
    {
        return await _db.ProductServers
            .Where(ps => ps.ServerId == serverId)
            .Select(ps => ps.ProductId)
            .ToListAsync();
    }

    public async Task LinkProductsAsync(int serverId, IEnumerable<int> productIds)
    {
        var existingIds = await _db.ProductServers
            .Where(ps => ps.ServerId == serverId)
            .Select(ps => ps.ProductId)
            .ToListAsync();

        var existingSet = existingIds.ToHashSet();
        var targetSet = productIds.ToHashSet();

        var toRemove = await _db.ProductServers
            .Where(ps => ps.ServerId == serverId && !targetSet.Contains(ps.ProductId))
            .ToListAsync();
        _db.ProductServers.RemoveRange(toRemove);

        var toAdd = targetSet.Except(existingSet)
            .Select(pid => new ProductServer { ServerId = serverId, ProductId = pid });
        _db.ProductServers.AddRange(toAdd);

        await _db.SaveChangesAsync();
    }
}
