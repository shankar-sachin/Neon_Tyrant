# Neon Tyrant Docs Index

This folder contains project documentation for setup, gameplay, native modules, and asset storage for the Raylib window build.

## Core Docs
- `info/clone.md`: clone + first-time setup instructions
- `README.md`: project overview and primary build/run commands
- `info/gameplay_guide.md`: controls, level flow, and gameplay systems
- `info/native_modules.md`: C/C++ native module architecture
- `info/ascii_assets.md`: format and usage of ASCII character pixel assets

## Project Structure (Quick Map)
- `game_cs/src/`: main game loop and Raylib rendering/input in C#
- `native_cpp/`: physics + gameplay smoothing helpers in C++
- `native_c/`: score persistence helper in C
- `assets/levels/`: level JSON files
- `data/`: runtime and stored data (scores and ASCII sprite pack)

## Notes
- Native gameplay enhancements are used when `physics.dll` is present.
- Managed fallbacks stay active if native binaries are unavailable.
- The Raylib window file is `game_cs/src/Program.cs`.
