using Confirmai.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Confirmai.Services.Core;

/// <summary>
/// C42-B: registra <c>page.view</c> por circuito do Blazor Server.
///
/// Regras:
///  - Message carrega o <b>template de rota</b> (<see cref="PageRoutes"/>),
///    nunca a URL crua — sem ids, códigos de convite nem query strings.
///  - Metadata: apenas { route, device, renderMs } — nada de PII.
///  - Debounce: o mesmo path repetido no mesmo circuito (rerender) grava uma
///    vez; ids diferentes no mesmo template (/grupo/1 → /grupo/2) contam.
///  - Amostragem: <c>Metrics:PageViewSampleRate</c> (default 1.0). O
///    histograma OTel <c>confirmai_page_render_ms</c> sempre registra.
/// </summary>
public sealed class PageViewTracker
{
    private readonly LogService _log;
    private readonly IJSRuntime _js;
    private readonly OperationalMetrics _ops;
    private readonly MetricsOptions _options;
    private readonly Random _rng = new();

    private string? _lastPath;
    private string? _device;

    public PageViewTracker(
        LogService log,
        IJSRuntime js,
        OperationalMetrics ops,
        IOptions<MetricsOptions> options)
    {
        _log = log;
        _js = js;
        _ops = ops;
        _options = options.Value;
    }

    /// <summary>
    /// Registra a navegação. <paramref name="renderMs"/> é o tempo entre o
    /// LocationChanged e o primeiro OnAfterRender da página.
    /// </summary>
    public async Task RecordAsync(string absoluteUri, string? userId, long renderMs)
    {
        string path;
        try
        {
            path = new Uri(absoluteUri).LocalPath;
        }
        catch
        {
            return;
        }

        if (path == _lastPath) return;
        _lastPath = path;

        var template = PageRoutes.Normalize(path);
        _ops.PageRendered(template, renderMs);

        var rate = _options.PageViewSampleRate;
        if (rate < 1.0 && _rng.NextDouble() >= rate) return;

        _device ??= await DetectDeviceAsync();

        try
        {
            await _log.AuditAsync(
                AuditEvents.PageView,
                AuditEntities.PageView,
                template,
                template,
                actorUserId: userId,
                source: "PageView",
                metadata: new { route = template, device = _device, renderMs });
        }
        catch
        {
            // Telemetria nunca derruba o circuito.
        }
    }

    private async Task<string> DetectDeviceAsync()
    {
        try
        {
            var width = await _js.InvokeAsync<int>("ConfirmaiViewportWidth");
            return width > 0 && width < _options.MobileBreakpointPx ? "mobile" : "desktop";
        }
        catch
        {
            return "unknown";
        }
    }
}
