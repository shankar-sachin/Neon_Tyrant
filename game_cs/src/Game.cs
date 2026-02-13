using System.Diagnostics;

namespace NeonTyrant;

public sealed class Game
{
    private const float PlayerWidth = 0.8f;
    private const float PlayerHeight = 0.9f;
    private const float MoveSpeed = 7.4f;
    private const int FrameMs = 33;

    private readonly ScoreService _scoreService = new(AppContext.BaseDirectory);
    private readonly List<LevelData> _levels;
    private readonly bool _nativeReady;

    private int _score;
    private int _lives = 3;
    private int _totalElapsedMs;
    private int _levelsCompleted;
    private string _playerName = "PLAYER";
    private bool _supportsCursorRepaint = true;

    public Game()
    {
        _levels = LevelLoader.Load(Path.Combine(AppContext.BaseDirectory, "assets", "levels"));
        _nativeReady = NativePhysicsBridge.Initialize();
    }

    public void Run()
    {
        SafeConsole.TrySetCursorVisible(false);
        SafeConsole.TryClear();

        try
        {
            DrawIntro();

            for (var levelIndex = 0; levelIndex < _levels.Count; levelIndex++)
            {
                var result = PlayLevel(levelIndex);
                _totalElapsedMs += result.ElapsedMs;
                if (result.GameOver)
                {
                    break;
                }
                _levelsCompleted++;
            }

            _scoreService.Save(_playerName, _score, _levelsCompleted, _totalElapsedMs);
            DrawOutro();
        }
        finally
        {
            NativePhysicsBridge.Shutdown();
            SafeConsole.TrySetCursorVisible(true);
        }
    }

    private LevelResult PlayLevel(int levelIndex)
    {
        var runtime = LevelLoader.BuildRuntime(_levels[levelIndex]);
        var playerX = runtime.Data.PlayerSpawn.X + 0.05f;
        var playerY = runtime.Data.PlayerSpawn.Y + 0.05f;
        var checkpointX = runtime.Data.Checkpoint.X + 0.05f;
        var checkpointY = runtime.Data.Checkpoint.Y + 0.05f;
        var velocityY = 0f;
        var invincibleMs = 0;
        var bossX = runtime.Data.Boss?.X ?? -999f;
        var bossY = runtime.Data.Boss?.Y ?? -999f;
        var bossHealth = runtime.Data.Boss?.Health ?? 0;
        var bossDir = 1f;
        var facingDir = 1f;
        var dashCooldownMs = 0;

        var timer = Stopwatch.StartNew();
        var frameClock = Stopwatch.StartNew();
        var secondAccumulator = 0;

        while (true)
        {
            var frameStart = frameClock.ElapsedMilliseconds;
            var input = InputService.ReadFrame();
            if (input.EscapePressed)
            {
                ShowPauseScreen();
            }

            var dt = FrameMs / 1000f;
            var step = NativePhysicsBridge.Step(dt, velocityY, input.JumpPressed, IsGrounded(runtime, playerX, playerY));
            velocityY = step.VelocityY;

            var moveX = 0f;
            if (input.Left)
            {
                moveX -= MoveSpeed * dt;
                facingDir = -1f;
            }
            if (input.Right)
            {
                moveX += MoveSpeed * dt;
                facingDir = 1f;
            }

            var dashResult = NativePhysicsBridge.ComputeDash(dt, input.DashPressed, facingDir, dashCooldownMs);
            dashCooldownMs = dashResult.CooldownMs;
            moveX += dashResult.MoveX;

            var hitGround = MoveWithCollision(runtime, ref playerX, ref playerY, moveX, step.MoveY);
            if (hitGround && velocityY > 0)
            {
                velocityY = 0;
            }

            UpdateEnemies(runtime, dt);
            if (runtime.Data.Boss is not null && bossHealth > 0)
            {
                var speed = bossHealth > runtime.Data.Boss.Health / 2
                    ? runtime.Data.Boss.PhaseSpeed[0]
                    : runtime.Data.Boss.PhaseSpeed[Math.Min(1, runtime.Data.Boss.PhaseSpeed.Count - 1)];
                bossX += speed * bossDir * dt;
                var leftBound = 4f;
                var rightBound = runtime.Width - 4f;
                if (bossX < leftBound || bossX > rightBound)
                {
                    bossDir *= -1f;
                    bossX = Math.Clamp(bossX, leftBound, rightBound);
                }

                if (input.ActionPressed)
                {
                    var attack = new NativePhysicsBridge.NtAabb
                    {
                        X = playerX + 0.6f,
                        Y = playerY + 0.2f,
                        W = 1.2f,
                        H = 0.4f
                    };
                    var boss = new NativePhysicsBridge.NtAabb { X = bossX, Y = bossY, W = 1.2f, H = 1.2f };
                    if (NativePhysicsBridge.BossHit(attack, boss))
                    {
                        bossHealth = Math.Max(0, bossHealth - 1);
                        _score += 150;
                    }
                }
            }

            var playerBox = GetPlayerAabb(playerX, playerY);
            if (TryCollect(runtime, playerBox))
            {
                _score += 100;
            }
            if (TouchesCheckpoint(runtime, playerBox))
            {
                checkpointX = runtime.Data.Checkpoint.X + 0.05f;
                checkpointY = runtime.Data.Checkpoint.Y + 0.05f;
            }

            var exitUnlocked = runtime.Data.Boss is null || bossHealth == 0;
            if (TouchesTile(runtime, playerBox, 'E') && exitUnlocked)
            {
                _score += Math.Max(0, runtime.RemainingTimeSec) * 5 + 200;
                return new LevelResult(false, (int)timer.ElapsedMilliseconds);
            }

            var hitHazard = TouchesHazardTile(runtime, playerBox)
                || TouchesEnemy(runtime, playerBox)
                || (runtime.Data.Boss is not null && bossHealth > 0 &&
                    NativePhysicsBridge.Intersects(playerBox, new NativePhysicsBridge.NtAabb { X = bossX, Y = bossY, W = 1.2f, H = 1.2f }));
            if (hitHazard && invincibleMs <= 0)
            {
                _lives--;
                if (_lives <= 0)
                {
                    return new LevelResult(true, (int)timer.ElapsedMilliseconds);
                }

                playerX = checkpointX;
                playerY = checkpointY;
                velocityY = 0;
                invincibleMs = 1250;
                runtime.RemainingTimeSec = Math.Max(20, runtime.RemainingTimeSec - 4);
            }

            secondAccumulator += FrameMs;
            if (secondAccumulator >= 1000)
            {
                runtime.RemainingTimeSec--;
                secondAccumulator = 0;
                if (runtime.RemainingTimeSec <= 0)
                {
                    _lives--;
                    if (_lives <= 0)
                    {
                        return new LevelResult(true, (int)timer.ElapsedMilliseconds);
                    }

                    runtime.RemainingTimeSec = runtime.Data.TimeLimitSec;
                    playerX = checkpointX;
                    playerY = checkpointY;
                    velocityY = 0;
                }
            }

            invincibleMs -= FrameMs;
            DrawFrame(runtime, levelIndex + 1, playerX, playerY, bossX, bossY, bossHealth, dashCooldownMs, invincibleMs > 0);

            var elapsed = frameClock.ElapsedMilliseconds - frameStart;
            if (elapsed < FrameMs)
            {
                Thread.Sleep((int)(FrameMs - elapsed));
            }
        }
    }

