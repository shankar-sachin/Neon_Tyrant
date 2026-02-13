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
        var left = false;
        var right = false;
        var jump = false;
        var action = false;
        var dash = false;
        var escape = false;

        while (SafeConsole.TryKeyAvailable(out var available) && available)
        {
            if (!SafeConsole.TryReadKey(intercept: true, out var keyInfo))
            {
                break;
            }

            var key = keyInfo.Key;
            switch (key)
            {
                case ConsoleKey.A:
                    left = true;
                    break;
                case ConsoleKey.D:
                    right = true;
                    break;
                case ConsoleKey.W:
                case ConsoleKey.Spacebar:
                    jump = true;
                    action = true;
                    break;
                case ConsoleKey.Q:
                    dash = true;
                    break;
                case ConsoleKey.Escape:
                    escape = true;
                    break;
            }
        }

        return new FrameInput(left, right, jump, action, dash, escape);
    }
}
