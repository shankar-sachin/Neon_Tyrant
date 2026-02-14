using System.Diagnostics;
using Raylib_cs;

namespace NeonTyrant;

public sealed class Game
{
    private const float PlayerWidth = 0.8f;
    private const float PlayerHeight = 0.9f;
    private const float MoveSpeed = 7.4f;
    private const float FatalLandingVelocity = 13.5f;

    private readonly ScoreService _scoreService = new(AppContext.BaseDirectory);
    private readonly GameConfig _config;
    private readonly List<LevelData> _levels;
    private readonly bool _nativeReady;

    private int _score;
    private int _lives;
    private int _totalElapsedMs;
    private int _levelsCompleted;
    private string _playerName = "PLAYER";

    public Game(GameConfig config)
    {
        _config = config;
        _lives = config.Lives;
        if (!string.IsNullOrWhiteSpace(config.PlayerName))
            _playerName = config.PlayerName;
        _levels = LevelLoader.Load(Path.Combine(AppContext.BaseDirectory, "assets", "levels"));
        _nativeReady = NativePhysicsBridge.Initialize();
    }

    public void Run()
    {
        try
        {
            DrawIntro();

            for (var levelIndex = 0; levelIndex < _levels.Count && !Raylib.WindowShouldClose(); levelIndex++)
            {
                var result = PlayLevel(levelIndex);
                _totalElapsedMs += result.ElapsedMs;
                if (result.GameOver)
                {
                    break;
                }
                _levelsCompleted++;
            }

            if (!Raylib.WindowShouldClose())
            {
                _scoreService.Save(_playerName, _score, _levelsCompleted, _totalElapsedMs);
                DrawOutro();
            }
        }
        finally
        {
            NativePhysicsBridge.Shutdown();
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
        var bossBaseY = runtime.Data.Boss?.Y ?? -999f;
        var bossY = bossBaseY;
        var bossHealth = runtime.Data.Boss?.Health ?? 0;
        var bossDir = 1f;
        var facingDir = 1f;
        var dashCooldownMs = 0;
        var coyoteMs = 0;
        var jumpBufferMs = 0;
        var bossMotionElapsedMs = 0;

        var timer = Stopwatch.StartNew();
        var secondAccumulator = 0;

        while (!Raylib.WindowShouldClose())
        {
            var dt = Math.Clamp(Raylib.GetFrameTime(), 0.001f, 0.1f);
            var frameMs = (int)(dt * 1000);

            var input = InputService.ReadFrame();
            if (input.EscapePressed)
            {
                ShowPauseScreen();
            }

            var groundedNow = IsGrounded(runtime, playerX, playerY);
            var jumpAssist = NativePhysicsBridge.UpdateJumpAssist(dt, input.JumpPressed, groundedNow, coyoteMs, jumpBufferMs);
            coyoteMs = jumpAssist.CoyoteMs;
            jumpBufferMs = jumpAssist.JumpBufferMs;
            var step = NativePhysicsBridge.Step(dt, velocityY, jumpAssist.ConsumeJump == 1, groundedNow);
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
            var tookFatalLanding = !groundedNow && hitGround && step.VelocityY >= FatalLandingVelocity;
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
                bossMotionElapsedMs += frameMs;
                var leftBound = 4f;
                var rightBound = runtime.Width - 4f;
                var bossStep = NativePhysicsBridge.UpdateBossMotion(
                    dt,
                    bossX,
                    bossBaseY,
                    bossDir,
                    leftBound,
                    rightBound,
                    speed,
                    bossHealth,
                    runtime.Data.Boss.Health,
                    bossMotionElapsedMs);
                bossX = bossStep.X;
                bossY = bossStep.Y;
                bossDir = bossStep.Dir;

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
            var tookDamage = tookFatalLanding || (hitHazard && invincibleMs <= 0);
            if (tookDamage)
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

            secondAccumulator += frameMs;
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

            invincibleMs = Math.Max(0, invincibleMs - frameMs);

            var blinkPlayer = invincibleMs > 0 && ((int)(timer.ElapsedMilliseconds / 100) % 2 == 0);
            DrawFrame(runtime, levelIndex + 1, playerX, playerY, bossX, bossY, bossHealth, dashCooldownMs, blinkPlayer);
        }

        return new LevelResult(true, (int)timer.ElapsedMilliseconds);
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
            var playerBox = new NativePhysicsBridge.NtAabb { X = playerX, Y = playerY, W = PlayerWidth, H = PlayerHeight };
            var correctedX = stepX;
            var correctedY = stepY;
            ResolveAgainstNearbySolids(runtime, playerBox, ref correctedX, ref correctedY);
            var trialX = playerX + correctedX;
            var trialY = playerY + correctedY;

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
            var state = new NativePhysicsBridge.NtEnemyState
            {
                X = enemy.X,
                Left = enemy.Left,
                Right = enemy.Right,
                Speed = enemy.Speed,
                Direction = enemy.Direction,
                TurnDelayMs = enemy.TurnDelayMs
            };
            var next = NativePhysicsBridge.UpdateEnemyPatrol(dt, state);
            enemy.X = next.X;
            enemy.Direction = next.Direction;
            enemy.TurnDelayMs = next.TurnDelayMs;
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
        PixelRenderer.BeginFrame();

        for (var y = 0; y < runtime.Height; y++)
            for (var x = 0; x < runtime.Width; x++)
                PixelRenderer.DrawTile(x, y, runtime.Tiles[y][x]);

        foreach (var enemy in runtime.Enemies)
        {
            var ex = (int)MathF.Round(enemy.X);
            var ey = (int)MathF.Round(enemy.Y);
            if (InBounds(runtime, ex, ey))
                PixelRenderer.DrawTile(ex, ey, 'M');
        }

        if (runtime.Data.Boss is not null && bossHealth > 0)
        {
            var bx = (int)MathF.Round(bossX);
            var by = (int)MathF.Round(bossY);
            if (InBounds(runtime, bx, by))
                PixelRenderer.DrawTile(bx, by, 'B');
        }

        if (!blinkPlayer)
        {
            var px = (int)MathF.Round(playerX);
            var py = (int)MathF.Round(playerY);
            if (InBounds(runtime, px, py))
                PixelRenderer.DrawTile(px, py, '@');
        }

        HudRenderer.Draw(levelNumber, _lives, _score, runtime.RemainingTimeSec,
            runtime.Data.Name, bossHealth, runtime.Data.Boss?.Health ?? 0,
            dashCooldownMs, _nativeReady);

        PixelRenderer.EndFrame();
    }

    private static bool InBounds(LevelRuntime runtime, int x, int y) => x >= 0 && y >= 0 && x < runtime.Width && y < runtime.Height;

    private void DrawIntro()
    {
        var nameBuffer = string.IsNullOrWhiteSpace(_config.PlayerName) ? "" : _config.PlayerName;
        var enteringName = true;

        while (!Raylib.WindowShouldClose())
        {
            if (enteringName)
            {
                int ch;
                while ((ch = Raylib.GetCharPressed()) != 0)
                {
                    if (nameBuffer.Length < 20 && ch >= 32 && ch < 127)
                        nameBuffer += (char)ch;
                }
                if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && nameBuffer.Length > 0)
                    nameBuffer = nameBuffer[..^1];
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                {
                    if (!string.IsNullOrWhiteSpace(nameBuffer))
                        _playerName = nameBuffer.Trim();
                    enteringName = false;
                }
            }
            else
            {
                if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                    break;
            }

            PixelRenderer.BeginFrame();

            Raylib.DrawText("=== NEON TYRANT ===", 120, 50, 20, new Color(0, 255, 255, 255));
            Raylib.DrawText("// CYBER FORTRESS BREACH //", 110, 80, 10, new Color(0, 180, 220, 255));

            if (enteringName)
            {
                Raylib.DrawText("Enter pilot tag:", 160, 130, 10, Color.White);
                var cursor = ((int)(Raylib.GetTime() * 3) % 2 == 0) ? "_" : " ";
                Raylib.DrawText(nameBuffer + cursor, 160, 150, 10, new Color(0, 255, 136, 255));
            }
            else
            {
                Raylib.DrawText($"Pilot: {_playerName}", 160, 130, 10, new Color(0, 255, 136, 255));
                Raylib.DrawText("Press ENTER to deploy.", 150, 160, 10, Color.White);
            }

            Raylib.DrawText("Controls:", 100, 200, 10, new Color(100, 100, 120, 255));
            Raylib.DrawText("A/D = Move   W/SPACE = Jump+Attack", 100, 214, 10, new Color(100, 100, 120, 255));
            Raylib.DrawText("Q = Dash     ESC = Pause", 100, 228, 10, new Color(100, 100, 120, 255));

            PixelRenderer.EndFrame();
        }

        _config.PlayerName = _playerName;
        _config.Save();
    }

