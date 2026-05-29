param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [switch]$StrictAdmin,

    [int]$TimeoutSeconds = 20
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

function Invoke-SmokeRequest {
    param(
        [string]$Name,
        [string]$Url,
        [int[]]$AllowedStatusCodes
    )

    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -MaximumRedirection 0 -TimeoutSec $TimeoutSeconds
        $statusCode = [int]$response.StatusCode
    }
    catch {
        if ($null -ne $_.Exception.Response -and $null -ne $_.Exception.Response.StatusCode) {
            $statusCode = [int]$_.Exception.Response.StatusCode.value__
        }
        else {
            throw "[$Name] Falha de rede ao acessar $Url. Detalhe: $($_.Exception.Message)"
        }
    }

    $allowed = $AllowedStatusCodes -contains $statusCode
    $statusLabel = if ($allowed) { "OK" } else { "FAIL" }
    Write-Host ("[{0}] {1} -> HTTP {2}" -f $statusLabel, $Name, $statusCode)

    return [pscustomobject]@{
        Name = $Name
        Url = $Url
        StatusCode = $statusCode
        Allowed = $allowed
    }
}

$checks = @(
    @{ Name = "Health endpoint"; Path = "/health"; Allowed = @(200) },
    @{ Name = "Home page"; Path = "/"; Allowed = @(200) },
    @{ Name = "Admin dashboard (anon/auth)"; Path = "/admin"; Allowed = @(200, 302) },
    @{ Name = "Admin payments (anon/auth)"; Path = "/admin/payments"; Allowed = @(200, 302) },
    @{ Name = "Admin logs (anon/auth)"; Path = "/admin/logs"; Allowed = @(200, 302) },
    @{ Name = "Dev seed endpoint disabled"; Path = "/api/test/seed-order"; Allowed = @(404) }
)

if ($StrictAdmin) {
    $checks = $checks | ForEach-Object {
        if ($_.Name -like "Admin*") {
            @{ Name = $_.Name; Path = $_.Path; Allowed = @(200) }
        }
        else {
            $_
        }
    }
}

Write-Host "Iniciando smoke pós-deploy..."
Write-Host ("Base URL: {0}" -f $BaseUrl)
Write-Host ("StrictAdmin: {0}" -f $StrictAdmin.IsPresent)
Write-Host ""

$results = @()
foreach ($check in $checks) {
    $url = Join-Url -Root $BaseUrl -Path $check.Path
    $results += Invoke-SmokeRequest -Name $check.Name -Url $url -AllowedStatusCodes $check.Allowed
}

$failed = $results | Where-Object { -not $_.Allowed }
Write-Host ""
Write-Host "Resumo do smoke:"
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

Write-Host "Smoke pós-deploy concluído com sucesso." -ForegroundColor Green
exit 0
