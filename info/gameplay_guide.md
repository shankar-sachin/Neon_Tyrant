# Gameplay Guide

Neon Tyrant gameplay runs inside a Raylib window (`game_cs/src/Program.cs`) with keyboard input handled per frame.

## Controls
- `A` / `D`: move left/right
- `W` or `Space`: jump and attack input
- `Q`: dash burst
- `Esc`: pause/unpause

## Core Systems
- Platforming movement with gravity and collision
- Hazard, enemy, and boss damage checks
- Checkpoints and lives
- Timed levels with score bonuses

## Smoothing Features
- Jump buffering: jump input can be queued briefly before landing
- Coyote time: jump still allowed shortly after leaving a ledge
- Dash cooldown handling
- Enemy patrol turn delay to reduce jitter
- Boss movement phase behavior and motion bobbing

## Level Goal
- Collect shards (`*`) for score
- Reach checkpoint (`C`) for safer respawns
- Defeat boss (`B`) when present
- Exit through (`E`) when unlocked
