# Neon Tyrant

Windows multi-language console game with multiple levels.

## Stack
- C# (.NET 8) for game loop, rendering, level flow
- C++ DLL for physics stepping and AABB collision tests
- C utility for score persistence
- Python for level validation
- BAT scripts for Windows build/run/clean

## Prerequisites
- Windows 10/11
- .NET 8 SDK
- Python 3 (`python` in PATH)
- Optional: MSVC `cl.exe` (Visual Studio Build Tools) for native C/C++ modules


## How to Use
- Open an IDE like Visual Studio or CLion to get started
- Use the command `git clone https://github.com/atom-bowl/TyrantErr.git` to clone the repository into your local
- Go to the file `build.bat` and click Run
- Then go to file `run.bat` and click Run
- The game will be displayed in a colorful ASCII console format

## Build
```bat
scripts\build.bat
```
If `cl.exe` is missing, the game still builds and runs in managed fallback mode.

## Run
```bat
scripts\run.bat
```

## Clean
```bat
scripts\clean.bat
```

## Controls
- `A` / `D`: move
- `W` or `Space`: jump and attack
- `Esc`: pause

## Notes
- Level data is stored in `assets/levels/`.
- High scores are persisted in `data/scores.csv`.
- Native features are auto-enabled when `physics.dll` and `score_store.exe` are present.
