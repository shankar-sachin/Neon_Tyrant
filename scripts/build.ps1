Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

Write-Host "[1/4] Validating levels..."
python tools/level_validator.py assets/levels
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error ".NET SDK not found. Install .NET 8 SDK and retry."
}

$hasCl = $null -ne (Get-Command cl.exe -ErrorAction SilentlyContinue)
if (-not $hasCl) {
    Write-Host "C/C++ compiler (cl.exe) not found. Native modules will be skipped."
}

New-Item -ItemType Directory -Force -Path build | Out-Null

if ($hasCl) {
    Write-Host "[2/4] Building C++ physics DLL..."
    & cl /nologo /LD /EHsc /I native_cpp\include native_cpp\src\*.cpp /link /OUT:build\physics.dll
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "[3/4] Building C score utility..."
    & cl /nologo /TC native_c\score_store.c /link /OUT:build\score_store.exe
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} else {
    Write-Host "[2/4] Skipping native C++/C build."
    Remove-Item -Force build\physics.dll -ErrorAction SilentlyContinue
    Remove-Item -Force build\score_store.exe -ErrorAction SilentlyContinue
}

Write-Host "[4/4] Building C# game..."
dotnet build game_cs/NeonTyrant.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$output = "game_cs/bin/Release/net8.0/win-x64"
if ($hasCl) {
    Copy-Item -Force build\physics.dll "$output/physics.dll"
    Copy-Item -Force build\score_store.exe "$output/score_store.exe"
    Write-Host "Build complete with native modules."
} else {
    Remove-Item -Force "$output/physics.dll" -ErrorAction SilentlyContinue
    Remove-Item -Force "$output/score_store.exe" -ErrorAction SilentlyContinue
    Write-Host "Build complete in managed fallback mode."
}
