# Script to fix JSON files with array roots by removing outer square brackets
# This will convert: [ { ... } ] to: { ... }
# Use with caution - backs up original files first

param(
    [Parameter(Mandatory=$true)]
    [string]$FolderPath,
    
    [Parameter(Mandatory=$false)]
    [switch]$WhatIf = $false
)

# Validate folder exists
if (-not (Test-Path $FolderPath)) {
    Write-Host "Error: Folder not found: $FolderPath" -ForegroundColor Red
    exit 1
}

Write-Host "JSON Array Root Fixer" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host "Folder: $FolderPath" -ForegroundColor Gray
Write-Host "WhatIf Mode: $WhatIf" -ForegroundColor Gray
Write-Host ""

# Get all JSON files recursively
$jsonFiles = Get-ChildItem -Path $FolderPath -Filter "*.json" -Recurse -File

Write-Host "Found $($jsonFiles.Count) JSON files" -ForegroundColor Yellow
Write-Host ""

$fixedCount = 0
$skippedCount = 0
$errorCount = 0

foreach ($file in $jsonFiles) {
    try {
        # Read file content
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        
        # Trim whitespace
        $trimmed = $content.Trim()
        
        # Check if it starts with [ and ends with ]
        if ($trimmed.StartsWith('[') -and $trimmed.EndsWith(']')) {
            
            # Parse as JSON to validate structure
            try {
                $json = $trimmed | ConvertFrom-Json
                
                # Check if it's an array with exactly one element
                if ($json -is [System.Array] -and $json.Count -eq 1) {
                    
                    if ($WhatIf) {
                        Write-Host "[WHATIF] Would fix: $($file.FullName)" -ForegroundColor Yellow
                    } else {
                        # Create backup
                        $backupPath = "$($file.FullName).backup"
                        Copy-Item -Path $file.FullName -Destination $backupPath -Force
                        
                        # Remove outer brackets
                        # Find the position of the first { and last }
                        $firstBrace = $trimmed.IndexOf('{')
                        $lastBrace = $trimmed.LastIndexOf('}')
                        
                        if ($firstBrace -gt 0 -and $lastBrace -gt $firstBrace) {
                            $fixed = $trimmed.Substring($firstBrace, $lastBrace - $firstBrace + 1)
                            
                            # Write fixed content back (preserve UTF-8 with BOM to match original)
                            $utf8WithBom = New-Object System.Text.UTF8Encoding $true
                            [System.IO.File]::WriteAllText($file.FullName, $fixed, $utf8WithBom)
                            
                            Write-Host "[FIXED] $($file.Name)" -ForegroundColor Green
                            $fixedCount++
                        } else {
                            Write-Host "[ERROR] Could not find braces in: $($file.Name)" -ForegroundColor Red
                            # Restore from backup
                            Move-Item -Path $backupPath -Destination $file.FullName -Force
                            $errorCount++
                        }
                    }
                    
                } elseif ($json -is [System.Array] -and $json.Count -gt 1) {
                    Write-Host "[SKIP] Array has multiple elements: $($file.Name) (count: $($json.Count))" -ForegroundColor Magenta
                    $skippedCount++
                } else {
                    Write-Host "[SKIP] Not a single-element array: $($file.Name)" -ForegroundColor Gray
                    $skippedCount++
                }
                
            } catch {
                Write-Host "[ERROR] Invalid JSON in: $($file.Name) - $_" -ForegroundColor Red
                $errorCount++
            }
            
        } else {
            # Already an object root, skip
            $skippedCount++
        }
        
    } catch {
        Write-Host "[ERROR] Failed to process: $($file.Name) - $_" -ForegroundColor Red
        $errorCount++
    }
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Fixed: $fixedCount" -ForegroundColor Green
Write-Host "  Skipped: $skippedCount" -ForegroundColor Gray
Write-Host "  Errors: $errorCount" -ForegroundColor Red

if (-not $WhatIf -and $fixedCount -gt 0) {
    Write-Host ""
    Write-Host "Backup files created with .backup extension" -ForegroundColor Yellow
    Write-Host "If everything works, you can delete them with:" -ForegroundColor Yellow
    Write-Host "  Get-ChildItem -Path '$FolderPath' -Filter '*.backup' -Recurse | Remove-Item" -ForegroundColor Gray
}
