using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Confirmai.Tests;

/// <summary>
/// Resolves the shared Postgres backend for integration tests, once per test run.
/// Resolution order:
///   1. POSTGRES_CONNECTION env var (explicit override; plan C33).
///   2. ConnectionStrings__DefaultConnection env var (what ci.yml already exports).
///   3. User-secrets ConnectionStrings:DefaultConnection (dev machines).
///   4. Testcontainers.PostgreSql shared container (machines with Docker).
/// Each IntegrationTestWebAppFactory then creates its own citest_* schema inside
/// the resolved server/database, so factories stay isolated without CREATEDB.
/// </summary>
internal static class PostgresTestBackend
{
    private static readonly Lazy<BackendConnection> _shared = new(Resolve);

    private static PostgreSqlContainer? _container;

    /// <summary>Base connection for the resolved backend (no SearchPath).</summary>
    public static BackendConnection Get() => _shared.Value;

    private static BackendConnection Resolve()
    {
        var candidates = new List<(string Name, string? ConnectionString)>
        {
            ("POSTGRES_CONNECTION", Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")),
            ("ConnectionStrings__DefaultConnection", Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")),
        };

        var secrets = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();
        candidates.Add(("user-secrets DefaultConnection", secrets.GetConnectionString("DefaultConnection")));

        var failures = new List<string>();
        foreach (var (name, connStr) in candidates)
        {
            if (string.IsNullOrWhiteSpace(connStr))
            {
                continue;
            }

            try
            {
                using var conn = new NpgsqlConnection(connStr);
                conn.Open();
                CleanupStaleSchemas(conn);
                return new BackendConnection(connStr, name);
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
            }
        }

        try
        {
            var container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            container.StartAsync().GetAwaiter().GetResult();
            _container = container;
            return new BackendConnection(container.GetConnectionString(), "Testcontainers postgres:16-alpine");
        }
        catch (Exception ex)
        {
            failures.Add($"Testcontainers: {ex.Message}");
        }

        throw new InvalidOperationException(
            "Nenhum backend Postgres disponivel para os testes de integracao. " +
            "Defina POSTGRES_CONNECTION, exporte ConnectionStrings__DefaultConnection, " +
            "configure dotnet user-secrets, ou instale Docker (Testcontainers). " +
            "Tentativas: " + string.Join(" | ", failures));
    }

    /// <summary>
    /// Drops citest_* schemas left behind by killed test runs. The schema name
    /// carries its creation time (citest_yyMMddHHmm_*); anything older than 6h
    /// is considered stale.
    /// </summary>
    private static void CleanupStaleSchemas(NpgsqlConnection conn)
    {
        var stale = new List<string>();
        using (var cmd = new NpgsqlCommand(
            "SELECT nspname FROM pg_namespace WHERE nspname LIKE 'citest\\_%'", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (TryParseSchemaTimestamp(name, out var createdAt) &&
                    createdAt < DateTime.UtcNow.AddHours(-6))
                {
                    stale.Add(name);
                }
            }
        }

        foreach (var name in stale)
        {
            try
            {
                using var drop = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{name}\" CASCADE", conn);
                drop.ExecuteNonQuery();
            }
            catch
            {
                // Best-effort cleanup; a schema in use by a parallel run may fail to drop.
            }
        }
    }

    /// <summary>Parses citest_yyMMddHHmm_guid names created by IntegrationTestWebAppFactory.</summary>
    internal static bool TryParseSchemaTimestamp(string schemaName, out DateTime createdAtUtc)
    {
        createdAtUtc = default;
        if (!schemaName.StartsWith("citest_", StringComparison.Ordinal) || schemaName.Length < 18)
        {
            return false;
        }

        return DateTime.TryParseExact(
            schemaName.Substring(7, 10),
            "yyMMddHHmm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out createdAtUtc);
    }

    internal sealed record BackendConnection(string ConnectionString, string Source);
}
