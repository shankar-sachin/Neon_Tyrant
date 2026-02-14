@echo off
setlocal

set VERSION=%1
if "%VERSION%"=="" set VERSION=1.0.0

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build_installer.ps1" -Version "%VERSION%"
exit /b %ERRORLEVEL%
