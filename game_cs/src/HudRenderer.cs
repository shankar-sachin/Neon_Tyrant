namespace NeonTyrant;

public static class HudRenderer
{
    public static void DrawTopBar(int levelNumber, int lives, int score, int timeLeft, string levelName)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[NEON TYRANT] L{levelNumber} ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"Lives:{lives} ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"Score:{score} ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write($"Time:{timeLeft}s ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write(levelName);
        Console.ResetColor();
        Console.WriteLine();
        var width = SafeConsole.GetWindowWidthOrDefault();
        Console.WriteLine(new string('-', Math.Max(width - 1, 60)));
    }

    public static void DrawLegend()
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("Controls: ");
        Console.ResetColor();
        Console.Write("A/D move | W/SPACE jump+attack | Q dash | ESC pause");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Tiles: # wall  ^ hazard  * data shard  C checkpoint  E exit  M drone  B boss");
        Console.ResetColor();
    }
}
