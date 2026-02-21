# Quick complexity analysis - top targets
param(
    [string]$ResultsDir = "Z:\repos\chronicis\TestResults"
)

$xmlFiles = Get-ChildItem -Path $ResultsDir -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending

$methods = @{}

foreach ($file in $xmlFiles) {
    [xml]$xml = Get-Content $file.FullName
    foreach ($package in $xml.coverage.packages.package) {
        foreach ($class in $package.classes.class) {
            $className = $class.name
            foreach ($method in $class.methods.method) {
                $cc = [int]$method.complexity
                $br = [double]$method.'branch-rate'
                if ($cc -lt 1) { $cc = 1 }
                $crap = [math]::Round([math]::Pow($cc, 2) * [math]::Pow(1 - $br, 3) + $cc, 0)
                $key = "$className`t$($method.name)"
                if (-not $methods.ContainsKey($key) -or $methods[$key].CrapScore -gt $crap) {
                    $methods[$key] = @{ Class=$className; Method=$method.name; CC=$cc; BR=$br; CRAP=$crap }
                }
            }
        }
    }
}

$all = $methods.Values | Sort-Object { $_.CRAP } -Descending

# Filter out noise: system-generated, ResourceCompiler, regex
$filtered = $all | Where-Object { 
    $_.Class -notmatch 'RegularExpressions' -and 
    $_.Class -notmatch 'ResourceCompiler' -and
    $_.CRAP -ge 30 -and $_.CC -ge 8
}

Write-Host "PHASE 5 TARGETS (CC>=8, CRAP>=30, excluding ResourceCompiler and regex):"
Write-Host "========================================================================="
Write-Host ""
$filtered | ForEach-Object {
    $shortClass = ($_.Class -replace 'Chronicis\.(Api|Client|Shared)\.', '') -replace '/<[^>]+>d__\d+', ''
    "{0,-70} {1,-30} CC={2,-4} BR={3,-5} CRAP={4}" -f $shortClass, $_.Method, $_.CC, ([math]::Round($_.BR, 2)), $_.CRAP
} | Select-Object -First 50

Write-Host ""
Write-Host "Total targets: $($filtered.Count)"

# Also show client targets
$clientTargets = $all | Where-Object { 
    $_.Class -match 'Chronicis\.Client' -and 
    $_.Class -notmatch 'RegularExpressions' -and
    $_.CRAP -ge 30 -and $_.CC -ge 8
}

Write-Host "`nCLIENT TARGETS:"
Write-Host "================"
$clientTargets | ForEach-Object {
    $shortClass = ($_.Class -replace 'Chronicis\.Client\.', '') -replace '/<[^>]+>d__\d+', ''
    "{0,-70} {1,-30} CC={2,-4} BR={3,-5} CRAP={4}" -f $shortClass, $_.Method, $_.CC, ([math]::Round($_.BR, 2)), $_.CRAP
} | Select-Object -First 40

Write-Host ""
Write-Host "Client targets: $($clientTargets.Count)"
