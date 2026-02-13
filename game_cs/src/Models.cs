using System.Text.Json.Serialization;

namespace NeonTyrant;

public sealed class LevelData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("timeLimitSec")]
    public int TimeLimitSec { get; set; } = 120;

    [JsonPropertyName("map")]
    public List<string> Map { get; set; } = [];

    [JsonPropertyName("playerSpawn")]
    public PointData PlayerSpawn { get; set; } = new();

    [JsonPropertyName("checkpoint")]
    public PointData Checkpoint { get; set; } = new();

    [JsonPropertyName("enemySpawns")]
    public List<EnemyData> EnemySpawns { get; set; } = [];

    [JsonPropertyName("boss")]
    public BossData? Boss { get; set; }
}

public sealed class PointData
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}

public sealed class EnemyData
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("left")]
    public float Left { get; set; }

    [JsonPropertyName("right")]
    public float Right { get; set; }

    [JsonPropertyName("speed")]
    public float Speed { get; set; }
}

public sealed class BossData
{
    [JsonPropertyName("x")]
    public float X { get; set; }

    [JsonPropertyName("y")]
    public float Y { get; set; }

    [JsonPropertyName("health")]
    public int Health { get; set; } = 10;

    [JsonPropertyName("phaseSpeed")]
    public List<float> PhaseSpeed { get; set; } = [1.6f, 2.7f];
}

public sealed class ScoreEntry
{
    public string Name { get; init; } = string.Empty;
    public int Score { get; init; }
    public int LevelReached { get; init; }
    public int TimeMs { get; init; }
}

public sealed class ScoreStats
{
    public int TotalRuns { get; init; }
    public int BestScore { get; init; }
    public int AverageScore { get; init; }
}

public sealed class EnemyRuntime
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Left { get; set; }
    public float Right { get; set; }
    public float Speed { get; set; }
    public int Direction { get; set; } = 1;
    public int TurnDelayMs { get; set; }
}

public sealed class LevelRuntime
{
    public required LevelData Data { get; init; }
    public required char[][] Tiles { get; init; }
    public required List<EnemyRuntime> Enemies { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public int RemainingTimeSec { get; set; }
    public bool ExitReached { get; set; }
}
