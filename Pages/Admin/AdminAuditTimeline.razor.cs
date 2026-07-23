using Confirmai.Models;
using Confirmai.Services;
using Confirmai.Services.Admin;
using Confirmai.Services.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Confirmai.Pages.Admin;

public partial class AdminAuditTimeline
{
    [Parameter] public string EntityType { get; set; } = string.Empty;
    [Parameter] public string EntityId { get; set; } = string.Empty;

    private List<AppLog> entries = new();
    private bool isLoading = true;

    [Inject] private AdminLogsQueryService AdminLogsQueryService { get; set; } = default!;
    [Inject] private AdminLogsExportService AdminLogsExportService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private UiTextService T { get; set; } = default!;

    protected override async Task OnParametersSetAsync()
    {
        isLoading = true;
        entries = new();

        if (!string.IsNullOrWhiteSpace(EntityType) && !string.IsNullOrWhiteSpace(EntityId))
        {
            entries = await AdminLogsQueryService.GetEntityTimelineAsync(EntityType, EntityId);
        }

        isLoading = false;
    }

    private async Task ExportCsvAsync()
    {
        if (!entries.Any()) return;
        var csv = AdminLogsExportService.BuildTimelineCsv(entries);
        var fileName = AdminLogsExportService.BuildTimelineFileName(EntityType, EntityId, "csv");
        await JS.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, csv, "text/csv;charset=utf-8;");
    }

    private async Task ExportJsonAsync()
    {
        if (!entries.Any()) return;
        var json = AdminLogsExportService.BuildTimelineJson(entries);
        var fileName = AdminLogsExportService.BuildTimelineFileName(EntityType, EntityId, "json");
        await JS.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, json, "application/json;charset=utf-8;");
    }

    private static string GetLevelBadgeClass(string? level) => (level?.Trim().ToLowerInvariant()) switch
    {
        "warning" => "log-level-warning",
        "error" or "bug" or "critical" or "fatal" => "log-level-error",
        _ => "log-level-info"
    };
}
