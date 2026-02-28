# Process JSON files and split into individual records with deep merge
param(
    [Parameter(Mandatory=$false)]
    [string]$SourcePath = ".",

    [Parameter(Mandatory=$false)]
    [string]$FilePattern = "*.json"
)

# Function to perform deep merge - keeps existing values, only adds missing fields
function Merge-JsonObjects {
    param(
        [Parameter(Mandatory=$true)]
        $Existing,

        [Parameter(Mandatory=$true)]
        $New
    )

    $newProps = $New.PSObject.Properties

    foreach ($prop in $newProps) {
        $propName = $prop.Name
        $newValue = $prop.Value

        if (-not ($Existing.PSObject.Properties.Name -contains $propName)) {
            $Existing | Add-Member -NotePropertyName $propName -NotePropertyValue $newValue -Force
        }
        elseif ($newValue -is [PSCustomObject] -and $Existing.$propName -is [PSCustomObject]) {
            Merge-JsonObjects -Existing $Existing.$propName -New $newValue
        }
    }

    return $Existing
}

function Get-SafeDirName {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Value
    )

    $v = $Value.Trim()

    # Normalize whitespace
    $v = $v -replace '\s+', ' '

    # Replace slashes and backslashes (path separators)
    $v = $v -replace '[/\\]+', '-'

    # Replace invalid filename characters (Windows-safe)
    $v = $v -replace '[<>:"|?*\x00-\x1F]', '-'

    # Collapse multiple dashes and trim
    $v = $v -replace '-{2,}', '-'
    $v = $v.Trim(' ', '.', '-')

    if ([string]::IsNullOrWhiteSpace($v)) { return "unknown" }
    return $v
}

function Get-RecordFields {
    param(
        [Parameter(Mandatory=$true)]
        $Record
    )

    # Ensure we always have something safe to query.
    if ($null -eq $Record) { return $null }
    if ($Record.PSObject.Properties.Name -contains 'fields') { return $Record.fields }
    return $null
}

function Get-SourceRootFolderName {
    param(
        [Parameter(Mandatory=$true)]
        [string]$FileName
    )

    # e.g. creatures17.json -> creatures
    $stem = [System.IO.Path]::GetFileNameWithoutExtension($FileName)

    # Remove digits anywhere in the stem
    $noDigits = $stem -replace '\d+', ''

    # If stripping digits leaves something like "creatures__" or empty, still sanitize
    return Get-SafeDirName -Value $noDigits
}

# Initialize counters
$totalRecords = 0
$errors = @()
$mergedRecords = 0
$pkFallbacks = 0

# Create computed directory
$computedDir = Join-Path $SourcePath "computed"
if (-not (Test-Path $computedDir)) {
    New-Item -ItemType Directory -Path $computedDir | Out-Null
    Write-Host "Created directory: $computedDir" -ForegroundColor Green
}

# Get all JSON files in source directory
$jsonFiles = Get-ChildItem -Path $SourcePath -Filter $FilePattern -File

Write-Host "`nProcessing JSON files..." -ForegroundColor Cyan
Write-Host "Source: $SourcePath" -ForegroundColor Gray
Write-Host "Output: $computedDir`n" -ForegroundColor Gray

