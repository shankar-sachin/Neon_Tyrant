@echo off
setlocal
cd /d "%~dp0.."

if exist build rmdir /s /q build
if exist game_cs\bin rmdir /s /q game_cs\bin
if exist game_cs\obj rmdir /s /q game_cs\obj

echo Clean complete.
endlocal
