param(
    [string]$AppBaseUrl = "http://localhost:8080",

    [string]$PrometheusBaseUrl = "http://localhost:9090",

    [string]$AlertmanagerBaseUrl = "http://localhost:9093",

    [string]$GrafanaBaseUrl = "http://localhost:3000",

    [string]$CollectorMetricsUrl = "http://localhost:9464/metrics",

    [string]$WebhookBaseUrl = "http://localhost:18080",

    [int]$TimeoutSeconds = 20,

    [switch]$RequirePaymentRules,

    [switch]$RequireCollectorMetrics
)

$ErrorActionPreference = "Stop"

function Join-Url {
    param(
        [string]$Root,
        [string]$Path
    )

    $rootTrimmed = $Root.TrimEnd('/')
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $rootTrimmed
    }

    if ($Path.StartsWith('/')) {
        return "$rootTrimmed$Path"
    }

    return "$rootTrimmed/$Path"
}

function Invoke-HttpCheck {
    param(
        [string]$Name,
        [string]$Url,
        [int[]]$AllowedStatusCodes,
        [string[]]$MustContain = @()
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -TimeoutSec $TimeoutSeconds
        $statusCode = [int]$response.StatusCode
        $content = [string]$response.Content
    }
    catch {
        if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode.value__
            $content = ""
        }
        else {
            throw "[$Name] Falha de rede ao acessar $Url. Detalhe: $($_.Exception.Message)"
        }
    }

    $allowed = $AllowedStatusCodes -contains $statusCode
    $containsAll = $true
    foreach ($needle in $MustContain) {
        if (-not $content.Contains($needle, [System.StringComparison]::OrdinalIgnoreCase)) {
            $containsAll = $false
            break
        }
    }

    $ok = $allowed -and $containsAll
    $statusLabel = if ($ok) { "OK" } else { "FAIL" }
    Write-Host ("[{0}] {1} -> HTTP {2}" -f $statusLabel, $Name, $statusCode)

    if ($allowed -and -not $containsAll) {
        Write-Host ("      Conteúdo esperado não encontrado: {0}" -f ($MustContain -join ", ")) -ForegroundColor Yellow
    }

    return [pscustomobject]@{
        Name = $Name
        Url = $Url
        StatusCode = $statusCode
        Allowed = $allowed
        ContainsExpected = $containsAll
        Ok = $ok
    }
}

$checks = @(
    @{ Name = "App health"; Url = (Join-Url -Root $AppBaseUrl -Path "/health"); Allowed = @(200); Contains = @() },
    @{ Name = "Collector metrics endpoint"; Url = $CollectorMetricsUrl; Allowed = @(200); Contains = @("# TYPE", "otelcol") },
    @{ Name = "Prometheus health"; Url = (Join-Url -Root $PrometheusBaseUrl -Path "/-/healthy"); Allowed = @(200); Contains = @() },
    @{ Name = "Prometheus rules API"; Url = (Join-Url -Root $PrometheusBaseUrl -Path "/api/v1/rules"); Allowed = @(200); Contains = @() },
    @{ Name = "Alertmanager health"; Url = (Join-Url -Root $AlertmanagerBaseUrl -Path "/-/healthy"); Allowed = @(200); Contains = @() },
    @{ Name = "Alertmanager status API"; Url = (Join-Url -Root $AlertmanagerBaseUrl -Path "/api/v2/status"); Allowed = @(200); Contains = @() },
    @{ Name = "Grafana health"; Url = (Join-Url -Root $GrafanaBaseUrl -Path "/api/health"); Allowed = @(200); Contains = @("ok", "database") },
    @{ Name = "Validation webhook"; Url = $WebhookBaseUrl; Allowed = @(200); Contains = @() }
)

if ($RequirePaymentRules) {
    foreach ($check in $checks) {
        if ($check.Name -eq "Prometheus rules API") {
            $check.Contains = @(
                "ConfirmaiPaymentPanelStale",
                "ConfirmaiPaymentFailedSpike",
                "ConfirmaiPaymentRefundedSpike",
                "ConfirmaiWebhookWarningSpike",
                "ConfirmaiReconciliationPendingGrowing"
            )
        }
    }
}

if ($RequireCollectorMetrics) {
    foreach ($check in $checks) {
        if ($check.Name -eq "Collector metrics endpoint") {
            $check.Contains = @("otelcol_exporter_sent_metric_points")
        }
    }
}

Write-Host "Iniciando smoke da stack de monitoramento..."
Write-Host ("App: {0}" -f $AppBaseUrl)
Write-Host ("Prometheus: {0}" -f $PrometheusBaseUrl)
Write-Host ("Alertmanager: {0}" -f $AlertmanagerBaseUrl)
Write-Host ("Grafana: {0}" -f $GrafanaBaseUrl)
Write-Host ("Collector metrics: {0}" -f $CollectorMetricsUrl)
Write-Host ("Webhook: {0}" -f $WebhookBaseUrl)
Write-Host ("RequirePaymentRules: {0}" -f $RequirePaymentRules.IsPresent)
Write-Host ("RequireCollectorMetrics: {0}" -f $RequireCollectorMetrics.IsPresent)
Write-Host ""

$results = @()
foreach ($check in $checks) {
    $results += Invoke-HttpCheck -Name $check.Name -Url $check.Url -AllowedStatusCodes $check.Allowed -MustContain $check.Contains
}

$failed = $results | Where-Object { -not $_.Ok }

Write-Host ""
Write-Host "Resumo do smoke de monitoramento:"
Write-Host ("Total: {0}" -f $results.Count)
Write-Host ("Sucesso: {0}" -f ($results.Count - $failed.Count))
Write-Host ("Falhas: {0}" -f $failed.Count)

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Falhas detectadas:" -ForegroundColor Red
    foreach ($item in $failed) {
        Write-Host ("- {0} ({1}) -> HTTP {2}" -f $item.Name, $item.Url, $item.StatusCode) -ForegroundColor Red
    }

    exit 1
}

Write-Host "Smoke de monitoramento concluído com sucesso." -ForegroundColor Green
exit 0