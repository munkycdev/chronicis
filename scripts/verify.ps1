$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
clear

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

function Get-LatestCoverageFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ResultsDir
    )

    if (-not (Test-Path $ResultsDir)) {
        return $null
    }

    return Get-ChildItem -Path $ResultsDir -Recurse -Filter "coverage.cobertura.xml" -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
}

function Get-CoverageClassesByPrefix {
    param(
        [Parameter(Mandatory = $true)]
        [xml]$Coverage,
        [Parameter(Mandatory = $true)]
        [string]$Prefix
    )

    $classes = @()
    foreach ($package in $Coverage.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
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

    dotnet build $Solution -c $Configuration -m

    # Clear previous per-test-project coverage outputs so latest run is authoritative.
    foreach ($target in $CoverageTargets) {
        if (Test-Path $target.ResultsDir) {
            Remove-Item $target.ResultsDir -Recurse -Force
        }
    }

    # Run all test projects in parallel using background jobs.
    # Each coverage project gets its own explicit --results-directory so coverlet
    # files stay isolated (solution-level -m can route all results to the same
    # temp directory, causing coverage files to overwrite each other).
    $testJobs = @()

    foreach ($target in $CoverageTargets) {
        $csproj = Join-Path $RepoRoot "tests\$($target.Name).Tests\$($target.Name).Tests.csproj"
        $dir    = $target.ResultsDir
        $rs     = $RunSettings
        $cfg    = $Configuration
        $testJobs += [PSCustomObject]@{
            Name = $target.Name
            Job  = Start-Job -ScriptBlock {
                param($csproj, $dir, $rs, $cfg)
                dotnet test $csproj -c $cfg --no-build --no-restore `
                    --collect:"XPlat Code Coverage" --settings $rs --results-directory $dir
                $LASTEXITCODE
            } -ArgumentList $csproj, $dir, $rs, $cfg
        }
    }

    # Architectural tests - no coverage needed.
    $archCsproj = Join-Path $RepoRoot "tests\Chronicis.ArchitecturalTests\Chronicis.ArchitecturalTests.csproj"
    $cfg = $Configuration
    $testJobs += [PSCustomObject]@{
        Name = "ArchitecturalTests"
        Job  = Start-Job -ScriptBlock {
            param($csproj, $cfg)
            dotnet test $csproj -c $cfg --no-build --no-restore
            $LASTEXITCODE
        } -ArgumentList $archCsproj, $cfg
    }

    Write-Host "Running $($testJobs.Count) test projects in parallel..."
    $null = $testJobs.Job | Wait-Job

    $anyFailed = $false
    foreach ($entry in $testJobs) {
        $results  = @(Receive-Job $entry.Job)
        Remove-Job $entry.Job
        $exitCode = [int]($results | Select-Object -Last 1)
        $results  | Select-Object -SkipLast 1 | ForEach-Object { Write-Host $_ }
        if ($exitCode -ne 0) {
            Write-Host "[FAIL] $($entry.Name) tests failed (exit $exitCode)" -ForegroundColor Red
            $anyFailed = $true
        }
    }
    if ($anyFailed) { throw "One or more test suites failed." }

    Write-Host ""
    Write-Host "Coverage verification (class-level, line+branch):"

    $coverageFailures = @()

    foreach ($target in $CoverageTargets) {
        $coverageFile = Get-LatestCoverageFile -ResultsDir $target.ResultsDir
        if ($null -eq $coverageFile) {
            $coverageFailures += "[$($target.Name)] No coverage.cobertura.xml found under $($target.ResultsDir)"
            continue
        }

        [xml]$coverage = Get-Content $coverageFile.FullName
        $matchingClasses = @(Get-CoverageClassesByPrefix -Coverage $coverage -Prefix $target.Prefix)

        if ($matchingClasses.Count -eq 0) {
            $coverageFailures += "[$($target.Name)] No classes matched prefix '$($target.Prefix)' in $($coverageFile.FullName)"
            continue
        }

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
    Read-Host -Prompt "Press Enter to exit"
}
