using NeonTyrant;
using Raylib_cs;

Raylib.InitWindow(PixelRenderer.WindowWidth, PixelRenderer.WindowHeight, "NEON TYRANT");
Raylib.SetTargetFPS(60);
PixelRenderer.Init();

try
{
    var game = new Game();
    game.Run();
}
finally
{
    PixelRenderer.Cleanup();
    Raylib.CloseWindow();
}
