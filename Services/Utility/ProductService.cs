using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Confirmai.Models;
using Confirmai.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Authorization;
using Npgsql;
using System.Globalization;


namespace Confirmai.Services.Utility;

[Authorize]
public class ProductService
{
    public enum ProductDeleteResult
    {
        Deleted,
        NotFound
    }

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IWebHostEnvironment _env;
    private readonly LogService? _log;

    public ProductService(IDbContextFactory<AppDbContext> dbFactory, IWebHostEnvironment env, LogService? log = null)
    {
        _dbFactory = dbFactory;
        _env = env;
        _log = log;
    }

    public async Task<List<Product>> GetAllAsync()
    {
        await using var context = _dbFactory.CreateDbContext();
        return await context.Products
            .Include(p => p.User)
            .Where(p => p.Category == null || (p.Category.ToLower() != "legacy-archived" && p.Category.ToLower() != "deleted-archived"))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var context = _dbFactory.CreateDbContext();
        return await context.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Product product, IBrowserFile? imageFile, IEnumerable<int>? serverIds = null)
    {
        await using var context = _dbFactory.CreateDbContext();
        product.RequiresDelivery = false;
        product.AccentColor = NormalizeAccentColor(product.AccentColor);

        if (imageFile != null)
        {
            var imagePath = await SaveImageAsync(imageFile);
            product.ImagePath = imagePath;
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ProductCreated,
                AuditEntities.Product,
                product.Id.ToString(),
                $"Produto criado: '{product.Name}' (R$ {product.Price:0.##}).",
                actorUserId: product.UserId,
                source: AdminAuditSources.Products,
                metadata: new { product.Id, product.Name, product.Price, product.Category });
        }
    }

    public async Task UpdateAsync(Product product, IBrowserFile? imageFile, IEnumerable<int>? serverIds = null)
    {
        await using var context = _dbFactory.CreateDbContext();
        var existing = await context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        if (existing == null)
        {
            return;
        }

        existing.Name = product.Name;
        existing.Description = product.Description;
        existing.Price = product.Price;
        existing.Weight = product.Weight;
        existing.Attack = product.Attack;
        existing.Defense = product.Defense;
        existing.Armor = product.Armor;
        existing.DropSources = product.DropSources;
        existing.ShortDescription = product.ShortDescription;
        existing.Category = product.Category;
        existing.PricingGateway = product.PricingGateway;
        existing.AccentColor = NormalizeAccentColor(product.AccentColor);
        existing.RequiresDelivery = false;

        if (imageFile != null)
        {
            var imagePath = await SaveImageAsync(imageFile);
            existing.ImagePath = imagePath;
        }

        await context.SaveChangesAsync();

        if (_log != null)
        {
            await _log.AuditAsync(
                AuditEvents.ProductUpdated,
                AuditEntities.Product,
                existing.Id.ToString(),
                $"Produto atualizado: '{existing.Name}'.",
                actorUserId: existing.UserId,
                source: AdminAuditSources.Products,
                metadata: new { existing.Id, existing.Name, existing.Price, existing.Category });
        }
    }

    public async Task<ProductDeleteResult> DeleteAsync(int id)
    {
        await using var context = _dbFactory.CreateDbContext();
        var product = await context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return ProductDeleteResult.NotFound;
        }

        context.Products.Remove(product);

        try
        {
            await context.SaveChangesAsync();
            if (_log != null)
            {
                await _log.AuditAsync(
                    AuditEvents.ProductDeleted,
                    AuditEntities.Product,
                    product.Id.ToString(),
                    $"Produto removido: '{product.Name}'.",
                    actorUserId: product.UserId,
                    source: AdminAuditSources.Products,
                    level: "Warning",
                    metadata: new { product.Id, product.Name });
            }
            return ProductDeleteResult.Deleted;
        }
        catch (DbUpdateException)
        {
            context.Entry(product).State = EntityState.Unchanged;
            ArchiveProduct(product);
            await context.SaveChangesAsync();
            if (_log != null)
            {
                await _log.AuditAsync(
                    AuditEvents.ProductArchived,
                    AuditEntities.Product,
                    product.Id.ToString(),
                    $"Produto arquivado (em uso): '{product.Name}'.",
                    actorUserId: product.UserId,
                    source: AdminAuditSources.Products,
                    level: "Warning",
                    metadata: new { product.Id, product.Name });
            }
            return ProductDeleteResult.Deleted;
        }
    }

    private static void ArchiveProduct(Product product)
    {
        product.Category = "deleted-archived";
        product.RequiresDelivery = false;
        if (!product.Name.StartsWith("[ARQUIVADO] ", StringComparison.OrdinalIgnoreCase))
        {
            product.Name = $"[ARQUIVADO] {product.Name}";
        }
    }

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private async Task<string> SaveImageAsync(IBrowserFile imageFile)
    {
        var ext = Path.GetExtension(imageFile.Name);
        if (string.IsNullOrEmpty(ext) || !AllowedImageExtensions.Contains(ext))
            throw new InvalidOperationException($"Tipo de arquivo n�o permitido: {ext}");

        var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };
        if (!allowedContentTypes.Contains(imageFile.ContentType))
            throw new InvalidOperationException($"Tipo de conte�do n�o permitido: {imageFile.ContentType}");

        var uploads = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploads, fileName);

        await using var stream = File.Create(filePath);
        await imageFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(stream); // 5MB max

        return $"/uploads/{fileName}";
    }

    public async Task<int> GetProductsCountAsync()
    {
        await using var context = _dbFactory.CreateDbContext();
        return await context.Products
            .Where(p => p.Category == null || (p.Category.ToLower() != "legacy-archived" && p.Category.ToLower() != "deleted-archived"))
            .CountAsync();
    }

    public async Task<List<Product>> GetAllExceptUserAsync(string userId)
    {
        await using var context = _dbFactory.CreateDbContext();
        return await context.Products
            .Where(p => p.Category == null || (p.Category.ToLower() != "legacy-archived" && p.Category.ToLower() != "deleted-archived"))
            .Where(p => p.UserId != userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Product>> GetByUserIdAsync(string userId)
    {
        await using var context = _dbFactory.CreateDbContext();
        return await context.Products
            .Where(p => p.Category == null || (p.Category.ToLower() != "legacy-archived" && p.Category.ToLower() != "deleted-archived"))
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    private static string? NormalizeAccentColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var normalized = color.Trim();
        if (normalized.StartsWith('#'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length != 6)
        {
            return null;
        }

        if (!int.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return null;
        }

        return $"#{normalized.ToUpperInvariant()}";
    }
}
