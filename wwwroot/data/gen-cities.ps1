$states = Invoke-RestMethod 'https://servicodados.ibge.gov.br/api/v1/localidades/estados?orderBy=nome'
$result = [ordered]@{}
foreach ($s in $states) {
    $muns = Invoke-RestMethod "https://servicodados.ibge.gov.br/api/v1/localidades/estados/$($s.id)/municipios?orderBy=nome"
    $names = $muns | ForEach-Object { $_.nome }
    $result[$s.sigla] = $names
    Write-Host "$($s.sigla): $($names.Count) cidades"
}
$json = $result | ConvertTo-Json -Depth 3 -Compress
$outPath = 'c:\FreezaSSD\Dev\Confirmai\wwwroot\data\cities.json'
[System.IO.File]::WriteAllText($outPath, $json, [System.Text.Encoding]::UTF8)
Write-Host "Done! File size:" ((Get-Item $outPath).Length) "bytes"