foreach ($file in $jsonFiles) {
    Write-Host "Processing: $($file.Name)" -ForegroundColor Yellow

    # New: per-source-file root folder under computed (digits removed)
    $sourceRoot = Get-SourceRootFolderName -FileName $file.Name
    $sourceComputedDir = Join-Path $computedDir $sourceRoot
    if (-not (Test-Path $sourceComputedDir)) {
        New-Item -ItemType Directory -Path $sourceComputedDir | Out-Null
    }

    try {
        # Read and parse JSON
        $content = Get-Content $file.FullName -Raw -ErrorAction Stop
        $data = $content | ConvertFrom-Json -ErrorAction Stop

        # Handle both array of records and single record
        $records = if ($data -is [Array]) { $data } else { @($data) }

        foreach ($record in $records) {
            try {
                $fields = Get-RecordFields -Record $record

                # Determine filename - use pk or fallback to fields.name
                $pkValue = $null

                if ($record.PSObject.Properties.Name -contains 'pk' -and $record.pk) {
                    $pkValue = [string]$record.pk
                }
                elseif ($null -ne $fields -and ($fields.PSObject.Properties.Name -contains 'name') -and $fields.name) {
                    $namePart = ([string]$fields.name).ToLower() -replace '[\s/\\]+', '-'
                    $pkValue = "symbaroum_$namePart"
                    $pkFallbacks++
                }
                else {
                    $errors += "Record in $($file.Name) missing both 'pk' and 'fields.name' fields"
                    continue
                }

                # Sanitize pkValue in case it contains slashes
                $pkValue = $pkValue -replace '[/\\]+', '-'

                # Build output subdirectories:
                # first: fields.category else fields.type else uncategorized
                $categoryOrType = $null
                if ($null -ne $fields -and ($fields.PSObject.Properties.Name -contains 'category') -and $fields.category) {
                    $categoryOrType = [string]$fields.category
                }
                elseif ($null -ne $fields -and ($fields.PSObject.Properties.Name -contains 'type') -and $fields.type) {
                    $categoryOrType = [string]$fields.type
                }
                else {
                    $categoryOrType = "uncategorized"
                }

                $dir1 = Get-SafeDirName -Value $categoryOrType

                # New base: computed\<sourceRoot>\...
                $outputDir = Join-Path $sourceComputedDir $dir1

                $dir2 = $null
                if ($null -ne $fields -and ($fields.PSObject.Properties.Name -contains 'subcategory') -and $fields.subcategory) {
                    $dir2 = Get-SafeDirName -Value ([string]$fields.subcategory)
                    $outputDir = Join-Path $outputDir $dir2
                }

                if (-not (Test-Path $outputDir)) {
                    New-Item -ItemType Directory -Path $outputDir | Out-Null
                }

                $baseFileName = "$pkValue.json"
                $outputPath = Join-Path $outputDir $baseFileName

                # Check if file already exists
                if (Test-Path $outputPath) {
                    # File exists - perform deep merge
                    try {
                        $existingContent = Get-Content $outputPath -Raw | ConvertFrom-Json
                        $mergedContent = Merge-JsonObjects -Existing $existingContent -New $record
                        $mergedContent | ConvertTo-Json -Depth 100 | Out-File $outputPath -Encoding UTF8
                        $mergedRecords++

                        # Friendly display
                        if ($null -ne $dir2) {
                            $displayPath = Join-Path (Join-Path (Join-Path $sourceRoot $dir1) $dir2) $baseFileName
                        } else {
                            $displayPath = Join-Path (Join-Path $sourceRoot $dir1) $baseFileName
                        }

                        Write-Host "  Merged: $displayPath" -ForegroundColor Cyan
                    }
                    catch {
                        $errors += "Error merging record '$pkValue' from $($file.Name): $($_.Exception.Message)"
                    }
                }
                else {
                    # New file - write it
                    $record | ConvertTo-Json -Depth 100 | Out-File $outputPath -Encoding UTF8
                }

                $totalRecords++
            }
            catch {
                $errorPk = if ($record.PSObject.Properties.Name -contains 'pk' -and $record.pk) {
                    $record.pk
                } elseif ($record.PSObject.Properties.Name -contains 'fields' -and $record.fields -and $record.fields.name) {
                    $record.fields.name
                } else {
                    "unknown"
                }

                $errors += "Error processing record '$errorPk' from $($file.Name): $($_.Exception.Message)"
            }
        }
    }
    catch {
        $errors += "Error reading file $($file.Name): $($_.Exception.Message)"
    }
}

# Output summary
Write-Host "`n=== Processing Complete ===" -ForegroundColor Cyan
Write-Host "Total records processed: $totalRecords" -ForegroundColor Green
Write-Host "Records merged: $mergedRecords" -ForegroundColor $(if ($mergedRecords -gt 0) { "Yellow" } else { "Green" })
Write-Host "Used fields.name fallback: $pkFallbacks" -ForegroundColor $(if ($pkFallbacks -gt 0) { "Yellow" } else { "Green" })
Write-Host "Errors encountered: $($errors.Count)" -ForegroundColor $(if ($errors.Count -gt 0) { "Red" } else { "Green" })

if ($errors.Count -gt 0) {
    Write-Host "`n=== Errors ===" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

Write-Host "`nOutput location: $computedDir" -ForegroundColor Gray
