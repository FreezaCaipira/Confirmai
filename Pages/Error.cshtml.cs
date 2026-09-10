using System.Diagnostics;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Confirmai.Pages;

/// <summary>
/// Server-side Razor Page for /error. Used by UseExceptionHandler("/error")
/// to handle unhandled exceptions without starting a Blazor circuit.
/// Never exposes the exception — only a generic message and the RequestId
/// for log correlation.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly UiTextService _t;

    public ErrorModel(UiTextService t)
    {
        _t = t;
    }

    public string Title => _t["Error.Title"];
    public string Description => _t["Error.Description"];
    public string RequestIdLabel => _t["Error.RequestId"];
    public string BackHomeLabel => _t["Error.BackHome"];

    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}
