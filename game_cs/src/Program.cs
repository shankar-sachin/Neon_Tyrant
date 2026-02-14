using NeonTyrant;
using Raylib_cs;

Raylib.InitWindow(PixelRenderer.WindowWidth, PixelRenderer.WindowHeight, "NEON TYRANT");
Raylib.SetTargetFPS(60);

var iconImage = IconBuilder.Build32x32();
Raylib.SetWindowIcon(iconImage);
Raylib.UnloadImage(iconImage);

PixelRenderer.Init();

try
{
    var config = GameConfig.TryLoad();
    if (config is null || !config.SetupCompleted)
    {
        var wizard = new SetupWizard();
        config = wizard.Run();
        if (config is null)
            return;
        config.Save();
    }

    var game = new Game(config);
    game.Run();
}
finally
{
    PixelRenderer.Cleanup();
    Raylib.CloseWindow();
}
