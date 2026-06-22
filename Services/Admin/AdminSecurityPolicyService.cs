using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services.Admin;

public sealed record RuntimeSecurityPolicy(
    bool RequireConfirmedEmail,
    int LockoutMaxFailedAccessAttempts,
    int LockoutMinutes);

public class AdminSecurityPolicyService
{
    public const string RequireConfirmedEmailKey = "Security.RequireConfirmedEmail";
    public const string LockoutMaxAttemptsKey = "Security.LockoutMaxFailedAccessAttempts";
    public const string LockoutMinutesKey = "Security.LockoutMinutes";

    private const int MinLockoutMaxAttempts = 1;
    private const int MaxLockoutMaxAttempts = 20;
    private const int MinLockoutMinutes = 1;
    private const int MaxLockoutMinutes = 1440;

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly IWebHostEnvironment _environment;
    private readonly LogService _logService;

    public AdminSecurityPolicyService(IDbContextFactory<AppDbContext> dbFactory, IWebHostEnvironment environment, LogService logService)
    {
        _dbFactory = dbFactory;
        _environment = environment;
        _logService = logService;
    }

    public async Task<RuntimeSecurityPolicy> GetRuntimePolicyAsync()
    {
        await using var db = _dbFactory.CreateDbContext();
        var defaults = SecurityPolicyDefaults.Create(_environment.IsDevelopment());
        var settings = await db.AppSettings
            .AsNoTracking()
            .Where(s => s.Key == RequireConfirmedEmailKey || s.Key == LockoutMaxAttemptsKey || s.Key == LockoutMinutesKey)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var requireConfirmedEmail = _environment.IsDevelopment()
            ? false
            : ParseBool(settings, RequireConfirmedEmailKey, defaults.RequireConfirmedEmail);
        var lockoutMaxAttempts = ParseInt(settings, LockoutMaxAttemptsKey, defaults.LockoutMaxFailedAccessAttempts, MinLockoutMaxAttempts, MaxLockoutMaxAttempts);
        var lockoutMinutes = ParseInt(settings, LockoutMinutesKey, defaults.LockoutMinutes, MinLockoutMinutes, MaxLockoutMinutes);

        return new RuntimeSecurityPolicy(requireConfirmedEmail, lockoutMaxAttempts, lockoutMinutes);
    }

    public Task<RuntimeSecurityPolicy> GetRuntimePolicyForAdminAsync(ClaimsPrincipal? user)
    {
        EnsureAdmin(user);
        return GetRuntimePolicyAsync();
    }

    public async Task<bool> SetRuntimePolicyForAdminAsync(ClaimsPrincipal? user, RuntimeSecurityPolicy policy)
    {
        EnsureAdmin(user);
        var currentPolicy = await GetRuntimePolicyAsync();

        if (policy.LockoutMaxFailedAccessAttempts < MinLockoutMaxAttempts || policy.LockoutMaxFailedAccessAttempts > MaxLockoutMaxAttempts)
        {
            return false;
        }

        if (policy.LockoutMinutes < MinLockoutMinutes || policy.LockoutMinutes > MaxLockoutMinutes)
        {
            return false;
        }

        await using var db = _dbFactory.CreateDbContext();
        await UpsertAsync(db, RequireConfirmedEmailKey, policy.RequireConfirmedEmail ? "true" : "false");
        await UpsertAsync(db, LockoutMaxAttemptsKey, policy.LockoutMaxFailedAccessAttempts.ToString(CultureInfo.InvariantCulture));
        await UpsertAsync(db, LockoutMinutesKey, policy.LockoutMinutes.ToString(CultureInfo.InvariantCulture));

        await db.SaveChangesAsync();

        var adminUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        await _logService.LogAsync(
            message: BuildAuditMessage(currentPolicy, policy),
            source: AdminAuditSources.SecurityPolicy,
            level: AdminAuditLevels.Success,
            userId: adminUserId);

        return true;
    }

    private static string BuildAuditMessage(RuntimeSecurityPolicy previous, RuntimeSecurityPolicy current)
    {
        var builder = new StringBuilder("Security policy updated.");
        builder.Append(" RequireConfirmedEmail: ")
            .Append(previous.RequireConfirmedEmail)
            .Append(" -> ")
            .Append(current.RequireConfirmedEmail)
            .Append(';');
        builder.Append(" LockoutMaxFailedAccessAttempts: ")
            .Append(previous.LockoutMaxFailedAccessAttempts)
            .Append(" -> ")
            .Append(current.LockoutMaxFailedAccessAttempts)
            .Append(';');
        builder.Append(" LockoutMinutes: ")
            .Append(previous.LockoutMinutes)
            .Append(" -> ")
            .Append(current.LockoutMinutes)
            .Append('.');

        return builder.ToString();
    }

    private async Task UpsertAsync(AppDbContext db, string key, string value)
    {
        var setting = await db.AppSettings.FindAsync(key);
        if (setting is null)
        {
            db.AppSettings.Add(new AppSetting { Key = key, Value = value });
            return;
        }

        setting.Value = value;
    }

    private static bool ParseBool(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
    {
        if (!settings.TryGetValue(key, out var raw))
        {
            return fallback;
        }

        return bool.TryParse(raw, out var parsed) ? parsed : fallback;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> settings, string key, int fallback, int min, int max)
    {
        if (!settings.TryGetValue(key, out var raw) || !int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return fallback;
        }

        return Math.Clamp(parsed, min, max);
    }

    private static void EnsureAdmin(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("admin"))
        {
            throw new UnauthorizedAccessException("Apenas administradores podem alterar a pol�tica de seguran�a.");
        }
    }
}


