$content = Get-Content 'wwwroot/css/site.css'
$opens = 0
$closes = 0
for ($i = 0; $i -lt $content.Count; $i++) {
    $line = $content[$i]
    $opens += ($line | Select-String '{' -AllMatches).Matches.Count
    $closes += ($line | Select-String '}' -AllMatches).Matches.Count
    if ($opens -lt $closes) {
        Write-Host "Unbalanced at line $($i+1): opens=$opens closes=$closes"
        Write-Host "Line content: $line"
        break
    }
}
Write-Host "Final: opens=$opens, closes=$closes"
