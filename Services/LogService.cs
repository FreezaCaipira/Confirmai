using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Confirmai.Data;
using Confirmai.Models;

namespace Confirmai.Services
{
    public class LogService
    {
        private static readonly JsonSerializerOptions MetadataJsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly AppDbContext _db;
        private readonly ILogger<LogService> _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public LogService(AppDbContext db, ILogger<LogService> logger, IHttpContextAccessor? httpContextAccessor = null)
        {
            _db = db;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string message, string source = "App", string level = "Info", string? userId = null, Exception? ex = null)
        {
            var log = new AppLog
            {
                UserId = userId,
                Level = level,
                Source = source,
                Message = message,
                Exception = ex?.ToString()
            };

            EnrichWithRequestContext(log);

            _db.Logs.Add(log);
            await _db.SaveChangesAsync();

            EmitToLoggingPipeline(log, ex);
        }

        /// <summary>
        /// Records a structured audit event. Prefer this over <see cref="LogAsync"/>
        /// for any business-relevant action (payments, orders, user/server/product
        /// CRUD, admin overrides). The <paramref name="eventType"/> should come
        /// from <see cref="AuditEvents"/> and <paramref name="entityType"/> from
        /// <see cref="AuditEntities"/>.
        /// </summary>
        public async Task AuditAsync(
            string eventType,
            string entityType,
            string? entityId,
            string message,
            string? actorUserId = null,
            string source = "Audit",
            string level = "Info",
            object? metadata = null,
            Exception? ex = null)
        {
            var log = new AppLog
            {
                UserId = actorUserId,
                Level = level,
                Source = source,
                Message = message,
                Exception = ex?.ToString(),
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                MetadataJson = SerializeMetadata(metadata),
            };

            EnrichWithRequestContext(log);

            _db.Logs.Add(log);
            await _db.SaveChangesAsync();

            EmitToLoggingPipeline(log, ex);
        }

        private void EnrichWithRequestContext(AppLog log)
        {
            var ctx = _httpContextAccessor?.HttpContext;
            if (ctx is null)
            {
                return;
            }

            if (string.IsNullOrEmpty(log.IpAddress))
            {
                var ip = ctx.Connection?.RemoteIpAddress?.ToString();
                if (!string.IsNullOrEmpty(ip))
                {
                    log.IpAddress = ip.Length > 64 ? ip[..64] : ip;
                }
            }

            if (string.IsNullOrEmpty(log.CorrelationId))
            {
                var traceId = ctx.TraceIdentifier;
                if (!string.IsNullOrEmpty(traceId))
                {
                    log.CorrelationId = traceId.Length > 80 ? traceId[..80] : traceId;
                }
            }
        }

        private static string? SerializeMetadata(object? metadata)
        {
            if (metadata is null) return null;
            try
            {
                var json = JsonSerializer.Serialize(metadata, MetadataJsonOptions);
                return PiiSanitizer.MaskEmailsInJson(json);
            }
            catch
            {
                // Never let metadata serialization fail an audit write.
                return null;
            }
        }

        private void EmitToLoggingPipeline(AppLog log, Exception? ex)
        {
            var logLevel = log.Level switch
            {
                "Warning" => LogLevel.Warning,
                "Error"   => LogLevel.Error,
                _         => LogLevel.Information,
            };

            if (ex != null)
                _logger.Log(logLevel, ex, "[{Source}] {Message}", log.Source, log.Message);
            else
                _logger.Log(logLevel, "[{Source}] {Message}", log.Source, log.Message);
        }
    }
}

