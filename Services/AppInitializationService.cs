using Confirmai.Data;
using Confirmai.Configuration;
using Confirmai.Enums;
using Confirmai.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Confirmai.Services
{
    public class AppInitializationService
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly AppDbContext _db;
        private readonly ILogger<AppInitializationService> _logger;

        public AppInitializationService(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            AppDbContext db,
            ILogger<AppInitializationService> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _configuration = configuration;
            _environment = environment;
            _db = db;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAndAdminAsync();
            await SeedTestUsersAsync();
            await SeedGatewaysAsync();
            await SeedAdminSettingsAsync();
            await CleanupLegacyForkDataAsync();
            await EnsureCrownHelmetCategoryAsync();
        }

        private sealed record SeedUserSpec(
            string Email,
            string FullName,
            bool IsSystemAdmin);

        private async Task SeedRolesAndAdminAsync()
        {
            string[] roles = new[] { "admin", "user", "venue_manager" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }

            var adminEmail = _configuration["AdminSeed:Email"] ?? "god@god";
            var adminPassword = _configuration["AdminSeed:Password"];
            var adminFullName = _configuration["AdminSeed:FullName"] ?? "Administrator";
            var syncAdminPassword = _configuration.GetValue<bool?>("AdminSeed:SyncPassword") ?? _environment.IsDevelopment();

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null && !string.IsNullOrWhiteSpace(adminPassword))
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = adminFullName
                };

                var createAdminResult = await _userManager.CreateAsync(adminUser, adminPassword);
                if (!createAdminResult.Succeeded)
                {
                    var errors = string.Join("; ", createAdminResult.Errors.Select(e => e.Description));
                    _logger.LogError("Falha ao criar usuário admin seed ({AdminEmail}): {Errors}", adminEmail, errors);
                    adminUser = null;
                }
                else
                {
                    _logger.LogInformation("Usuário admin seed criado: {AdminEmail}", adminEmail);
                }
            }

            if (adminUser != null && !await _userManager.IsInRoleAsync(adminUser, "admin"))
            {
                await _userManager.AddToRoleAsync(adminUser, "admin");
                _logger.LogInformation("Usuário {AdminEmail} promovido a admin.", adminEmail);
            }

            if (adminUser != null && !string.IsNullOrWhiteSpace(adminPassword) && syncAdminPassword)
            {
                var isExpectedPassword = await _userManager.CheckPasswordAsync(adminUser, adminPassword);
                if (!isExpectedPassword)
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
                    var resetPasswordResult = await _userManager.ResetPasswordAsync(adminUser, resetToken, adminPassword);

                    if (!resetPasswordResult.Succeeded)
                    {
                        var errors = string.Join("; ", resetPasswordResult.Errors.Select(e => e.Description));
                        _logger.LogError("Falha ao sincronizar senha do admin seed ({AdminEmail}): {Errors}", adminEmail, errors);
                    }
                    else
                    {
                        _logger.LogInformation("Senha do admin seed sincronizada para {AdminEmail}.", adminEmail);
                    }
                }
            }
        }

        private async Task SeedGatewaysAsync()
        {
            var defaultGateways = new[]
            {
                new { Name = "Pix", Enabled = true },
                new { Name = "Testnet", Enabled = true },
                new { Name = "BTCPayServer", Enabled = false },
            };

            var existingGatewayNames = _db.Gateways
                .Select(g => g.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var gateway in defaultGateways)
            {
                if (!existingGatewayNames.Contains(gateway.Name))
                {
                    _db.Gateways.Add(new GatewayInfo
                    {
                        Name = gateway.Name,
                        Enabled = gateway.Enabled
                    });
                }
            }

            await _db.SaveChangesAsync();
        }

        private async Task SeedTestUsersAsync()
        {
            var enabled = _configuration.GetValue<bool?>("SeedTestUsers:Enabled") ?? _environment.IsDevelopment();
            if (!enabled)
            {
                return;
            }

            var defaultPassword = _configuration["SeedTestUsers:Password"] ?? "Test@12345";

            await EnsureFakeTestUserAccessAsync("gm@teste.com", defaultPassword);
            await EnsureFakeTestUserAccessAsync("gm2@teste.com", defaultPassword);

            var seedUsers = new[]
            {
                new SeedUserSpec("admin.teste@otserv.local", "Admin Sistema Teste", true),
                new SeedUserSpec("adm.server@otserv.local", "Admin Servidor Teste", false),
                new SeedUserSpec("player1@otserv.local", "Player Teste 1", false),
                new SeedUserSpec("player2@otserv.local", "Player Teste 2", false)
            };

            foreach (var spec in seedUsers)
            {
                await EnsureSeedUserAsync(spec, defaultPassword);
            }
        }

        private async Task EnsureFakeTestUserAccessAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = "GM Teste"
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var createErrors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao criar usuário fake de teste {Email}: {Errors}", email, createErrors);
                    return;
                }

                if (!await _userManager.IsInRoleAsync(user, "user"))
                {
                    await _userManager.AddToRoleAsync(user, "user");
                }

                _logger.LogInformation("Usuário fake de teste criado com e-mail confirmado: {Email}", email);
                return;
            }

            var requiresUpdate = false;
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                requiresUpdate = true;
            }

            if (requiresUpdate)
            {
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    var updateErrors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao confirmar e-mail fake do usuário {Email}: {Errors}", email, updateErrors);
                    return;
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "user"))
            {
                await _userManager.AddToRoleAsync(user, "user");
            }

            var hasExpectedPassword = await _userManager.CheckPasswordAsync(user, password);
            if (!hasExpectedPassword)
            {
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, password);

                if (!resetResult.Succeeded)
                {
                    var resetErrors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao sincronizar senha fake do usuário {Email}: {Errors}", email, resetErrors);
                    return;
                }
            }

            _logger.LogInformation("Acesso fake do usuário de teste sincronizado: {Email}", email);
        }

        private async Task<ApplicationUser?> EnsureSeedUserAsync(SeedUserSpec spec, string password)
        {
            var user = await _userManager.FindByEmailAsync(spec.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = spec.Email,
                    Email = spec.Email,
                    EmailConfirmed = true,
                    FullName = spec.FullName
                };

                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Falha ao criar usuario seed {Email}: {Errors}", spec.Email, errors);
                    return null;
                }
            }

            if (!await _userManager.IsInRoleAsync(user, "user"))
            {
                await _userManager.AddToRoleAsync(user, "user");
            }

            if (spec.IsSystemAdmin && !await _userManager.IsInRoleAsync(user, "admin"))
            {
                await _userManager.AddToRoleAsync(user, "admin");
            }

            if (!spec.IsSystemAdmin && await _userManager.IsInRoleAsync(user, "admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "admin");
            }

            return user;
        }

        private async Task SeedAdminSettingsAsync()
        {
            var hasOperationFee = await _db.AppSettings
                .AnyAsync(s => s.Key == AdminSettingsService.OperationFeePercentKey);

            if (!hasOperationFee)
            {
                _db.AppSettings.Add(new AppSetting
                {
                    Key = AdminSettingsService.OperationFeePercentKey,
                    Value = AdminSettingsService.DefaultOperationFeePercent
                        .ToString("0.##", CultureInfo.InvariantCulture)
                });

                await _db.SaveChangesAsync();
            }

            var defaults = SecurityPolicyDefaults.Create(_environment.IsDevelopment());

            await EnsureAppSettingAsync(AdminSecurityPolicyService.RequireConfirmedEmailKey, defaults.RequireConfirmedEmail ? "true" : "false");
            await EnsureAppSettingAsync(AdminSecurityPolicyService.LockoutMaxAttemptsKey, defaults.LockoutMaxFailedAccessAttempts.ToString(CultureInfo.InvariantCulture));
            await EnsureAppSettingAsync(AdminSecurityPolicyService.LockoutMinutesKey, defaults.LockoutMinutes.ToString(CultureInfo.InvariantCulture));
            await EnsureAppSettingAsync(AdminSettingsService.LuaDeliveryEnabledKey, "false");

            await _db.SaveChangesAsync();
        }

        private async Task EnsureAppSettingAsync(string key, string value)
        {
            var exists = await _db.AppSettings.AnyAsync(s => s.Key == key);
            if (exists)
            {
                return;
            }

            _db.AppSettings.Add(new AppSetting
            {
                Key = key,
                Value = value
            });
        }

        /// <summary>Cleans up legacy seed data inherited from the CryptoMarket fork.</summary>
        private async Task CleanupLegacyForkDataAsync()
        {
            // Key kept as-is for backward compatibility with existing DB records
            var cleanupFlag = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "LegacyCleanup:CryptoMarketDone");
            if (cleanupFlag?.Value == "true")
            {
                return;
            }

            var legacyProducts = await _db.Products
                .Where(p =>
                    (p.Name != null && (
                        EF.Functions.ILike(p.Name, "%seed%") ||
                        EF.Functions.ILike(p.Name, "%bitcoin%") ||
                        EF.Functions.ILike(p.Name, "%ethereum%") ||
                        EF.Functions.ILike(p.Name, "%solana%") ||
                        EF.Functions.ILike(p.Name, "%crypto%")))
                    ||
                    (p.Description != null && EF.Functions.ILike(p.Description, "%crypto%")))
                .ToListAsync();

            if (legacyProducts.Any())
            {
                var legacyIds = legacyProducts.Select(p => p.Id).ToList();

                var referencedLegacyIds = await _db.Orders
                    .Where(o => legacyIds.Contains(o.ProductId))
                    .Select(o => o.ProductId)
                    .Distinct()
                    .ToListAsync();

                var deletableProducts = legacyProducts
                    .Where(p => !referencedLegacyIds.Contains(p.Id))
                    .ToList();

                if (deletableProducts.Any())
                {
                    _db.Products.RemoveRange(deletableProducts);
                }

                var archivedProducts = legacyProducts
                    .Where(p => referencedLegacyIds.Contains(p.Id))
                    .ToList();

                foreach (var archivedProduct in archivedProducts)
                {
                    if (!string.Equals(archivedProduct.Category, "legacy-archived", StringComparison.OrdinalIgnoreCase))
                    {
                        archivedProduct.Category = "legacy-archived";
                    }

                    if (!archivedProduct.Name.StartsWith("[ARCHIVED]", StringComparison.OrdinalIgnoreCase))
                    {
                        archivedProduct.Name = $"[ARCHIVED] Item legado #{archivedProduct.Id}";
                    }
                }
            }

            if (cleanupFlag == null)
            {
                _db.AppSettings.Add(new AppSetting
                {
                    Key = "LegacyCleanup:CryptoMarketDone",
                    Value = "true"
                });
            }
            else
            {
                cleanupFlag.Value = "true";
            }

            await _db.SaveChangesAsync();
        }

        private async Task EnsureCrownHelmetCategoryAsync()
        {
            var crownHelmetItems = await _db.Products
                .Where(p => p.Name != null && EF.Functions.ILike(p.Name, "%crown%helmet%"))
                .ToListAsync();

            if (!crownHelmetItems.Any())
            {
                return;
            }

            var hasChanges = false;
            foreach (var item in crownHelmetItems)
            {
                if (!string.Equals(item.Category, "helmet", StringComparison.OrdinalIgnoreCase))
                {
                    item.Category = "helmet";
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _db.SaveChangesAsync();
            }
        }
    }
}

