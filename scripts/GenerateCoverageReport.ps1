#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates code coverage reports for Chronicis test projects.

.DESCRIPTION
    This script runs unit test projects with coverage collection, merges those
    results, runs architectural tests without coverage, and generates HTML
    coverage reports using ReportGenerator.

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

        # Run unit tests with coverage (avoid duplicate branch metrics from architectural tests)
        Write-Host ""
        Write-Host "Running unit tests with code coverage..." -ForegroundColor Yellow
        Write-Host ""

        $unitCoverageRoot = "tests/TestResults/unit-coverage"
        New-Item -ItemType Directory -Force -Path $unitCoverageRoot | Out-Null

        $coverageRuns = @(
            @{ Name = "Shared"; Project = "tests/Chronicis.Shared.Tests/Chronicis.Shared.Tests.csproj"; ResultsSubdir = "shared" },
            @{ Name = "Api";    Project = "tests/Chronicis.Api.Tests/Chronicis.Api.Tests.csproj";       ResultsSubdir = "api" },
            @{ Name = "Client"; Project = "tests/Chronicis.Client.Tests/Chronicis.Client.Tests.csproj"; ResultsSubdir = "client" }
        )

        foreach ($run in $coverageRuns) {
            $projectResultsDir = Join-Path $unitCoverageRoot $run.ResultsSubdir
            Write-Host "Collecting coverage for $($run.Name): $($run.Project)" -ForegroundColor Cyan

            dotnet test $run.Project `
                --configuration $Configuration `
                --settings coverlet.runsettings `
                --collect:"XPlat Code Coverage" `
                --results-directory $projectResultsDir `
                --verbosity normal

            if ($LASTEXITCODE -ne 0) {
                throw "Coverage test run failed for $($run.Project) with exit code $LASTEXITCODE"
            }
        }

        Write-Host ""
        Write-Host "Running architectural tests (no coverage)..." -ForegroundColor Yellow
        dotnet test "tests/Chronicis.ArchitecturalTests/Chronicis.ArchitecturalTests.csproj" `
            --configuration $Configuration `
            --verbosity normal

        if ($LASTEXITCODE -ne 0) {
            throw "Architectural tests failed with exit code $LASTEXITCODE"
        }
    }

    # Find all coverage files
    Write-Host ""
    Write-Host "Collecting coverage files..." -ForegroundColor Yellow
    $coverageRoot = "tests/TestResults/unit-coverage"
    $coverageFiles = Get-ChildItem -Path $coverageRoot -Filter "coverage.cobertura.xml" -Recurse
    
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "No coverage files found under $coverageRoot. Make sure unit test coverage runs completed successfully."
        exit 1
    }
    
    Write-Host "Found $($coverageFiles.Count) coverage file(s)" -ForegroundColor Green

    # Generate HTML report
    Write-Host ""
    Write-Host "Generating HTML coverage report..." -ForegroundColor Yellow

    $coverageMap = @{
        Shared = (Get-ChildItem -Recurse -Path (Join-Path $coverageRoot "shared") -Filter "coverage.cobertura.xml" | Select-Object -First 1 -ExpandProperty FullName)
        Api    = (Get-ChildItem -Recurse -Path (Join-Path $coverageRoot "api")    -Filter "coverage.cobertura.xml" | Select-Object -First 1 -ExpandProperty FullName)
        Client = (Get-ChildItem -Recurse -Path (Join-Path $coverageRoot "client")  -Filter "coverage.cobertura.xml" | Select-Object -First 1 -ExpandProperty FullName)
    }

    foreach ($key in @("Shared","Api","Client")) {
        if (-not $coverageMap[$key]) {
            throw "Missing coverage input for $key"
        }
    }

    $outputPath = Join-Path $solutionRoot $Output
    $stagingPath = Join-Path $solutionRoot "tests/TestResults/CoverageStaging"
    
    # Ensure output directory exists
    if (-not (Test-Path $outputPath)) {
        New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
    }
    if (Test-Path $stagingPath) {
        Remove-Item -Recurse -Force $stagingPath
    }
    New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

    $stagedCoberturas = @()
    $assemblyInputs = @(
        @{ Name = "shared"; Assembly = "Chronicis.Shared"; Source = $coverageMap["Shared"] },
        @{ Name = "api";    Assembly = "Chronicis.Api";    Source = $coverageMap["Api"] },
        @{ Name = "client"; Assembly = "Chronicis.Client"; Source = $coverageMap["Client"] }
    )

    foreach ($input in $assemblyInputs) {
        $target = Join-Path $stagingPath $input.Name
        New-Item -ItemType Directory -Path $target -Force | Out-Null

        reportgenerator `
            "-reports:$($input.Source)" `
            "-targetdir:$target" `
            "-assemblyfilters:+$($input.Assembly)" `
            "-reporttypes:Cobertura" `
            "-verbosity:Info"

        if ($LASTEXITCODE -ne 0) {
            throw "Staging coverage generation failed for $($input.Assembly)"
        }

        $stagedCobertura = Join-Path $target "Cobertura.xml"
        if (-not (Test-Path $stagedCobertura)) {
            throw "Expected staged Cobertura.xml not found for $($input.Assembly)"
        }

        $stagedCoberturas += $stagedCobertura
    }

    $reportFiles = $stagedCoberturas -join ';'
    
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
