$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
try { Clear-Host } catch { }

# --- Config ---
$RepoRoot = "z:\repos\chronicis"
$Solution = Join-Path $RepoRoot "Chronicis.CI.sln"
$RunSettings = Join-Path $RepoRoot "coverlet.runsettings"
$Configuration = "Debug" # or "Release"

$CoverageTargets = @(
    @{
        Name = "Chronicis.Shared"
        Prefix = "Chronicis.Shared."
        ResultsDir = Join-Path $RepoRoot "tests\Chronicis.Shared.Tests\TestResults"
    },
    @{
        Name = "Chronicis.Api"
        Prefix = "Chronicis.Api."
        ResultsDir = Join-Path $RepoRoot "tests\Chronicis.Api.Tests\TestResults"
    },
    @{
        Name = "Chronicis.Client"
        Prefix = "Chronicis.Client."
        ResultsDir = Join-Path $RepoRoot "tests\Chronicis.Client.Tests\TestResults"
    }
)

function Get-CoverageVerificationData {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResultsDir,
        [Parameter(Mandatory = $true)]
        [string]$Prefix
    )

    if (-not (Test-Path $ResultsDir)) {
        return $null
    }

    foreach ($coverageFile in Get-ChildItem -Path $ResultsDir -Recurse -Filter "coverage.cobertura.xml" -File |
        Sort-Object LastWriteTime -Descending) {
        try {
            [xml]$coverage = Get-Content $coverageFile.FullName
            $matchingClasses = @(Get-CoverageClassesByPrefix -Coverage $coverage -Prefix $Prefix)

            if ($matchingClasses.Count -gt 0) {
                return @{
                    File = $coverageFile
                    Coverage = $coverage
                    MatchingClasses = $matchingClasses
                }
            }
        }
        catch {
            continue
        }
    }

    return $null
}

function Get-CoverageClassesByPrefix {
    param(
        [Parameter(Mandatory = $true)]
        [xml]$Coverage,
        [Parameter(Mandatory = $true)]
        [string]$Prefix
    )

    $classes = @()
    $packages = @()
    if ($null -ne $Coverage.coverage -and $null -ne $Coverage.coverage.packages) {
        $packages = @($Coverage.coverage.packages.package)
    }

    foreach ($package in $packages) {
        if ($null -eq $package -or $null -eq $package.classes) {
            continue
        }

        foreach ($class in @($package.classes.class)) {
            if ($class.name -like "$Prefix*") {
                $classes += $class
            }
        }
    }

    return $classes
}

Push-Location $RepoRoot
try {
    dotnet --version

    # Format only files changed vs main (whitespace + style only - analyzers already run during build).
    $changedFiles = @(git diff --name-only main...HEAD --diff-filter=ACMR | Where-Object { $_ -match '\.cs$' })
    if ($changedFiles.Count -gt 0) {
        $includeArg = $changedFiles -join " "
        Write-Host "Running dotnet format on $($changedFiles.Count) changed file(s)..."
        dotnet format whitespace $Solution --no-restore --include $includeArg
        dotnet format style     $Solution --no-restore --include $includeArg
    } else {
        Write-Host "No changed .cs files - skipping dotnet format."
    }

    # Defense against stale bin\ artifacts after cross-branch switches.
    # Coverlet scrapes compiled types from DLLs. If a previous branch compiled
    # types whose source files no longer exist on the current branch, those
    # types remain in bin\ and show up as "uncovered" in cobertura output,
    # producing phantom coverage failures that have no source to fix.
    # A clean pass forces the compiler to re-emit DLLs from only the current
    # working tree, eliminating the class of ghost-type false positives.
    Write-Host "Cleaning solution to eliminate stale build artifacts..."
    dotnet clean $Solution -c $Configuration --nologo --verbosity minimal | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet clean failed." }

    dotnet build $Solution -c $Configuration -m
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

    # Clear previous per-test-project coverage outputs so latest run is authoritative.
    foreach ($target in $CoverageTargets) {
        if (Test-Path $target.ResultsDir) {
            Remove-Item $target.ResultsDir -Recurse -Force
        }
    }

    Write-Host "Running all tests..."
    dotnet test $Solution -c $Configuration --no-build --no-restore `
        --collect:"XPlat Code Coverage" --settings $RunSettings
    if ($LASTEXITCODE -ne 0) { throw "One or more test suites failed." }

    Write-Host ""
    Write-Host "Coverage verification (class-level, line+branch):"

    $coverageFailures = @()

    foreach ($target in $CoverageTargets) {
        $coverageData = Get-CoverageVerificationData -ResultsDir $target.ResultsDir -Prefix $target.Prefix
        if ($null -eq $coverageData) {
            $coverageFailures += "[$($target.Name)] No coverage.cobertura.xml found under $($target.ResultsDir)"
            continue
        }

        $coverageFile = $coverageData.File
        $matchingClasses = $coverageData.MatchingClasses

        $below100 = @($matchingClasses | Where-Object {
            [double]$_.'line-rate' -lt 1 -or [double]$_.'branch-rate' -lt 1
        })

        if ($below100.Count -eq 0) {
            Write-Host "  [PASS] $($target.Name): 100% line and branch across $($matchingClasses.Count) classes"
            continue
        }

        $coverageFailures += "[$($target.Name)] Coverage below 100% for prefix '$($target.Prefix)' in $($coverageFile.FullName)"
        Write-Host "  [FAIL] $($target.Name): classes below 100%"
        foreach ($class in $below100) {
            $linePct = [math]::Round([double]$class.'line-rate' * 100, 2)
            $branchPct = [math]::Round([double]$class.'branch-rate' * 100, 2)
            Write-Host ("    - {0} (line={1}%, branch={2}%)" -f $class.name, $linePct, $branchPct)
        }
    }

    if ($coverageFailures.Count -gt 0) {
        Write-Host ""
        foreach ($failure in $coverageFailures) {
            Write-Warning $failure
        }
        throw "Coverage requirements not met."
    }

    Write-Host ""
    Write-Host "Coverage is 100% line and branch for Chronicis.Shared, Chronicis.Api, and Chronicis.Client." -ForegroundColor Green
    Write-Host "Pre-merge validation complete." -ForegroundColor Green

}
finally {
    Pop-Location
    if ($Host.Name -eq "ConsoleHost" -and [Environment]::UserInteractive -and -not [Console]::IsInputRedirected) {
        try { Read-Host -Prompt "Press Enter to exit" } catch { }
    }
}
