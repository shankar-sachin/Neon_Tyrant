using Raylib_cs;

namespace NeonTyrant;

public sealed class SetupWizard
{
    private static readonly Color Cyan = new(0, 255, 255, 255);
    private static readonly Color DarkCyan = new(0, 180, 220, 255);
    private static readonly Color Green = new(0, 255, 136, 255);
    private static readonly Color Yellow = new(255, 255, 0, 255);
    private static readonly Color Dim = new(100, 100, 120, 255);
    private static readonly Color Magenta = new(255, 0, 255, 255);

    private static readonly string[] DifficultyNames = ["EASY", "NORMAL", "HARD"];
    private static readonly int[] DifficultyLives = [5, 3, 1];
    private static readonly string[] DifficultyDesc =
    [
        "Recommended for new pilots",
        "Standard breach protocol",
        "No margin for error",
    ];

    public GameConfig? Run()
    {
        if (!DrawWelcomeScreen()) return null;
        var selected = DrawDifficultyScreen();
        if (selected < 0) return null;
        if (!DrawControlsScreen()) return null;
        if (!DrawReadyScreen(selected)) return null;

        return new GameConfig
        {
            Difficulty = DifficultyNames[selected],
            Lives = DifficultyLives[selected],
            SetupCompleted = true,
        };
    }

    private static bool DrawWelcomeScreen()
    {
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                return true;

            PixelRenderer.BeginFrame();

            Raylib.DrawText("=== NEON TYRANT ===", 120, 40, 20, Cyan);
            Raylib.DrawText("// FIRST LAUNCH SETUP //", 130, 70, 10, DarkCyan);

            Raylib.DrawText("Welcome, pilot.", 160, 110, 10, Color.White);
            Raylib.DrawText("Before you breach the fortress,", 110, 135, 10, Color.White);
            Raylib.DrawText("let's configure your mission.", 120, 150, 10, Color.White);

            var blink = (int)(Raylib.GetTime() * 2) % 2 == 0;
            if (blink)
                Raylib.DrawText("Press ENTER to continue", 140, 210, 10, Green);

            PixelRenderer.EndFrame();
        }

        return false;
    }

    private static int DrawDifficultyScreen()
    {
        var selected = 1; // default to Normal

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Up))
                selected = Math.Max(0, selected - 1);
            if (Raylib.IsKeyPressed(KeyboardKey.S) || Raylib.IsKeyPressed(KeyboardKey.Down))
                selected = Math.Min(2, selected + 1);
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                return selected;

            PixelRenderer.BeginFrame();

            Raylib.DrawText("=== DIFFICULTY ===", 135, 30, 20, Cyan);
            Raylib.DrawText("Select threat level:", 150, 65, 10, DarkCyan);

            for (var i = 0; i < 3; i++)
            {
                var y = 100 + i * 40;
                var isSelected = i == selected;
                var marker = isSelected ? "> " : "  ";
                var nameColor = isSelected ? Cyan : Dim;
                var descColor = isSelected ? Color.White : Dim;

                Raylib.DrawText($"{marker}{DifficultyNames[i]}", 100, y, 14, nameColor);
                Raylib.DrawText($"({DifficultyLives[i]} {(DifficultyLives[i] == 1 ? "life" : "lives")})", 220, y + 2, 10, isSelected ? Yellow : Dim);
                Raylib.DrawText(DifficultyDesc[i], 120, y + 18, 10, descColor);
            }

            Raylib.DrawText("W/S or UP/DOWN to select", 120, 230, 10, Dim);
            Raylib.DrawText("ENTER to confirm", 160, 245, 10, Dim);

            PixelRenderer.EndFrame();
        }

        return -1;
    }

    private static bool DrawControlsScreen()
    {
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                return true;

            PixelRenderer.BeginFrame();

            Raylib.DrawText("=== CONTROLS ===", 140, 20, 20, Cyan);

            var y = 60;
            DrawControlRow(ref y, "Movement", "A / D");
            DrawControlRow(ref y, "Jump", "W / SPACE");
            DrawControlRow(ref y, "Attack", "W / SPACE (near enemy)");
            DrawControlRow(ref y, "Dash", "Q (cooldown applies)");
            DrawControlRow(ref y, "Pause", "ESC");

            y += 15;
            Raylib.DrawText("Collect shards for points.", 100, y, 10, Yellow);
            Raylib.DrawText("Reach the exit portal to clear levels.", 80, y + 16, 10, Green);

            var blink = (int)(Raylib.GetTime() * 2) % 2 == 0;
            if (blink)
                Raylib.DrawText("Press ENTER to continue", 140, 230, 10, Green);

            PixelRenderer.EndFrame();
        }

        return false;
    }

    private static void DrawControlRow(ref int y, string label, string keys)
    {
        Raylib.DrawText($"{label}:", 80, y, 10, Cyan);
        Raylib.DrawText(keys, 200, y, 10, Color.White);
        y += 22;
    }

    private static bool DrawReadyScreen(int difficultyIndex)
    {
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                return true;

            PixelRenderer.BeginFrame();

            Raylib.DrawText("=== MISSION READY ===", 115, 50, 20, Cyan);

            Raylib.DrawText("Difficulty:", 140, 110, 10, DarkCyan);
            Raylib.DrawText($"{DifficultyNames[difficultyIndex]} ({DifficultyLives[difficultyIndex]} {(DifficultyLives[difficultyIndex] == 1 ? "life" : "lives")})",
                230, 110, 10, Yellow);

            Raylib.DrawText("All systems operational.", 140, 145, 10, Color.White);

            var blink = (int)(Raylib.GetTime() * 2) % 2 == 0;
            if (blink)
                Raylib.DrawText("Press ENTER to deploy", 145, 200, 10, Green);

            DrawDecorativeLine(230, Magenta);

            PixelRenderer.EndFrame();
        }

        return false;
    }

    private static void DrawDecorativeLine(int y, Color color)
    {
        Raylib.DrawLine(80, y, 400, y, color);
    }
}
