using System.Text.Json;

namespace NeonTyrant;

public static class LevelLoader
{
    public static List<LevelData> Load(string levelsDirectory)
    {
        if (!Directory.Exists(levelsDirectory))
        {
            throw new DirectoryNotFoundException($"Missing level directory: {levelsDirectory}");
        }

        var files = Directory.GetFiles(levelsDirectory, "level*.json")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (files.Count == 0)
        {
            throw new InvalidOperationException("No levels found.");
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var levels = new List<LevelData>(files.Count);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var level = JsonSerializer.Deserialize<LevelData>(text, options)
                ?? throw new InvalidOperationException($"Invalid level JSON: {file}");
            if (level.Map.Count == 0)
            {
                throw new InvalidOperationException($"Level has no map rows: {file}");
            }

            levels.Add(level);
        }

        return levels;
    }

    public static LevelRuntime BuildRuntime(LevelData level)
    {
        var height = level.Map.Count;
        var width = level.Map.Max(row => row.Length);
        var tiles = new char[height][];
        for (var y = 0; y < height; y++)
        {
            var row = level.Map[y].PadRight(width, ' ');
            tiles[y] = row.ToCharArray();
        }

        var enemies = level.EnemySpawns.Select(e => new EnemyRuntime
        {
            X = e.X,
            Y = e.Y,
            Left = e.Left,
            Right = e.Right,
            Speed = e.Speed
        }).ToList();

        return new LevelRuntime
        {
            Data = level,
            Tiles = tiles,
            Enemies = enemies,
            Width = width,
            Height = height,
            RemainingTimeSec = level.TimeLimitSec
        };
    }
}