    private bool MoveWithCollision(LevelRuntime runtime, ref float playerX, ref float playerY, float dx, float dy)
    {
        var hitGround = false;
        MoveAxis(runtime, ref playerX, ref playerY, dx, 0, ref hitGround);
        MoveAxis(runtime, ref playerX, ref playerY, 0, dy, ref hitGround);
        return hitGround;
    }

    private static void MoveAxis(LevelRuntime runtime, ref float playerX, ref float playerY, float dx, float dy, ref bool hitGround)
    {
        var steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)) / 0.1f));
        var stepX = dx / steps;
        var stepY = dy / steps;

        for (var i = 0; i < steps; i++)
        {
            var trialX = playerX + stepX;
            var trialY = playerY + stepY;
            var playerBox = new NativePhysicsBridge.NtAabb { X = playerX, Y = playerY, W = PlayerWidth, H = PlayerHeight };
            var correctedX = stepX;
            var correctedY = stepY;
            ResolveAgainstNearbySolids(runtime, playerBox, ref correctedX, ref correctedY);
            trialX = playerX + correctedX;
            trialY = playerY + correctedY;

            if (CollidesWithSolid(runtime, trialX, trialY))
            {
                if (dy > 0)
                {
                    hitGround = true;
                }
                break;
            }

            playerX = trialX;
            playerY = trialY;
        }
    }

    private static void ResolveAgainstNearbySolids(LevelRuntime runtime, NativePhysicsBridge.NtAabb player, ref float dx, ref float dy)
    {
        var minX = Math.Max(0, (int)MathF.Floor(player.X) - 1);
        var maxX = Math.Min(runtime.Width - 1, (int)MathF.Ceiling(player.X + player.W) + 1);
        var minY = Math.Max(0, (int)MathF.Floor(player.Y) - 1);
        var maxY = Math.Min(runtime.Height - 1, (int)MathF.Ceiling(player.Y + player.H) + 1);

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                if (!IsSolid(runtime.Tiles[y][x]))
                {
                    continue;
                }

                var tile = new NativePhysicsBridge.NtAabb { X = x, Y = y, W = 1f, H = 1f };
                var resolved = NativePhysicsBridge.Resolve(player, dx, dy, tile);
                dx = resolved.ResolvedDx;
                dy = resolved.ResolvedDy;
            }
        }
    }

    private static bool CollidesWithSolid(LevelRuntime runtime, float x, float y)
    {
        var left = (int)MathF.Floor(x);
        var right = (int)MathF.Floor(x + PlayerWidth);
        var top = (int)MathF.Floor(y);
        var bottom = (int)MathF.Floor(y + PlayerHeight);
        for (var iy = top; iy <= bottom; iy++)
        {
            for (var ix = left; ix <= right; ix++)
            {
                if (ix < 0 || iy < 0 || ix >= runtime.Width || iy >= runtime.Height)
                {
                    return true;
                }

                if (IsSolid(runtime.Tiles[iy][ix]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsGrounded(LevelRuntime runtime, float x, float y)
    {
        return CollidesWithSolid(runtime, x, y + 0.05f);
    }

    private static bool IsSolid(char tile) => tile is '#' or '=';

    private static void UpdateEnemies(LevelRuntime runtime, float dt)
    {
        foreach (var enemy in runtime.Enemies)
        {
            enemy.X += enemy.Speed * enemy.Direction * dt;
            if (enemy.X <= enemy.Left || enemy.X >= enemy.Right)
            {
                enemy.Direction *= -1;
                enemy.X = Math.Clamp(enemy.X, enemy.Left, enemy.Right);
            }
        }
    }

    private static bool TouchesEnemy(LevelRuntime runtime, NativePhysicsBridge.NtAabb player)
    {
        foreach (var enemy in runtime.Enemies)
        {
            var enemyBox = new NativePhysicsBridge.NtAabb { X = enemy.X, Y = enemy.Y, W = 0.9f, H = 0.9f };
            if (NativePhysicsBridge.Intersects(player, enemyBox))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TouchesHazardTile(LevelRuntime runtime, NativePhysicsBridge.NtAabb player)
    {
        for (var y = 0; y < runtime.Height; y++)
        {
            for (var x = 0; x < runtime.Width; x++)
            {
                if (runtime.Tiles[y][x] != '^')
                {
                    continue;
                }

                var hazard = new NativePhysicsBridge.NtAabb { X = x, Y = y, W = 1f, H = 1f };
                if (NativePhysicsBridge.Intersects(player, hazard))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryCollect(LevelRuntime runtime, NativePhysicsBridge.NtAabb player)
    {
        for (var y = 0; y < runtime.Height; y++)
        {
            for (var x = 0; x < runtime.Width; x++)
            {
                if (runtime.Tiles[y][x] != '*')
                {
                    continue;
                }

                var shard = new NativePhysicsBridge.NtAabb { X = x, Y = y, W = 0.9f, H = 0.9f };
                if (NativePhysicsBridge.Intersects(player, shard))
                {
                    runtime.Tiles[y][x] = ' ';
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TouchesCheckpoint(LevelRuntime runtime, NativePhysicsBridge.NtAabb player) => TouchesTile(runtime, player, 'C');

    private static bool TouchesTile(LevelRuntime runtime, NativePhysicsBridge.NtAabb player, char marker)
    {
        for (var y = 0; y < runtime.Height; y++)
        {
            for (var x = 0; x < runtime.Width; x++)
            {
                if (runtime.Tiles[y][x] != marker)
                {
                    continue;
                }

                var box = new NativePhysicsBridge.NtAabb { X = x, Y = y, W = 1f, H = 1f };
                if (NativePhysicsBridge.Intersects(player, box))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static NativePhysicsBridge.NtAabb GetPlayerAabb(float x, float y) => new()
    {
        X = x,
        Y = y,
        W = PlayerWidth,
        H = PlayerHeight
    };

    private void DrawFrame(LevelRuntime runtime, int levelNumber, float playerX, float playerY, float bossX, float bossY, int bossHealth, int dashCooldownMs, bool blinkPlayer)
    {
        if (_supportsCursorRepaint)
        {
            if (!SafeConsole.TrySetCursorPosition(0, 0))
            {
                _supportsCursorRepaint = false;
                SafeConsole.TryClear();
            }
        }
        else
        {
            SafeConsole.TryClear();
        }
        HudRenderer.DrawTopBar(levelNumber, _lives, _score, runtime.RemainingTimeSec, runtime.Data.Name);
        HudRenderer.DrawLegend();
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine(_nativeReady ? "Native bridge: ON" : "Native bridge: OFF (managed fallback)");
        Console.ResetColor();

        var buffer = runtime.Tiles.Select(row => row.ToArray()).ToArray();
        foreach (var enemy in runtime.Enemies)
        {
            var ex = (int)MathF.Round(enemy.X);
            var ey = (int)MathF.Round(enemy.Y);
            if (InBounds(runtime, ex, ey))
            {
                buffer[ey][ex] = 'M';
            }
        }

        if (runtime.Data.Boss is not null && bossHealth > 0)
        {
            var bx = (int)MathF.Round(bossX);
            var by = (int)MathF.Round(bossY);
            if (InBounds(runtime, bx, by))
            {
                buffer[by][bx] = 'B';
            }
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Boss HP: {bossHealth}");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("Boss HP: --");
        }

        if (dashCooldownMs <= 0)
        {
            Console.WriteLine("Dash: READY");
        }
        else
        {
            Console.WriteLine($"Dash: {dashCooldownMs / 1000.0:F1}s");
        }

        var px = (int)MathF.Round(playerX);
        var py = (int)MathF.Round(playerY);
        if (InBounds(runtime, px, py) && !blinkPlayer)
        {
            buffer[py][px] = '@';
        }

        foreach (var row in buffer)
        {
            for (var i = 0; i < row.Length; i++)
            {
                DrawTile(row[i]);
            }
            Console.WriteLine();
            Console.ResetColor();
        }
    }

    private static bool InBounds(LevelRuntime runtime, int x, int y) => x >= 0 && y >= 0 && x < runtime.Width && y < runtime.Height;

    private static void DrawTile(char tile)
    {
        Console.ForegroundColor = tile switch
        {
            '#' or '=' => ConsoleColor.DarkGray,
            '^' => ConsoleColor.Red,
            '*' => ConsoleColor.Yellow,
            'C' => ConsoleColor.Green,
            'E' => ConsoleColor.Cyan,
            '@' => ConsoleColor.White,
            'M' => ConsoleColor.Magenta,
            'B' => ConsoleColor.DarkRed,
            _ => ConsoleColor.Gray
        };
        Console.Write(tile);
    }

    private void DrawIntro()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== NEON TYRANT // CYBER FORTRESS BREACH ===");
        Console.ResetColor();
        Console.WriteLine("Enter pilot tag:");
        var input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            _playerName = input.Trim();
        }
        Console.WriteLine("Press ENTER to deploy.");
        Console.ReadLine();
    }

    private void DrawOutro()
    {
        SafeConsole.TryClear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== MISSION REPORT ===");
        Console.ResetColor();
        Console.WriteLine($"Pilot: {_playerName}");
        Console.WriteLine($"Score: {_score}");
        Console.WriteLine($"Levels Cleared: {_levelsCompleted} / {_levels.Count}");
        Console.WriteLine($"Time: {_totalElapsedMs / 1000.0:F1}s");
        var stats = _scoreService.TryLoadStats();
        if (stats is not null)
        {
            Console.WriteLine($"Career: Runs {stats.TotalRuns} | Best {stats.BestScore} | Avg {stats.AverageScore}");
        }
        Console.WriteLine();
        Console.WriteLine("Top Scores:");
        var top = _scoreService.LoadTop();
        if (top.Count == 0)
        {
            Console.WriteLine("  (no scores yet)");
        }
        else
        {
            for (var i = 0; i < top.Count; i++)
            {
                var s = top[i];
                Console.WriteLine($"  {i + 1}. {s.Name,-10} {s.Score,6} pts  L{s.LevelReached}  {s.TimeMs / 1000.0:F1}s");
            }
        }
        Console.WriteLine();
        Console.WriteLine("Press ENTER to exit.");
        Console.ReadLine();
    }

    private void ShowPauseScreen()
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("PAUSED - Press ESC to continue");
        Console.ResetColor();
        while (true)
        {
            if (!SafeConsole.TryKeyAvailable(out var available))
            {
                Thread.Sleep(20);
                continue;
            }

            if (!available)
            {
                Thread.Sleep(20);
                continue;
            }

            if (!SafeConsole.TryReadKey(intercept: true, out var keyInfo))
            {
                Thread.Sleep(20);
                continue;
            }

            var key = keyInfo.Key;
            if (key == ConsoleKey.Escape)
            {
                break;
            }
        }
    }

    private readonly record struct LevelResult(bool GameOver, int ElapsedMs);
}
