Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

$output = "game_cs/bin/Release/net8.0/win-x64"

if (-not (Test-Path "$output/NeonTyrant.exe") -and -not (Test-Path "$output/NeonTyrant.dll")) {
    Write-Error "Game build not found. Run scripts/build.ps1 first."
}

if (Test-Path "$output/physics.dll") {
    Write-Host "Native physics: ON"
} else {
    Write-Host "Native physics: OFF (managed fallback)"
}

if (Test-Path "$output/NeonTyrant.exe") {
    Start-Process -FilePath "$output/NeonTyrant.exe" -WorkingDirectory $RepoRoot
} else {
    dotnet "$output/NeonTyrant.dll"
}
