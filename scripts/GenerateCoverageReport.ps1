#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates code coverage reports for Chronicis test projects.

.DESCRIPTION
    This script runs all tests with coverage collection, merges the results,
    and generates HTML coverage reports using ReportGenerator.

.PARAMETER Configuration
    Build configuration (Debug or Release). Default: Debug

.PARAMETER Output
    Output directory for coverage reports. Default: tests/TestResults/Coverage

.PARAMETER SkipTests
    Skip running tests and only generate reports from existing coverage files.

.EXAMPLE
    .\GenerateCoverageReport.ps1
    
.EXAMPLE
    .\GenerateCoverageReport.ps1 -Configuration Release -Output ./coverage-output
#>

param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    
    [Parameter()]
    [string]$Output = 'tests/TestResults/Coverage',
    
    [Parameter()]
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

# Ensure we're in the solution root
$solutionRoot = Split-Path -Parent $PSScriptRoot
Push-Location $solutionRoot

try {
    Write-Host "=== Chronicis Code Coverage Report Generator ===" -ForegroundColor Cyan
    Write-Host ""

    # Check if ReportGenerator is installed
    $reportGeneratorInstalled = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"
    if (-not $reportGeneratorInstalled) {
        Write-Host "Installing ReportGenerator global tool..." -ForegroundColor Yellow
        dotnet tool install -g dotnet-reportgenerator-globaltool
    }

    if (-not $SkipTests) {
        # Clean previous test results
        Write-Host "Cleaning previous test results..." -ForegroundColor Yellow
        if (Test-Path "tests/TestResults") {
            Remove-Item -Recurse -Force "tests/TestResults"
        }

        # Run tests with coverage
        Write-Host ""
        Write-Host "Running tests with code coverage..." -ForegroundColor Yellow
        Write-Host ""
        
        dotnet test `
            --configuration $Configuration `
            --settings coverlet.runsettings `
            --collect:"XPlat Code Coverage" `
            --results-directory "tests/TestResults" `
            --verbosity normal
        
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed with exit code $LASTEXITCODE"
        }
    }

    # Find all coverage files
    Write-Host ""
    Write-Host "Collecting coverage files..." -ForegroundColor Yellow
    $coverageFiles = Get-ChildItem -Path "tests/TestResults" -Filter "coverage.cobertura.xml" -Recurse `
        | Where-Object { $_.FullName -notmatch '[\\/]+Chronicis\.ArchitecturalTests[\\/]' }
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found. Make sure tests ran successfully."
        exit 1
    }
    
    Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

    # Generate HTML report
    Write-Host ""
    Write-Host "Generating HTML coverage report..." -ForegroundColor Yellow
    
    $reportFiles = ($coverageFiles | ForEach-Object { $_.FullName }) -join ';'
    $outputPath = Join-Path $solutionRoot $Output
    
    # Ensure output directory exists
    if (-not (Test-Path $outputPath)) {
        New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
    }
    
    reportgenerator `
        "-reports:$reportFiles" `
        "-targetdir:$outputPath" `
        "-reporttypes:Html;HtmlSummary;Badges;Cobertura" `
        "-verbosity:Info"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Report generation failed with exit code $LASTEXITCODE"
    }

    # Display summary
    Write-Host ""
    Write-Host "=== Coverage Report Generated ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "HTML Report: $outputPath/index.html" -ForegroundColor Cyan
    Write-Host "Summary:     $outputPath/summary.html" -ForegroundColor Cyan
    Write-Host ""
    
    # Parse and display coverage summary
    $summaryFile = Join-Path $outputPath "summary.html"
    if (Test-Path $summaryFile) {
        $summary = Get-Content $summaryFile -Raw
        if ($summary -match 'Line coverage.*?(\d+\.?\d*)%') {
            $lineCoverage = [double]$matches[1]
            Write-Host "Line Coverage: $lineCoverage%" -ForegroundColor $(
                if ($lineCoverage -ge 80) { 'Green' }
                elseif ($lineCoverage -ge 70) { 'Yellow' }
                else { 'Red' }
            )
        }
        if ($summary -match 'Branch coverage.*?(\d+\.?\d*)%') {
            $branchCoverage = [double]$matches[1]
            Write-Host "Branch Coverage: $branchCoverage%" -ForegroundColor $(
                if ($branchCoverage -ge 75) { 'Green' }
                elseif ($branchCoverage -ge 65) { 'Yellow' }
                else { 'Red' }
            )
        }
    }
    
    Write-Host ""
    Write-Host "Opening report in browser..." -ForegroundColor Yellow
    Start-Process (Join-Path $outputPath "index.html")
    
} catch {
    Write-Error "Coverage report generation failed: $_"
    exit 1
} finally {
    Pop-Location
}
