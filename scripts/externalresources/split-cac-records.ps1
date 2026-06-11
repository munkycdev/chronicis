# Split Castles & Crusades JSON files into one-record-per-blob layout.
#
# Source files have the shape:
#   { "$schema": "...", "<array-key>": [ { "id": "...", "name": "...", ... }, ... ], ... }
#
# Output layout (mirrors ros/srd "computed" convention):
#   computed/<file-stem>/<subfolder>/<slug>.json
#
# Subfolder selection:
#   - Single top-level array  -> item.category / classification / type (if any)
#   - Multiple top-level arrays -> array-key, then optionally item.category
param(
    [string]$SourcePath = "Z:\external-data\cac\raw",
    [string]$FilePattern = "*.json"
)

function Get-SafeSlug {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return "" }
    $v = $Value.ToLowerInvariant().Trim()
    $v = $v -replace '[^a-z0-9]+', '-'
    $v = $v -replace '-{2,}', '-'
    return $v.Trim('-')
}

function Get-ItemSubfolder {
    param($Item)
    foreach ($probe in 'category','classification','type') {
        if ($Item.PSObject.Properties.Name -contains $probe) {
            $v = $Item.$probe
            if ($v -and -not [string]::IsNullOrWhiteSpace([string]$v)) {
                return Get-SafeSlug -Value ([string]$v)
            }
        }
    }
    return ""
}

function Get-ItemSlug {
    param($Item)
    if ($Item.PSObject.Properties.Name -contains 'id' -and $Item.id) {
        return Get-SafeSlug -Value ([string]$Item.id)
    }
    if ($Item.PSObject.Properties.Name -contains 'name' -and $Item.name) {
        return Get-SafeSlug -Value ([string]$Item.name)
    }
    return ""
}

function Test-IsSplittableArray {
    param($Value)
    if ($null -eq $Value) { return $false }
    if (-not ($Value -is [System.Collections.IList])) { return $false }
    if ($Value.Count -eq 0) { return $false }
    foreach ($el in $Value) {
        if ($el -isnot [PSCustomObject]) { return $false }
    }
    # Require at least one item to have an id or name
    foreach ($el in $Value) {
        $names = $el.PSObject.Properties.Name
        if (($names -contains 'id' -and $el.id) -or ($names -contains 'name' -and $el.name)) {
            return $true
        }
    }
    return $false
}

$computedDir = Join-Path $SourcePath "computed"
if (Test-Path $computedDir) {
    Write-Host "Clearing existing computed/ ..." -ForegroundColor Gray
    Remove-Item -Path (Join-Path $computedDir '*') -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $computedDir | Out-Null
}

$totalRecords = 0
$skipped = 0
$errors = @()
$files = Get-ChildItem -Path $SourcePath -Filter $FilePattern -File

Write-Host ""
Write-Host "Source: $SourcePath" -ForegroundColor Gray
Write-Host "Output: $computedDir" -ForegroundColor Gray
Write-Host ""

foreach ($file in $files) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Yellow
    try {
        $data = Get-Content $file.FullName -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        $errors += "Cannot parse $($file.Name): $($_.Exception.Message)"
        continue
    }

    if ($data -isnot [PSCustomObject]) {
        $errors += "$($file.Name) is not a JSON object at top level"
        continue
    }

    $fileStem = Get-SafeSlug -Value ([System.IO.Path]::GetFileNameWithoutExtension($file.Name))
    $arrayProps = $data.PSObject.Properties | Where-Object { Test-IsSplittableArray -Value $_.Value }

    if ($arrayProps.Count -eq 0) {
        Write-Host "  (no splittable arrays)" -ForegroundColor DarkGray
        continue
    }

    $useArrayKeyAsFolder = $arrayProps.Count -gt 1

    foreach ($prop in $arrayProps) {
        $arrayKey = Get-SafeSlug -Value $prop.Name
        $items = $prop.Value

        foreach ($item in $items) {
            $slug = Get-ItemSlug -Item $item
            if ([string]::IsNullOrEmpty($slug)) {
                $skipped++
                continue
            }

            $segments = @($fileStem)
            if ($useArrayKeyAsFolder) { $segments += $arrayKey }
            $sub = Get-ItemSubfolder -Item $item
            if (-not [string]::IsNullOrEmpty($sub)) { $segments += $sub }

            $outDir = Join-Path $computedDir ($segments -join '\')
            if (-not (Test-Path $outDir)) {
                New-Item -ItemType Directory -Path $outDir -Force | Out-Null
            }

            $outPath = Join-Path $outDir "$slug.json"
            try {
                $item | ConvertTo-Json -Depth 100 | Out-File -FilePath $outPath -Encoding UTF8 -Force
                $totalRecords++
            }
            catch {
                $errors += "Failed to write ${outPath}: $($_.Exception.Message)"
            }
        }
    }
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
Write-Host "Records written: $totalRecords" -ForegroundColor Green
Write-Host "Skipped (no id/name): $skipped" -ForegroundColor $(if ($skipped -gt 0) { 'Yellow' } else { 'Green' })
Write-Host "Errors: $($errors.Count)" -ForegroundColor $(if ($errors.Count -gt 0) { 'Red' } else { 'Green' })
if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}
