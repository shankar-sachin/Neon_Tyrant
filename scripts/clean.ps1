Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

Remove-Item -Recurse -Force build -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force game_cs/bin -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force game_cs/obj -ErrorAction SilentlyContinue

Write-Host "Clean complete."
