using System.Numerics;
using Archery.Assets;
using Archery.Scenes;
using Raylib_cs;

namespace Archery.Core;

/// <summary>Owns the window, assets, and main loop.</summary>
public sealed class Game
{
    // OpenGL blend factors/equation (raylib exposes the call, not the constants).
    private const int GlOne = 1, GlSrcAlpha = 0x0302, GlOneMinusSrcAlpha = 0x0303, GlFuncAdd = 0x8006;

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
        var fineFrame = Raylib.LoadRenderTexture(PixelScreen.Width * PixelScreen.FineScale, PixelScreen.Height * PixelScreen.FineScale);
        var frame = Raylib.LoadRenderTexture(PixelScreen.Width, PixelScreen.Height);
        Raylib.SetTextureFilter(fineFrame.Texture, TextureFilter.Point);
        Raylib.SetTextureFilter(frame.Texture, TextureFilter.Point);
        var assets = new AssetStore(_config.AssetRoot);
        try
        {
            assets.LoadAll();
            _scenes.Switch(new RangeScene(assets));

            while (!Raylib.WindowShouldClose())
            {
                _scenes.Update(Raylib.GetFrameTime());

                // Back layer at FineScale× resolution, in native coordinates (the 2D camera scales them up).
                Raylib.BeginTextureMode(fineFrame);
                Raylib.ClearBackground(Color.Black);
                Raylib.BeginMode2D(new Camera2D { Offset = Vector2.Zero, Target = Vector2.Zero, Rotation = 0f, Zoom = PixelScreen.FineScale });
                _scenes.DrawFinePixels();
                Raylib.EndMode2D();
                Raylib.EndTextureMode();

                // Front pixel-art layer at native resolution, starting transparent so the back layer shows through.
                // Alpha is accumulated properly (the frame holds premultiplied colour) so semi-transparent pixels keep
                // their strength when composited.
                Raylib.BeginTextureMode(frame);
                Raylib.ClearBackground(Color.Blank);
                Rlgl.SetBlendFactorsSeparate(GlSrcAlpha, GlOneMinusSrcAlpha, GlOne, GlOneMinusSrcAlpha, GlFuncAdd, GlFuncAdd);
                Raylib.BeginBlendMode(BlendMode.CustomSeparate);
                _scenes.DrawPixelArt();
                Raylib.EndBlendMode();
                Raylib.EndTextureMode();

                var view = FrameRect();
                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);
                // Render textures are stored bottom-up; a negative source height flips them upright.
                Raylib.DrawTexturePro(fineFrame.Texture, new Rectangle(0, 0, fineFrame.Texture.Width, -fineFrame.Texture.Height), view, Vector2.Zero, 0f, Color.White);
                Raylib.BeginBlendMode(BlendMode.AlphaPremultiply);
                Raylib.DrawTexturePro(frame.Texture, new Rectangle(0, 0, PixelScreen.Width, -PixelScreen.Height), view, Vector2.Zero, 0f, Color.White);
                Raylib.EndBlendMode();

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
            Raylib.UnloadRenderTexture(fineFrame);
            Raylib.CloseWindow();
        }
    }

    /// <summary>Where the native frame goes in the window: scaled by the largest whole factor that fits, centred (letterboxed).</summary>
    private static Rectangle FrameRect()
    {
        int scale = Math.Max(1, Math.Min(Raylib.GetScreenWidth() / PixelScreen.Width, Raylib.GetScreenHeight() / PixelScreen.Height));
        int w = PixelScreen.Width * scale, h = PixelScreen.Height * scale;
        return new Rectangle((Raylib.GetScreenWidth() - w) / 2, (Raylib.GetScreenHeight() - h) / 2, w, h);
    }
}
