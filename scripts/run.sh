#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

OUTPUT="game_cs/bin/Release/net8.0"
if [ ! -f "$OUTPUT/NeonTyrant.dll" ]; then
  echo "Game build not found. Run scripts/build.sh first."
  exit 1
fi

if [ -f "$OUTPUT/physics.dll" ]; then
  echo "Native physics: ON"
else
  echo "Native physics: OFF (managed fallback)"
fi

if [ -f "$OUTPUT/score_store.exe" ]; then
  echo "Native score utility: ON"
else
  echo "Native score utility: OFF (scores not persisted)"
fi

dotnet "$OUTPUT/NeonTyrant.dll"
