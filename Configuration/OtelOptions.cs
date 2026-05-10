namespace Confirmai.Configuration;

/// <summary>
/// Configuration for the OpenTelemetry OTLP export pipeline.
///
/// When <see cref="Endpoint"/> is empty or whitespace, the entire pipeline
/// (traces, metrics, logs via Serilog sink) is disabled so the app runs
/// normally without any external telemetry backend.
///
/// Typical OTLP-compatible backends:
///   - Grafana Cloud (use the OTLP endpoint + Authorization header)
///   - Loki / Tempo / Mimir self-hosted
///   - Azure Monitor (use the ApplicationInsights connection string via
///     the Azure.Monitor.OpenTelemetry.AspNetCore package instead)
///
/// Configuration section: "OpenTelemetry"
/// </summary>
public sealed class OtelOptions
{
    public const string Section = "OpenTelemetry";

    /// <summary>
    /// OTLP collector endpoint. Example: https://otlp-gateway-prod-eu-west-0.grafana.net/otlp
    /// Leave empty to disable all OTLP export.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Additional HTTP headers sent with every OTLP request, comma-separated:
    /// "Authorization=Basic xxxx,X-Scope-OrgID=my-org"
    /// Used for Grafana Cloud authentication and Loki tenant headers.
    /// </summary>
    public string Headers { get; init; } = string.Empty;

    /// <summary>
    /// Service name reported to the telemetry backend.
    /// Defaults to "Confirmai".
    /// </summary>
    public string ServiceName { get; init; } = "Confirmai";

    /// <summary>
    /// Service version reported to the telemetry backend.
    /// Defaults to the assembly informational version.
    /// </summary>
    public string ServiceVersion { get; init; } = "1.0.0";

    /// <summary>
    /// Environment tag (e.g. "production", "staging"). Reported as
    /// deployment.environment resource attribute.
    /// </summary>
    public string Environment { get; init; } = "production";

    /// <summary>
    /// Returns true when OTLP export is configured and should be activated.
    /// </summary>
    public bool IsEnabled => !string.IsNullOrWhiteSpace(Endpoint);

    /// <summary>
    /// Parses the <see cref="Headers"/> string into a dictionary suitable
    /// for the OTLP exporter options.
    /// Format: "Key1=Value1,Key2=Value2"
    /// </summary>
    public Dictionary<string, string> ParsedHeaders()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(Headers)) return result;

        foreach (var pair in Headers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq > 0)
            {
                result[pair[..eq].Trim()] = pair[(eq + 1)..].Trim();
            }
        }
        return result;
    }
}
