# Neon Tyrant

Windows multi-language platformer running in a Raylib window.

![Demo](data/images/demo.gif)

## How to Use
- Go to [landing page](https://tinyurl.com/neontyrant)
- Go to [latest release page](https://github.com/shankar-sachin/neon-tyrant/releases/tag/v1.0.1)
- Click download and double-click the installer to download application

## Stack
- C# (.NET 8) for game loop and level flow
- Raylib-cs (`Raylib-cs` NuGet package) for window creation, drawing, and input
- C++ DLL for physics stepping and AABB collision tests
- C utility for score persistence
- Python for level validation
- BAT scripts for Windows build/run/clean
- PowerShell and shell scripts for smoother local workflow

## Prerequisites
- Windows 10/11
- Note: All you need is Windows 10 or 11 if you're going to download the app, if you're building and running locally, you will need the prequisites listed below
- .NET 8 SDK
- Python 3 (`python` in PATH)
- Optional: MSVC `cl.exe` (Visual Studio Build Tools) for native C/C++ modules


## How to Play

### Option 1: Run the .msi (no setup required)
Go to the [landing page](https://tinyurl.com/neon-tyrant) or the [latest release notes](https://github.com/shankar-sachin/neon-tyrant/releases/tag/v1.0.1) to download the `.msi` application. Double click it and follow the steps on the setup wizard.

### Option 1.5: Install with WiX (complicated and useless way, just download the MSI from the [landing page](https://www.tinyurl.com/neon-tyrant))
If you want Neon Tyrant as a normal installed Windows app (Start Menu/Desktop shortcuts + Add/Remove Programs entry), build the MSI installer with WiX:

#### WiX Toolset Way

- Install **WiX Toolset 6.0.2** and ensure `wix.exe` is in `PATH`
- Ensure your portable app files are in `app/` (including `NeonTyrant.exe`)
- Run:

```bat
scripts\build_installer.bat 1.0.0
```

PowerShell:
```powershell
./scripts/build_installer.ps1 -Version 1.0.0
```

Installer output:
- `build/dist/NeonTyrant-1.0.0-x64.msi`

### Option 2: Build and run locally
- Use `git clone https://github.com/atom-bowl/TyrantErr.git` to clone the repository
- Run `scripts\build.bat` to build the project
- Run `scripts\run.bat` to launch the game
- The game opens as a Raylib desktop window (`game_cs/src/Program.cs`)

#### Build
```bat
scripts\build.bat
```
If `cl.exe` is missing, the game still builds and runs in managed fallback mode.

PowerShell:
```powershell
./scripts/build.ps1
```

Shell:
```bash
./scripts/build.sh
```

#### Run
```bat
scripts\run.bat
```
PowerShell: `./scripts/run.ps1`  
Shell: `./scripts/run.sh`

#### Clean
```bat
scripts\clean.bat
```
PowerShell: `./scripts/clean.ps1`  
Shell: `./scripts/clean.sh`

## Controls
- `A` / `D`: move
- `W` or `Space`: jump and attack
- `Q`: dash burst (cooldown based)
- `Esc`: pause

## Notes
- Level data is stored in `assets/levels/`.
- High scores are persisted in `data/scores.csv`.
- Native features are auto-enabled when `physics.dll` and `score_store.exe` are present.
- The main window entrypoint is `game_cs/src/Program.cs` (`Raylib.InitWindow(...)`).

*Last updated Feb. 14, 2026 at 2:20 PM*
