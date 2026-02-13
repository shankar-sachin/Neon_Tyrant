namespace NeonTyrant;

public static class SafeConsole
{
    public static bool TrySetCursorVisible(bool visible)
    {
        try
        {
            Console.CursorVisible = visible;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool TrySetCursorPosition(int left, int top)
    {
        try
        {
            Console.SetCursorPosition(left, top);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    public static bool TryClear()
    {
        try
        {
            Console.Clear();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool TryWrite(string text)
    {
        try
        {
            Console.Write(text);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static int GetWindowWidthOrDefault(int fallback = 100)
    {
        try
        {
            return Math.Max(40, Console.WindowWidth);
        }
        catch (IOException)
        {
            return fallback;
        }
        catch (InvalidOperationException)
        {
            return fallback;
        }
    }

    public static bool TryKeyAvailable(out bool available)
    {
        try
        {
            available = Console.KeyAvailable;
            return true;
        }
        catch (IOException)
        {
            available = false;
            return false;
        }
        catch (InvalidOperationException)
        {
            available = false;
            return false;
        }
    }

    public static bool TryReadKey(bool intercept, out ConsoleKeyInfo keyInfo)
    {
        try
        {
            keyInfo = Console.ReadKey(intercept);
            return true;
        }
        catch (IOException)
        {
            keyInfo = default;
            return false;
        }
        catch (InvalidOperationException)
        {
            keyInfo = default;
            return false;
        }
    }
}
