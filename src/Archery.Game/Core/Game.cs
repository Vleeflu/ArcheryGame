using System.Numerics;
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
        Raylib.SetConfigFlags(ConfigFlags.VSyncHint);
        Raylib.InitWindow(PixelScreen.Width * _config.WindowScale, PixelScreen.Height * _config.WindowScale, _config.Title);
        Raylib.SetTargetFPS(_config.TargetFps);

        // GPU resources need the GL context, so they're created after InitWindow and released before CloseWindow.
        var frame = Raylib.LoadRenderTexture(PixelScreen.Width, PixelScreen.Height);
        Raylib.SetTextureFilter(frame.Texture, TextureFilter.Point);
        var assets = new AssetStore(_config.AssetRoot);
        try
        {
            assets.LoadAll();
            _scenes.Switch(new RangeScene(assets));

            while (!Raylib.WindowShouldClose())
            {
                _scenes.Update(Raylib.GetFrameTime());

                Raylib.BeginTextureMode(frame);
                Raylib.ClearBackground(Color.Black);
                _scenes.Draw();
                Raylib.EndTextureMode();

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                PresentFrame(frame.Texture);
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
            Raylib.UnloadRenderTexture(frame);
            Raylib.CloseWindow();
        }
    }

    /// <summary>Scales the native frame by the largest whole factor that fits, centred (letterboxed).</summary>
    private static void PresentFrame(Texture2D frame)
    {
        int scale = Math.Max(1, Math.Min(Raylib.GetScreenWidth() / PixelScreen.Width, Raylib.GetScreenHeight() / PixelScreen.Height));
        int w = PixelScreen.Width * scale, h = PixelScreen.Height * scale;
        var dest = new Rectangle((Raylib.GetScreenWidth() - w) / 2, (Raylib.GetScreenHeight() - h) / 2, w, h);
        // Render textures are stored bottom-up; a negative source height flips them upright.
        var source = new Rectangle(0, 0, PixelScreen.Width, -PixelScreen.Height);
        Raylib.DrawTexturePro(frame, source, dest, Vector2.Zero, 0f, Color.White);
    }
}
