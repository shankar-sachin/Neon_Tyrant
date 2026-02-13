# ASCII Asset Storage

## Purpose
This project now includes a structured data pack for ASCII character visuals.

## Location
- `data/ascii_chars/manifest.json`
- `data/ascii_chars/sprites/*.txt`
- `data/ascii_chars/pixels/*.ppm`

## Asset Types
- Sprite text (`.txt`): character layout using ASCII symbols
- Pixel image (`.ppm`): simple pixel-art preview image (P3 portable pixmap)

## Why PPM
- Plain text format
- Easy to version and inspect in git
- No external image tool required to generate

## Naming Convention
- `player_idle`
- `enemy_patrol`
- `boss_core`
- `shard`
- `wall_tile`

## Manifest Fields
- `id`: unique asset id
- `role`: gameplay role
- `spriteText`: relative path to ASCII sprite text
- `pixelImage`: relative path to `.ppm` image
- `width` / `height`: intended grid dimensions
