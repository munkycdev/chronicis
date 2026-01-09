$ErrorActionPreference = "Stop"

Write-Host "Stopping local dev processes..."

# ---- Helper: stop processes by predicate ----
function Stop-MatchingProcesses {
  param(
    [string]$Description,
    [ScriptBlock]$Match
  )

  $procs = Get-Process | Where-Object $Match

  if (-not $procs) {
    Write-Host "  No $Description processes found."
    return
  }

  foreach ($p in $procs) {
    try {
      Write-Host "  Stopping $Description (PID $($p.Id), $($p.ProcessName))"
      Stop-Process -Id $p.Id -Force
    }
    catch {
      Write-Warning "  Failed to stop PID $($p.Id): $($_.Exception.Message)"
    }
  }
}

# ---- Stop Azure Functions host ----
# func.exe usually runs as a node process underneath
Stop-MatchingProcesses -Description "Azure Functions host" -Match {
  $_.ProcessName -in @("func", "func.exe", "node", "node.exe") -and
  $_.Path -and
  $_.Path -match "azure[-_]?functions"
}

# ---- Stop dotnet run web apps ----
Stop-MatchingProcesses -Description "dotnet web app" -Match {
  $_.ProcessName -in @("dotnet", "dotnet.exe")
}

Write-Host ""
Write-Host "Done. If something is still running, it was likely not started by this repo."
