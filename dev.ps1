# Libera a porta 5000 matando qualquer processo que a esteja usando
$owner = (Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue).OwningProcess
if ($owner) { Stop-Process -Id $owner -Force -ErrorAction SilentlyContinue }

Write-Host "Limpando build anterior..." -ForegroundColor Yellow
& "C:\Program Files\dotnet\dotnet.exe" clean --verbosity quiet --nologo
Write-Host "Iniciando servidor..." -ForegroundColor Green
& "C:\Program Files\dotnet\dotnet.exe" run
