using Confirmai.Services.Admin;
using Microsoft.JSInterop;

namespace Confirmai.Services.Admin;

public sealed record AdminLogsExportResult(string FileName, string Content, string MimeType, string? Notice);

public sealed class AdminLogsExportCommandService
{
    private readonly AdminLogsQueryService _queryService;
    private readonly AdminLogsExportService _exportService;
    private readonly IJSRuntime _js;

    public AdminLogsExportCommandService(
        AdminLogsQueryService queryService,
        AdminLogsExportService exportService,
        IJSRuntime js)
    {
        _queryService = queryService;
        _exportService = exportService;
        _js = js;
    }

    public async Task<string?> ExportCsvAsync(AdminLogFilterCriteria criteria, int maxRows, string level, string source, DateTime? startDate, DateTime? endDate, string truncatedMessage)
    {
        var export = await BuildExportRowsAsync(criteria, maxRows);
        if (export.Rows.Count == 0)
            return null;

        var csv = _exportService.BuildCsv(export.Rows);
        var fileName = _exportService.BuildExportFileName("csv", level, source, startDate, endDate, export.Truncated);
        await _js.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, csv, "text/csv;charset=utf-8;");

        return export.Truncated ? string.Format(truncatedMessage, maxRows) : null;
    }

    public async Task<string?> ExportJsonAsync(AdminLogFilterCriteria criteria, int maxRows, string level, string source, DateTime? startDate, DateTime? endDate, string truncatedMessage)
    {
        var export = await BuildExportRowsAsync(criteria, maxRows);
        if (export.Rows.Count == 0)
            return null;

        var json = _exportService.BuildJson(export.Rows);
        var fileName = _exportService.BuildExportFileName("json", level, source, startDate, endDate, export.Truncated);
        await _js.InvokeVoidAsync("ConfirmaiDownloadFile", fileName, json, "application/json;charset=utf-8;");

        return export.Truncated ? string.Format(truncatedMessage, maxRows) : null;
    }

    private async Task<(List<AdminLogExportRow> Rows, bool Truncated)> BuildExportRowsAsync(AdminLogFilterCriteria criteria, int maxRows)
    {
        return await _queryService.GetExportRowsAsync(criteria, maxRows);
    }
}
