using Raylib_cs;

namespace NeonTyrant;

public readonly record struct FrameInput(
    bool Left,
    bool Right,
    bool JumpPressed,
    bool ActionPressed,
    bool DashPressed,
    bool EscapePressed);

public static class InputService
{
    public static FrameInput ReadFrame()
    {
        return new FrameInput(
            Left: Raylib.IsKeyDown(KeyboardKey.A) || Raylib.IsKeyDown(KeyboardKey.Left),
            Right: Raylib.IsKeyDown(KeyboardKey.D) || Raylib.IsKeyDown(KeyboardKey.Right),
            JumpPressed: Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Space)
                || Raylib.IsKeyPressed(KeyboardKey.Up),
            ActionPressed: Raylib.IsKeyPressed(KeyboardKey.W) || Raylib.IsKeyPressed(KeyboardKey.Space)
                || Raylib.IsKeyPressed(KeyboardKey.Up),
            DashPressed: Raylib.IsKeyPressed(KeyboardKey.Q),
            EscapePressed: Raylib.IsKeyPressed(KeyboardKey.Escape));
    }
}
