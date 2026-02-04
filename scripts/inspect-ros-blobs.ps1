# Quick Azure Blob Storage inspection script
# Run this to see the actual structure of the ros folder

$storageAccountName = "stchronicis"
$containerName = "open5e"

Write-Host "Inspecting ROS blob structure..." -ForegroundColor Cyan
Write-Host ""

# Check if Azure CLI is available
try {
    $null = az --version 2>$null
} catch {
    Write-Host "Azure CLI not found. Please install it or check the storage manually." -ForegroundColor Red
    exit 1
}

# List blobs in ros folder
Write-Host "Listing blobs under 'ros/' prefix..." -ForegroundColor Yellow
az storage blob list `
    --account-name $storageAccountName `
    --container-name $containerName `
    --prefix "ros/" `
    --num-results 20 `
    --output table

Write-Host ""
Write-Host "Listing blobs under 'ros/computed/' prefix..." -ForegroundColor Yellow
az storage blob list `
    --account-name $storageAccountName `
    --container-name $containerName `
    --prefix "ros/computed/" `
    --num-results 20 `
    --output table
