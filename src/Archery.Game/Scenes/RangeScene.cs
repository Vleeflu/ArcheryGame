using System.Numerics;
using Archery.Assets;
using Archery.Core;
using Raylib_cs;

namespace Archery.Scenes;

/// <summary>
/// First-person view of the shooting range: floating bow with a nocked arrow aimed at the target.
/// Left click releases the arrow; it travels along its line until the tip reaches the crosshair and sticks there,
/// then a fresh arrow is nocked. Drawn in native <see cref="PixelScreen"/> pixels.
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

    /// <summary>Seconds from release until the arrow sticks.</summary>
    private const float FlightSeconds = 0.18f;

    private static readonly Vector2 ArrowRest = Target - SpriteAnchors.Arrow.Direction * RestDistance;
    private static readonly Vector2 BowOrigin = ArrowRest - SpriteAnchors.Bow.Shelf;
    private static readonly Vector2 NockedNock = BowOrigin + SpriteAnchors.Bow.Nock;

    /// <summary>Arrow steps the nock travels until the arrow's tip is on the crosshair.</summary>
    private static readonly int FlightSteps = StepsAlongArrow(Target - NockedNock) - StepsAlongArrow(SpriteAnchors.Arrow.Tip - SpriteAnchors.Arrow.Nock);

    private static readonly Color StringColor = new(234, 212, 170, 255);
    private static readonly Color ServingColor = new(24, 20, 37, 255);

    private readonly AssetStore _assets;

    /// <summary>Nock positions of arrows that have landed in the target, oldest first.</summary>
    private readonly List<Vector2> _stuckArrows = new();

    /// <summary>Seconds since release while an arrow is in the air; null when an arrow is nocked.</summary>
    private float? _flightTime;

    public RangeScene(AssetStore assets)
    {
        _assets = assets;
    }

    public void Enter() { }

    public void Update(float dt)
    {
        if (_flightTime is null)
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _flightTime = 0f;
            }
            return;
        }

        _flightTime += dt;
        if (_flightTime >= FlightSeconds)
        {
            _stuckArrows.Add(FlyingNock(FlightSeconds));
            _flightTime = null; // next arrow is nocked
        }
    }

    public void Draw()
    {
        var hills = _assets.Texture(AssetIds.Hills);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Sky), 0, 0, Color.White);
        Raylib.DrawTexture(hills, 0, Horizon + 2 - hills.Height, Color.White);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Ground), 0, Horizon, Color.White);

        DrawAt(AssetIds.TargetStand, SpriteAnchors.TargetStand.FaceCenter, Target);
        DrawAt(AssetIds.TargetFace, SpriteAnchors.TargetFace.Center, Target);
        foreach (var nock in _stuckArrows)
        {
            DrawAt(AssetIds.Arrow, SpriteAnchors.Arrow.Nock, nock);
        }

        DrawAt(AssetIds.Bow, SpriteAnchors.Bow.Shelf, ArrowRest);
        if (_flightTime is { } t)
        {
            DrawAt(AssetIds.Arrow, SpriteAnchors.Arrow.Nock, FlyingNock(t));
        }
        else
        {
            DrawAt(AssetIds.Arrow, SpriteAnchors.Arrow.Nock, NockedNock);
        }
        DrawString(BowOrigin, NockedNock);

        var crosshair = _assets.Texture(AssetIds.Crosshair);
        DrawAt(AssetIds.Crosshair, new Vector2(crosshair.Width / 2, crosshair.Height / 2), Target);
    }

    public void Exit() { }

    /// <summary>Nock position of the flying arrow, snapped to whole arrow steps so it moves pixel-exactly.</summary>
    private static Vector2 FlyingNock(float time)
    {
        int steps = (int)(FlightSteps * Math.Clamp(time / FlightSeconds, 0f, 1f));
        return NockedNock + SpriteAnchors.Arrow.Direction * steps;
    }

    /// <summary>How many arrow steps <paramref name="offset"/> spans (it must lie on the arrow's line).</summary>
    private static int StepsAlongArrow(Vector2 offset) => (int)(offset.X / SpriteAnchors.Arrow.Direction.X);

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
