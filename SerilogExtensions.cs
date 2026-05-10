using Confirmai.Configuration;
using Serilog;
using Serilog.Configuration;
using Serilog.Sinks.OpenTelemetry;

namespace Confirmai;

/// <summary>
/// Extension methods for Serilog <see cref="LoggerConfiguration"/> that
/// conditionally wire up the OpenTelemetry OTLP log sink.
/// </summary>
internal static class SerilogExtensions
{
    /// <summary>
    /// Adds the Serilog OTLP sink when <see cref="OtelOptions.IsEnabled"/> is
    /// true. When the endpoint is not configured, this is a no-op.
    /// </summary>
    public static LoggerConfiguration WriteSerilogOtlpSinkIfEnabled(
        this LoggerConfiguration config,
        OtelOptions options)
    {
        if (!options.IsEnabled) return config;

        var headers = options.ParsedHeaders();

        return config.WriteTo.OpenTelemetry(sink =>
        {
            sink.Endpoint = options.Endpoint;
            sink.Protocol = OtlpProtocol.HttpProtobuf;
            sink.ResourceAttributes = new Dictionary<string, object>
            {
                ["service.name"]    = options.ServiceName,
                ["service.version"] = options.ServiceVersion,
                ["deployment.environment"] = options.Environment,
            };
            if (headers.Count > 0)
            {
                sink.Headers = headers;
            }
        });
    }
}
