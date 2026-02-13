#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "[1/4] Validating levels..."
python tools/level_validator.py assets/levels

if ! command -v dotnet >/dev/null 2>&1; then
  echo ".NET SDK not found. Install .NET 8 SDK and retry."
  exit 1
fi

HAS_CL=1
if ! command -v cl >/dev/null 2>&1; then
  HAS_CL=0
  echo "C/C++ compiler (cl.exe) not found. Native modules will be skipped."
fi

mkdir -p build

if [ "$HAS_CL" -eq 1 ]; then
  echo "[2/4] Building C++ physics DLL..."
  cl /nologo /LD /EHsc /I native_cpp\\include native_cpp\\src\\*.cpp /link /OUT:build\\physics.dll

  echo "[3/4] Building C score utility..."
  cl /nologo /TC native_c\\score_store.c /link /OUT:build\\score_store.exe
else
  echo "[2/4] Skipping native C++/C build."
  rm -f build/physics.dll build/score_store.exe
fi

echo "[4/4] Building C# game..."
dotnet build game_cs/NeonTyrant.csproj -c Release

OUTPUT="game_cs/bin/Release/net8.0"
if [ "$HAS_CL" -eq 1 ]; then
  cp -f build/physics.dll "$OUTPUT/physics.dll"
  cp -f build/score_store.exe "$OUTPUT/score_store.exe"
  echo "Build complete with native modules."
else
  rm -f "$OUTPUT/physics.dll" "$OUTPUT/score_store.exe"
  echo "Build complete in managed fallback mode."
fi
