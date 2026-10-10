using System.Numerics;
using Archery.Assets;
using Archery.Core;
using Raylib_cs;

namespace Archery.Scenes;

/// <summary>
/// First-person view of the shooting range. Static layout only: no aiming, drawing, or shooting yet.
/// </summary>
public sealed class RangeScene : IScene
{
    private const float HorizonRatio = 0.55f;
    private const float TargetScreenSize = 160f;

    private readonly AssetStore _assets;

    public RangeScene(AssetStore assets)
    {
        _assets = assets;
    }

    public void Enter() { }

    public void Update(float dt) { }

    public void Draw()
    {
        float w = Raylib.GetScreenWidth();
        float h = Raylib.GetScreenHeight();
        float horizon = h * HorizonRatio;

        DrawStretched(_assets.Texture(AssetIds.Sky), new Rectangle(0, 0, w, horizon));
        DrawStretched(_assets.Texture(AssetIds.Ground), new Rectangle(0, horizon, w, h - horizon));

        // Target stands on the horizon in the middle of the view.
        DrawCentered(_assets.Texture(AssetIds.Target), new Vector2(w / 2f, horizon - TargetScreenSize / 2f), TargetScreenSize);

        // Bow held in front of the viewer, arrow nocked along the center line.
        var bow = _assets.Texture(AssetIds.Bow);
        float bowHeight = h * 0.95f;
        DrawCentered(bow, new Vector2(w / 2f, h * 0.72f), bowHeight);

        var arrow = _assets.Texture(AssetIds.Arrow);
        DrawCentered(arrow, new Vector2(w / 2f, h * 0.85f), h * 0.6f);

        var crosshair = _assets.Texture(AssetIds.Crosshair);
        DrawCentered(crosshair, new Vector2(w / 2f, h / 2f), crosshair.Height);
    }

    public void Exit() { }

    private static void DrawStretched(Texture2D texture, Rectangle dest)
    {
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        Raylib.DrawTexturePro(texture, source, dest, Vector2.Zero, 0f, Color.White);
    }

    /// <summary>Draws the texture centered on <paramref name="center"/>, scaled to <paramref name="height"/> (aspect kept).</summary>
    private static void DrawCentered(Texture2D texture, Vector2 center, float height)
    {
        float scale = height / texture.Height;
        float width = texture.Width * scale;
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(center.X - width / 2f, center.Y - height / 2f, width, height);
        Raylib.DrawTexturePro(texture, source, dest, Vector2.Zero, 0f, Color.White);
    }
}
