# Stop the running API process
Write-Host "Stopping running Chronicis API process..." -ForegroundColor Yellow
Get-Process -Name "Chronicis.Api" -ErrorAction SilentlyContinue | Stop-Process -Force

Start-Sleep -Seconds 2

# Build the API project
Write-Host "`nBuilding Chronicis API..." -ForegroundColor Cyan
Set-Location "Z:\repos\chronicis"
dotnet build src/Chronicis.API/Chronicis.API.csproj

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild succeeded!" -ForegroundColor Green
} else {
    Write-Host "`nBuild failed with errors!" -ForegroundColor Red
    exit 1
}

Write-Host "`nYou can now manually restart the API if needed." -ForegroundColor Yellow
