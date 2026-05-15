using System.Globalization;
using System.Security.Claims;
using System.Text;
using Confirmai.Configuration;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

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

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly LogService _logService;

    public AdminSecurityPolicyService(AppDbContext db, IWebHostEnvironment environment, LogService logService)
    {
        _db = db;
        _environment = environment;
        _logService = logService;
    }

    public async Task<RuntimeSecurityPolicy> GetRuntimePolicyAsync()
    {
        var defaults = SecurityPolicyDefaults.Create(_environment.IsDevelopment());
        var settings = await _db.AppSettings
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

        await UpsertAsync(RequireConfirmedEmailKey, policy.RequireConfirmedEmail ? "true" : "false");
        await UpsertAsync(LockoutMaxAttemptsKey, policy.LockoutMaxFailedAccessAttempts.ToString(CultureInfo.InvariantCulture));
        await UpsertAsync(LockoutMinutesKey, policy.LockoutMinutes.ToString(CultureInfo.InvariantCulture));

        await _db.SaveChangesAsync();

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

    private async Task UpsertAsync(string key, string value)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value });
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
            throw new UnauthorizedAccessException("Apenas administradores podem alterar a política de segurança.");
        }
    }
}


