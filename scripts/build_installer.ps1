param(
    [string]$Version = "1.0.0"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

if (-not (Get-Command wix.exe -ErrorAction SilentlyContinue)) {
    Write-Error "wix.exe not found. Install WiX Toolset 6.0.2 and ensure wix.exe is in PATH."
}

$sourceDir = Join-Path $RepoRoot "app"
if (-not (Test-Path $sourceDir)) {
    Write-Error "App folder not found at $sourceDir. Build/copy your portable output into app/ first."
}

$outDir = Join-Path $RepoRoot "build\dist"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$productWxs = Join-Path $RepoRoot "installer\Product.wxs"
$msiPath = Join-Path $outDir "NeonTyrant-$Version-x64.msi"

Write-Host "[1/2] Building MSI with wix.exe..."
& wix.exe build `
    -nologo `
    -arch x64 `
    -d SourceDir="$sourceDir" `
    -d ProductVersion="$Version" `
    -o $msiPath `
    $productWxs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[2/2] MSI complete."
Write-Host "Installer created: $msiPath"
