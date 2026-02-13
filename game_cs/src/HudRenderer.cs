using Raylib_cs;

namespace NeonTyrant;

public static class HudRenderer
{
    private static readonly Color Cyan = new(0, 255, 255, 255);
    private static readonly Color Red = new(255, 40, 80, 255);
    private static readonly Color Yellow = new(255, 255, 0, 255);
    private static readonly Color Green = new(0, 255, 136, 255);
    private static readonly Color Magenta = new(255, 0, 255, 255);
    private static readonly Color DarkCyan = new(0, 160, 200, 255);
    private static readonly Color DimGray = new(100, 100, 120, 255);
    private static readonly Color BossRed = new(255, 50, 50, 255);
    private static readonly Color BossBarBg = new(60, 20, 20, 255);
    private static readonly Color DashReady = new(0, 255, 136, 255);
    private static readonly Color DashCooldown = new(255, 200, 50, 255);

    public static void Draw(int levelNumber, int lives, int score, int timeLeft,
        string levelName, int bossHealth, int bossMaxHealth, int dashCooldownMs, bool nativeReady)
    {
        Raylib.DrawText($"NEON TYRANT", 4, 2, 10, Cyan);
        Raylib.DrawText($"L{levelNumber}", 100, 2, 10, Cyan);

        for (var i = 0; i < lives; i++)
            PixelRenderer.DrawHeart(130 + i * 10, 2);

        Raylib.DrawText($"Score:{score}", 170, 2, 10, Yellow);
        Raylib.DrawText($"Time:{timeLeft}s", 260, 2, 10, Green);
        Raylib.DrawText(levelName, 350, 2, 10, Magenta);

        Raylib.DrawText("A/D move  W/SPACE jump  Q dash  ESC pause", 4, 14, 10, DimGray);

        Raylib.DrawText(nativeReady ? "Native:ON" : "Native:OFF", 420, 14, 10, DarkCyan);

        if (bossMaxHealth > 0)
        {
            Raylib.DrawText("Boss:", 4, 26, 10, BossRed);
            var barX = 44;
            var barW = 80;
            var barH = 8;
            Raylib.DrawRectangle(barX, 27, barW, barH, BossBarBg);
            if (bossHealth > 0)
            {
                var fillW = (int)((float)bossHealth / bossMaxHealth * barW);
                Raylib.DrawRectangle(barX, 27, fillW, barH, BossRed);
            }
            Raylib.DrawText($"{bossHealth}/{bossMaxHealth}", barX + barW + 4, 26, 10, BossRed);
        }

        var dashX = bossMaxHealth > 0 ? 220 : 4;
        if (dashCooldownMs <= 0)
        {
            Raylib.DrawText("Dash: READY", dashX, 26, 10, DashReady);
        }
        else
        {
            Raylib.DrawText($"Dash: {dashCooldownMs / 1000.0:F1}s", dashX, 26, 10, DashCooldown);
        }
    }
}
