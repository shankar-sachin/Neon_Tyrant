# Native Modules

## Overview
Neon Tyrant uses optional native modules for performance and gameplay smoothing.

## C++ (`native_cpp`)
- Exposes `physics.dll` through `native_cpp/include/physics_api.h`
- Includes:
  - world stepping and jump/fall velocity
  - AABB collision and overlap tests
  - dash computation
  - jump assist (coyote + jump buffer)
  - enemy patrol update
  - boss movement update

## C (`native_c`)
- `score_store.exe` handles CSV score persistence commands:
  - `load`
  - `stats`
  - `save`

## Bridge Layer
- `game_cs/src/NativePhysicsBridge.cs` calls native APIs via P/Invoke.
- If native binaries are absent, managed fallback logic is used.

## Build Notes
- `scripts/build.*` compiles all C++ files under `native_cpp/src/*.cpp`
- Native build requires `cl.exe`
