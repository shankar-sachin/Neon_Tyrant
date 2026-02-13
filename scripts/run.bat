@echo off
setlocal
cd /d "%~dp0.."

set OUTPUT=game_cs\bin\Release\net8.0
if not exist %OUTPUT%\NeonTyrant.dll (
  echo Game build not found. Run scripts\build.bat first.
  exit /b 1
)

if exist %OUTPUT%\physics.dll (
  echo Native physics: ON
) else (
  echo Native physics: OFF ^(managed fallback^)
)

if exist %OUTPUT%\score_store.exe (
  echo Native score utility: ON
) else (
  echo Native score utility: OFF ^(scores not persisted^)
)

dotnet %OUTPUT%\NeonTyrant.dll
endlocal
