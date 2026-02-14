using System.Text.Json;
using System.Text.Json.Serialization;

namespace NeonTyrant;

public sealed class GameConfig
{
    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "Normal";

    [JsonPropertyName("lives")]
    public int Lives { get; set; } = 3;

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = "";

    [JsonPropertyName("setupCompleted")]
    public bool SetupCompleted { get; set; }

    private static readonly string FilePath = AppPaths.ConfigPath;

    public static GameConfig? TryLoad()
    {
        if (!File.Exists(FilePath))
            return null;
        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<GameConfig>(json);
        }
        catch
        {
            return null;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
