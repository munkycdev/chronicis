$ErrorActionPreference = "Stop"

Write-Host "=== Verifying Phase 1: Quest Entities ===" -ForegroundColor Cyan
Write-Host ""

cd Z:\repos\chronicis

Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --no-incremental

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Build succeeded!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Phase 1 Complete - Ready for Phase 2 (DTOs)" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "✗ Build failed - please review errors above" -ForegroundColor Red
    exit 1
}
