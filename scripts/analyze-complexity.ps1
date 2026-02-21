# Analyze complexity from Cobertura coverage files
param(
    [string]$ResultsDir = "Z:\repos\chronicis\TestResults"
)

$xmlFiles = Get-ChildItem -Path $ResultsDir -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending

$methods = @()

foreach ($file in $xmlFiles) {
    [xml]$xml = Get-Content $file.FullName
    foreach ($package in $xml.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            $className = $class.name
            foreach ($method in $class.methods.method) {
                $cc = [int]$method.complexity
                $branchRate = [double]$method.'branch-rate'
                $lineRate = [double]$method.'line-rate'
                if ($cc -lt 1) { $cc = 1 }
                
                # CRAP = CC^2 * (1 - coverage)^3 + CC
                $crap = [math]::Pow($cc, 2) * [math]::Pow(1 - $branchRate, 3) + $cc
                $crap = [math]::Round($crap, 0)

                $key = "$className|$($method.name)|$cc|$branchRate"
                $methods += [PSCustomObject]@{
                    Class = $className
                    Method = $method.name
                    Complexity = $cc
                    BranchRate = $branchRate
                    CrapScore = $crap
                }
            }
        }
    }
}

# Deduplicate (same method may appear in multiple coverage files)
$unique = $methods | Sort-Object { "$($_.Class)|$($_.Method)" } -Unique

Write-Host "`nTOTAL METHODS: $($unique.Count)"
Write-Host "`nCOMPLEXITY DISTRIBUTION:"
Write-Host "  CC 1 : $(($unique | Where-Object { $_.Complexity -eq 1 }).Count)"
Write-Host "  CC 2-3 : $(($unique | Where-Object { $_.Complexity -ge 2 -and $_.Complexity -le 3 }).Count)"
Write-Host "  CC 4-5 : $(($unique | Where-Object { $_.Complexity -ge 4 -and $_.Complexity -le 5 }).Count)"
Write-Host "  CC 6-7 : $(($unique | Where-Object { $_.Complexity -ge 6 -and $_.Complexity -le 7 }).Count)"
Write-Host "  CC 8-10 : $(($unique | Where-Object { $_.Complexity -ge 8 -and $_.Complexity -le 10 }).Count)"
Write-Host "  CC 11+ : $(($unique | Where-Object { $_.Complexity -ge 11 }).Count)"

Write-Host "`nTOP 30 BY CRAP SCORE:"
$unique | Sort-Object CrapScore -Descending | Select-Object -First 30 | Format-Table Class, Method, CrapScore, Complexity, BranchRate -AutoSize

Write-Host "`nTOP 30 BY CYCLOMATIC COMPLEXITY:"
$unique | Sort-Object Complexity -Descending | Select-Object -First 30 | Format-Table Class, Method, CrapScore, Complexity, BranchRate -AutoSize

Write-Host "`nMETHODS WITH CC >= 8 AND CRAP >= 30 (Phase 5 targets):"
$targets = $unique | Where-Object { $_.Complexity -ge 8 -and $_.CrapScore -ge 30 } | Sort-Object CrapScore -Descending
$targets | Format-Table Class, Method, CrapScore, Complexity, BranchRate -AutoSize
Write-Host "Count: $($targets.Count)"
