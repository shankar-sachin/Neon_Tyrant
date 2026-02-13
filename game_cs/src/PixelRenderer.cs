using System.Numerics;
using Raylib_cs;

namespace NeonTyrant;

public static class PixelRenderer
{
    public const int InternalWidth = 480;
    public const int InternalHeight = 272;
    public const int TileSize = 8;
    public const int Scale = 3;
    public const int WindowWidth = InternalWidth * Scale;
    public const int WindowHeight = InternalHeight * Scale;
    public const int LevelOffsetY = 40;

    private static RenderTexture2D _target;
    private static readonly Dictionary<char, Texture2D> _tileTextures = new();
    private static Texture2D _heartTexture;

    public static void Init()
    {
        _target = Raylib.LoadRenderTexture(InternalWidth, InternalHeight);
        Raylib.SetTextureFilter(_target.Texture, TextureFilter.Point);

        char[] tileChars = ['#', '=', '^', '*', 'C', 'E', '@', 'M', 'B', ' '];
        foreach (var ch in tileChars)
        {
            var colors = PixelChars.GetTile(ch);
            if (colors == null) continue;
            _tileTextures[ch] = BuildTexture(colors);
        }

        _heartTexture = BuildTexture(PixelChars.Heart);
    }

    private static Texture2D BuildTexture(Color[,] colors)
    {
        var img = Raylib.GenImageColor(TileSize, TileSize, new Color(0, 0, 0, 0));
        for (var y = 0; y < TileSize; y++)
            for (var x = 0; x < TileSize; x++)
                Raylib.ImageDrawPixel(ref img, x, y, colors[y, x]);
        var tex = Raylib.LoadTextureFromImage(img);
        Raylib.SetTextureFilter(tex, TextureFilter.Point);
        Raylib.UnloadImage(img);
        return tex;
    }

    public static void BeginFrame()
    {
        Raylib.BeginTextureMode(_target);
        Raylib.ClearBackground(PixelChars.BgColor);
    }

    public static void EndFrame()
    {
        Raylib.EndTextureMode();
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.DrawTexturePro(
            _target.Texture,
            new Rectangle(0, 0, InternalWidth, -InternalHeight),
            new Rectangle(0, 0, WindowWidth, WindowHeight),
            Vector2.Zero,
            0f,
            Color.White);
        Raylib.EndDrawing();
    }

    public static void DrawTile(int tileX, int tileY, char tile)
    {
        if (_tileTextures.TryGetValue(tile, out var tex))
        {
            Raylib.DrawTexture(tex, tileX * TileSize, tileY * TileSize + LevelOffsetY, Color.White);
        }
    }

    public static void DrawHeart(int pixelX, int pixelY)
    {
        Raylib.DrawTexture(_heartTexture, pixelX, pixelY, Color.White);
    }

    public static void Cleanup()
    {
        foreach (var tex in _tileTextures.Values)
            Raylib.UnloadTexture(tex);
        _tileTextures.Clear();
        Raylib.UnloadTexture(_heartTexture);
        Raylib.UnloadRenderTexture(_target);
    }
}
