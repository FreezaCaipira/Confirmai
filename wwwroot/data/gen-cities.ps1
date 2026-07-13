$states = Invoke-RestMethod 'https://servicodados.ibge.gov.br/api/v1/localidades/estados?orderBy=nome'
$sb = [System.Text.StringBuilder]::new("{")
$firstState = $true
foreach ($s in $states) {
    $muns = Invoke-RestMethod "https://servicodados.ibge.gov.br/api/v1/localidades/estados/$($s.id)/municipios?orderBy=nome"
    if (-not $firstState) { [void]$sb.Append(",") }
    $firstState = $false
    $cityNames = $muns | ForEach-Object { "`"$($_.nome)`"" }
    $arr = [string]::Join(",", $cityNames)
    [void]$sb.Append("`"$($s.sigla)`":[$arr]")
    Write-Host "$($s.sigla): $($muns.Count) cidades"
}
[void]$sb.Append("}")
$outPath = 'c:\FreezaSSD\Dev\Confirmai\wwwroot\data\cities.json'
[System.IO.File]::WriteAllText($outPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host "Done! File size:" ((Get-Item $outPath).Length) "bytes"
