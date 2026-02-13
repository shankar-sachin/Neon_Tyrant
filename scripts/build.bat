@echo off
setlocal
cd /d "%~dp0.."

echo [1/4] Validating levels...
python tools\level_validator.py assets\levels
if not "%ERRORLEVEL%"=="0" exit /b %ERRORLEVEL%

for /f %%i in ('dotnet --list-sdks') do set HAS_DOTNET_SDK=1
if not defined HAS_DOTNET_SDK (
  echo .NET SDK not found. Install .NET 8 SDK and retry.
  exit /b 1
)

set HAS_CL=1
where cl >nul 2>nul
if errorlevel 1 (
  set HAS_CL=0
  echo C/C++ compiler ^(cl.exe^) not found. Native modules will be skipped.
)

if not exist build mkdir build

if "%HAS_CL%"=="1" (
  echo [2/4] Building C++ physics DLL...
  cl /nologo /LD /EHsc /I native_cpp\include native_cpp\src\*.cpp native_c\particles.c native_c\spatial_hash.c /link /OUT:build\physics.dll
  if not "%ERRORLEVEL%"=="0" exit /b %ERRORLEVEL%

  echo [3/4] Building C score utility...
  cl /nologo /TC native_c\score_store.c /link /OUT:build\score_store.exe
  if not "%ERRORLEVEL%"=="0" exit /b %ERRORLEVEL%
) else (
  echo [2/4] Skipping native C++/C build.
  if exist build\physics.dll del /q build\physics.dll >nul 2>nul
  if exist build\score_store.exe del /q build\score_store.exe >nul 2>nul
)

echo [4/4] Building C# game...
dotnet build game_cs\NeonTyrant.csproj -c Release
if not "%ERRORLEVEL%"=="0" exit /b %ERRORLEVEL%

set OUTPUT=game_cs\bin\Release\net8.0\win-x64
if "%HAS_CL%"=="1" (
  copy /Y build\physics.dll %OUTPUT%\physics.dll >nul
  copy /Y build\score_store.exe %OUTPUT%\score_store.exe >nul
 ) else (
  if exist %OUTPUT%\physics.dll del /q %OUTPUT%\physics.dll >nul 2>nul
  if exist %OUTPUT%\score_store.exe del /q %OUTPUT%\score_store.exe >nul 2>nul
)

if "%HAS_CL%"=="1" (
  echo Build complete with native modules.
) else (
  echo Build complete in managed fallback mode.
)
endlocal
