using System.Globalization;
using System.Security.Claims;
using Confirmai.Data;
using Confirmai.Models;
using Microsoft.EntityFrameworkCore;

namespace Confirmai.Services;

public class AdminSettingsService
{
    public const string OperationFeePercentKey = "OperationFeePercent";
    public const string SiteIntermediaryPixKey = "SiteIntermediaryPixKey";
    public const string LuaDeliveryEnabledKey = "LuaDeliveryEnabled";
    public const decimal DefaultOperationFeePercent = 2.0m;

    private readonly AppDbContext _db;
    private readonly LogService? _log;

    public AdminSettingsService(AppDbContext db, LogService? log = null)
    {
        _db = db;
        _log = log;
    }

    public async Task<decimal> GetOperationFeePercentAsync()
    {
        var setting = await _db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == OperationFeePercentKey);

        if (setting is null)
        {
            return DefaultOperationFeePercent;
        }

        if (!decimal.TryParse(setting.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return DefaultOperationFeePercent;
        }

        return Math.Clamp(parsed, 0m, 100m);
    }

    public Task<decimal> GetOperationFeePercentForAdminAsync(ClaimsPrincipal? user)
    {
        EnsureAdmin(user);
        return GetOperationFeePercentAsync();
    }

    public async Task<bool> SetOperationFeePercentAsync(decimal operationFeePercent, string? actorUserId = null)
    {
        if (operationFeePercent < 0m || operationFeePercent > 100m)
        {
            return false;
        }

        var normalized = Math.Round(operationFeePercent, 2, MidpointRounding.AwayFromZero)
            .ToString("0.##", CultureInfo.InvariantCulture);

        var setting = await _db.AppSettings.FindAsync(OperationFeePercentKey);

        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = OperationFeePercentKey,
                Value = normalized
            };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = normalized;
        }

        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.AdminSettingChanged,
                AuditEntities.Setting,
                OperationFeePercentKey,
                $"Configura\u00e7\u00e3o alterada: {OperationFeePercentKey} = {normalized}.",
                actorUserId: actorUserId,
                source: AdminAuditSources.AdminSettings);

        return true;
    }

    public Task<bool> SetOperationFeePercentForAdminAsync(ClaimsPrincipal? user, decimal operationFeePercent)
    {
        EnsureAdmin(user);
        var actorUserId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return SetOperationFeePercentAsync(operationFeePercent, actorUserId);
    }

    public async Task<string?> GetSiteIntermediaryPixKeyAsync()
    {
        var setting = await _db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == SiteIntermediaryPixKey);

        var key = setting?.Value?.Trim();
        return string.IsNullOrWhiteSpace(key) ? null : key;
    }

    public Task<string?> GetSiteIntermediaryPixKeyForAdminAsync(ClaimsPrincipal? user)
    {
        EnsureAdmin(user);
        return GetSiteIntermediaryPixKeyAsync();
    }

    public async Task<bool> SetSiteIntermediaryPixKeyAsync(string? pixKey, string? actorUserId = null)
    {
        var normalized = pixKey?.Trim();
        var setting = await _db.AppSettings.FindAsync(SiteIntermediaryPixKey);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            if (setting is not null)
            {
                _db.AppSettings.Remove(setting);
                await _db.SaveChangesAsync();

                if (_log != null)
                    await _log.AuditAsync(
                        AuditEvents.AdminSettingChanged,
                        AuditEntities.Setting,
                        SiteIntermediaryPixKey,
                        $"Configuração removida: {SiteIntermediaryPixKey}.",
                        actorUserId: actorUserId,
                        source: AdminAuditSources.AdminSettings);
            }

            return true;
        }

        if (normalized.Length > 160)
        {
            return false;
        }

        if (setting is null)
        {
            setting = new AppSetting
            {
                Key = SiteIntermediaryPixKey,
                Value = normalized
            };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = normalized;
        }

        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.AdminSettingChanged,
                AuditEntities.Setting,
                SiteIntermediaryPixKey,
                $"Configura\u00e7\u00e3o alterada: {SiteIntermediaryPixKey}.",
                actorUserId: actorUserId,
                source: AdminAuditSources.AdminSettings);

        return true;
    }

    public Task<bool> SetSiteIntermediaryPixKeyForAdminAsync(ClaimsPrincipal? user, string? pixKey)
    {
        EnsureAdmin(user);
        var actorUserId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return SetSiteIntermediaryPixKeyAsync(pixKey, actorUserId);
    }

    public async Task<bool> GetLuaDeliveryEnabledAsync()
    {
        var setting = await _db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == LuaDeliveryEnabledKey);

        return setting?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    public Task<bool> GetLuaDeliveryEnabledForAdminAsync(ClaimsPrincipal? user)
    {
        EnsureAdmin(user);
        return GetLuaDeliveryEnabledAsync();
    }

    public async Task SetLuaDeliveryEnabledAsync(bool enabled, string? actorUserId = null)
    {
        var setting = await _db.AppSettings.FindAsync(LuaDeliveryEnabledKey);
        var value = enabled ? "true" : "false";

        if (setting is null)
        {
            _db.AppSettings.Add(new AppSetting { Key = LuaDeliveryEnabledKey, Value = value });
        }
        else
        {
            setting.Value = value;
        }

        await _db.SaveChangesAsync();

        if (_log != null)
            await _log.AuditAsync(
                AuditEvents.AdminSettingChanged,
                AuditEntities.Setting,
                LuaDeliveryEnabledKey,
                $"Configura\u00e7\u00e3o alterada: {LuaDeliveryEnabledKey} = {value}.",
                actorUserId: actorUserId,
                source: AdminAuditSources.AdminSettings);
    }

    public Task SetLuaDeliveryEnabledForAdminAsync(ClaimsPrincipal? user, bool enabled)
    {
        EnsureAdmin(user);
        var actorUserId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return SetLuaDeliveryEnabledAsync(enabled, actorUserId);
    }

    private static void EnsureAdmin(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true || !user.IsInRole("admin"))
        {
            throw new UnauthorizedAccessException("Apenas administradores podem alterar configurações administrativas.");
        }
    }
}


