using System.Diagnostics;

namespace NeonTyrant;

public sealed class ScoreService
{
    private readonly string _exePath;
    private readonly string _scoreFilePath;

    public ScoreService(string baseDir)
    {
        _exePath = Path.Combine(baseDir, "score_store.exe");
        _scoreFilePath = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "data", "scores.csv"));
        Directory.CreateDirectory(Path.GetDirectoryName(_scoreFilePath)!);
    }

    public List<ScoreEntry> LoadTop(int top = 5)
    {
        if (!File.Exists(_exePath))
        {
            return [];
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = $"load \"{_scoreFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(2000);
        if (process.ExitCode != 0)
        {
            return [];
        }

        var scores = new List<ScoreEntry>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                continue;
            }

            if (!int.TryParse(parts[1], out var score) ||
                !int.TryParse(parts[2], out var level) ||
                !int.TryParse(parts[3], out var timeMs))
            {
                continue;
            }

            scores.Add(new ScoreEntry
            {
                Name = parts[0],
                Score = score,
                LevelReached = level,
                TimeMs = timeMs
            });
        }

        return scores
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.TimeMs)
            .Take(top)
            .ToList();
    }

    public void Save(string name, int score, int levelReached, int timeMs)
    {
        if (!File.Exists(_exePath))
        {
            return;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _exePath,
                Arguments = $"save \"{_scoreFilePath}\" \"{Sanitize(name)}\" {score} {levelReached} {timeMs}",
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit(2000);
    }

    private static string Sanitize(string value)
    {
        var cleaned = new string(value.Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "PLAYER" : cleaned.ToUpperInvariant();
    }
}