    private void DrawOutro()
    {
        var stats = _scoreService.TryLoadStats();
        var top = _scoreService.LoadTop();

        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
                break;

            PixelRenderer.BeginFrame();

            Raylib.DrawText("=== MISSION REPORT ===", 130, 20, 20, new Color(0, 255, 255, 255));
            Raylib.DrawText($"Pilot: {_playerName}", 60, 60, 10, new Color(0, 255, 136, 255));
            Raylib.DrawText($"Score: {_score}", 60, 76, 10, new Color(255, 255, 0, 255));
            Raylib.DrawText($"Levels Cleared: {_levelsCompleted} / {_levels.Count}", 60, 92, 10, Color.White);
            Raylib.DrawText($"Time: {_totalElapsedMs / 1000.0:F1}s", 60, 108, 10, Color.White);

            if (stats is not null)
            {
                Raylib.DrawText($"Career: Runs {stats.TotalRuns} | Best {stats.BestScore} | Avg {stats.AverageScore}",
                    60, 128, 10, new Color(0, 160, 200, 255));
            }

            Raylib.DrawText("Top Scores:", 60, 152, 10, new Color(255, 0, 255, 255));
            if (top.Count == 0)
            {
                Raylib.DrawText("  (no scores yet)", 60, 168, 10, new Color(100, 100, 120, 255));
            }
            else
            {
                for (var i = 0; i < top.Count; i++)
                {
                    var s = top[i];
                    Raylib.DrawText($"  {i + 1}. {s.Name,-10} {s.Score,6} pts  L{s.LevelReached}  {s.TimeMs / 1000.0:F1}s",
                        60, 168 + i * 14, 10, new Color(200, 200, 220, 255));
                }
            }

            Raylib.DrawText("Press ENTER to exit.", 160, 250, 10, Color.White);

            PixelRenderer.EndFrame();
        }
    }

    private static void ShowPauseScreen()
    {
        while (!Raylib.WindowShouldClose())
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                break;

            PixelRenderer.BeginFrame();
            Raylib.DrawText("=== PAUSED ===", 170, 110, 20, new Color(255, 200, 0, 255));
            Raylib.DrawText("Press ESC to continue", 155, 150, 10, Color.White);
            PixelRenderer.EndFrame();
        }
    }

    private readonly record struct LevelResult(bool GameOver, int ElapsedMs);
}
