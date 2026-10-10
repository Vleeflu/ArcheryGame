using Archery.Assets;
using Archery.Scenes;
using Raylib_cs;

namespace Archery.Core;

/// <summary>Owns the window, assets, and main loop.</summary>
public sealed class Game
{
    private readonly GameConfig _config;
    private readonly SceneManager _scenes = new();

    public Game(GameConfig config)
    {
        _config = config;
    }

    public void Run()
    {
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.Msaa4xHint);
        Raylib.InitWindow(_config.ScreenWidth, _config.ScreenHeight, _config.Title);
        Raylib.SetTargetFPS(_config.TargetFps);

        // Textures need the GL context, so they're loaded after InitWindow and unloaded before CloseWindow.
        var assets = new AssetStore(_config.AssetRoot);
        try
        {
            assets.LoadAll();
            _scenes.Switch(new RangeScene(assets));

            while (!Raylib.WindowShouldClose())
            {
                _scenes.Update(Raylib.GetFrameTime());

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                _scenes.Draw();
                if (_config.ShowFps)
                {
                    Raylib.DrawFPS(10, 10);
                }
                Raylib.EndDrawing();
            }
        }
        finally
        {
            _scenes.Shutdown();
            assets.Dispose();
            Raylib.CloseWindow();
        }
    }
}
