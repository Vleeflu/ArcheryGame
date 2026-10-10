using System.Numerics;
using Archery.Assets;
using Archery.Core;
using Raylib_cs;

namespace Archery.Scenes;

/// <summary>
/// First-person view of the shooting range at rest: floating bow, arrow nocked and pointing at the target.
/// Drawn in native <see cref="PixelScreen"/> pixels. Layout only; no aiming, drawing, or shooting yet.
/// </summary>
public sealed class RangeScene : IScene
{
    private const int Horizon = 81;
    private static readonly Vector2 Target = new(132, 84);

    /// <summary>
    /// The arrow is drawn on a fixed slope, so the arrow rest sits on that line through the target centre, this many
    /// arrow steps back from it.
    /// </summary>
    private const int RestDistance = 40;
    private static readonly Vector2 ArrowRest = Target - SpriteAnchors.Arrow.Direction * RestDistance;

    private static readonly Color StringColor = new(234, 212, 170, 255);
    private static readonly Color ServingColor = new(24, 20, 37, 255);

    private readonly AssetStore _assets;

    public RangeScene(AssetStore assets)
    {
        _assets = assets;
    }

    public void Enter() { }

    public void Update(float dt) { }

    public void Draw()
    {
        var hills = _assets.Texture(AssetIds.Hills);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Sky), 0, 0, Color.White);
        Raylib.DrawTexture(hills, 0, Horizon + 2 - hills.Height, Color.White);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Ground), 0, Horizon, Color.White);

        DrawAt(AssetIds.TargetStand, SpriteAnchors.TargetStand.FaceCenter, Target);
        DrawAt(AssetIds.TargetFace, SpriteAnchors.TargetFace.Center, Target);

        // Bow rig: bow placed by its arrow rest; arrow nock sits on the string, shaft over the rest.
        var bowOrigin = ArrowRest - SpriteAnchors.Bow.Shelf;
        var nock = bowOrigin + SpriteAnchors.Bow.Nock;

        DrawAt(AssetIds.Bow, SpriteAnchors.Bow.Shelf, ArrowRest);
        DrawAt(AssetIds.Arrow, SpriteAnchors.Arrow.Nock, nock);
        DrawString(bowOrigin, nock);

        var crosshair = _assets.Texture(AssetIds.Crosshair);
        DrawAt(AssetIds.Crosshair, new Vector2(crosshair.Width / 2, crosshair.Height / 2), Target);
    }

    public void Exit() { }

    /// <summary>Braced string: tips wrap the recurves, straight run between the contact points, serving at the nock.</summary>
    private static void DrawString(Vector2 bowOrigin, Vector2 nock)
    {
        var topTip = bowOrigin + SpriteAnchors.Bow.TopTip;
        var topContact = bowOrigin + SpriteAnchors.Bow.TopContact;
        var bottomContact = bowOrigin + SpriteAnchors.Bow.BottomContact;
        var bottomTip = bowOrigin + SpriteAnchors.Bow.BottomTip;

        PixelLine(topTip, topContact, StringColor);
        PixelLine(topContact, bottomContact, StringColor);
        PixelLine(bottomContact, bottomTip, StringColor);
        var along = Vector2.Normalize(topContact - bottomContact) * 4.5f;
        PixelLine(nock - along, nock + along, ServingColor);
    }

    /// <summary>Bresenham line, one pixel wide, no anti-aliasing.</summary>
    private static void PixelLine(Vector2 from, Vector2 to, Color color)
    {
        int x0 = (int)from.X, y0 = (int)from.Y, x1 = (int)to.X, y1 = (int)to.Y;
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            Raylib.DrawPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1) return;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>Draws a sprite so that its pixel <paramref name="anchor"/> lands on <paramref name="screen"/>.</summary>
    private void DrawAt(string id, Vector2 anchor, Vector2 screen)
    {
        var pos = screen - anchor;
        Raylib.DrawTexture(_assets.Texture(id), (int)pos.X, (int)pos.Y, Color.White);
    }
}
