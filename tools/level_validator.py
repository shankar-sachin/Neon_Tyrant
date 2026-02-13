import json
import os
import sys


REQUIRED_KEYS = {"name", "timeLimitSec", "playerSpawn", "checkpoint", "enemySpawns", "boss", "map"}


def fail(message: str) -> None:
    print(f"[level-validator] ERROR: {message}", file=sys.stderr)
    sys.exit(1)


def validate_point(label: str, point: dict, width: int, height: int) -> None:
    if not isinstance(point, dict) or "x" not in point or "y" not in point:
        fail(f"{label} must have x/y")
    x = int(point["x"])
    y = int(point["y"])
    if x < 0 or y < 0 or x >= width or y >= height:
        fail(f"{label} out of bounds ({x}, {y}) for map {width}x{height}")


def validate_file(path: str, level_index: int) -> None:
    with open(path, "r", encoding="utf-8") as f:
        data = json.load(f)

    missing = REQUIRED_KEYS.difference(data.keys())
    if missing:
        fail(f"{os.path.basename(path)} missing keys: {sorted(missing)}")

    game_map = data["map"]
    if not isinstance(game_map, list) or not game_map:
        fail(f"{os.path.basename(path)} map must be a non-empty list")
    if any(not isinstance(row, str) for row in game_map):
        fail(f"{os.path.basename(path)} map rows must be strings")

    width = max(len(row) for row in game_map)
    height = len(game_map)
    if width < 20 or height < 8:
        fail(f"{os.path.basename(path)} map too small")

    validate_point("playerSpawn", data["playerSpawn"], width, height)
    validate_point("checkpoint", data["checkpoint"], width, height)

    has_exit = any("E" in row for row in game_map)
    if not has_exit:
        fail(f"{os.path.basename(path)} must include exit tile E")

    has_collectible = any("*" in row for row in game_map)
    if not has_collectible:
        fail(f"{os.path.basename(path)} should include at least one collectible *")

    if level_index == 5:
        boss = data["boss"]
        if not boss:
            fail("level5 must define boss object")
        if boss.get("health", 0) < 1:
            fail("level5 boss.health must be > 0")
        phase = boss.get("phaseSpeed", [])
        if not isinstance(phase, list) or len(phase) < 2:
            fail("level5 boss.phaseSpeed requires at least 2 entries")


def main() -> int:
    if len(sys.argv) != 2:
        fail("Usage: python tools/level_validator.py assets/levels")

    level_dir = sys.argv[1]
    if not os.path.isdir(level_dir):
        fail(f"Directory not found: {level_dir}")

    files = sorted(
        f for f in os.listdir(level_dir)
        if f.lower().startswith("level") and f.lower().endswith(".json")
    )
    if len(files) < 5:
        fail(f"Expected at least 5 levels, found {len(files)}")

    for index, name in enumerate(files, start=1):
        validate_file(os.path.join(level_dir, name), index)

    print(f"[level-validator] OK ({len(files)} files)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
