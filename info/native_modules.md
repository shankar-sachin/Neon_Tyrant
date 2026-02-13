# Native Modules

## Overview
Neon Tyrant uses optional native modules for performance and gameplay smoothing while rendering via Raylib in a desktop window.

## C++ (`native_cpp`)
- Exposes `physics.dll` through `native_cpp/include/physics_api.h`
- Includes:
  - world stepping and jump/fall velocity
  - AABB collision and overlap tests
  - dash computation
  - jump assist (coyote + jump buffer)
  - enemy patrol update
  - boss movement update
  - screen effects (`native_cpp/src/screen_fx.cpp`)
  - procedural generation (`native_cpp/src/procgen.cpp`)

## C (`native_c`)
- `score_store.exe` handles CSV score persistence commands:
  - `load`
  - `stats`
  - `save`
- `physics.dll` now also includes:
  - particle system (`native_c/particles.c`)
  - spatial hash broad-phase (`native_c/spatial_hash.c`)

## Bridge Layer
- `game_cs/src/NativePhysicsBridge.cs` calls native APIs via P/Invoke.
- The bridge is used from the Raylib game loop in `game_cs/src/Game.cs`.
- If native binaries are absent, managed fallback logic is used.

## Build Notes
- `scripts/build.*` compiles all C++ files under `native_cpp/src/*.cpp` and links `native_c/particles.c` + `native_c/spatial_hash.c` into `physics.dll`
- Native build requires `cl.exe`
