using System.Numerics;
using Archery.Assets;
using Archery.Core;
using Archery.Gameplay;
using Raylib_cs;

namespace Archery.Scenes;

/// <summary>
/// First-person view of the shooting range: floating bow with a nocked arrow aimed at a target ~30 m away.
/// Holding the left mouse button draws the bow (the arrow slides back, the string bends into a V); releasing shoots.
/// The arrow flies as a 3D ballistic object (projected through <see cref="Camera"/>), lands on the crosshair and
/// stays in the target, then a fresh arrow is nocked. Drawn in native <see cref="PixelScreen"/> pixels.
/// </summary>
public sealed class RangeScene : IScene
{
    private const int Horizon = 81;
    private static readonly Vector2 Target = new(132, 86);

    /// <summary>
    /// The nocked arrow sprite is drawn on a fixed slope, so the arrow rest sits on that line through the target
    /// centre, this many arrow steps back from it.
    /// </summary>
    private const int RestDistance = 40;

    private const float TargetDistance = 30f;       // metres to the target face
    private const float TargetFaceRadius = 0.61f;   // metres (122 cm face)
    private const float NockDepth = 0.75f;          // metres from the eye to the nocked arrow's nock
    private const float FlightSeconds = 0.6f;       // ~50 m/s: a touch slower than a real recurve so the arc reads on screen
    private const float ShaftThickness = 0.006f;    // metres, slightly exaggerated so near arrows read clearly
    private const float DrawSeconds = 0.7f;         // time to reach full draw while holding the button

    private static readonly Vector2 PixelCentre = new(0.5f, 0.5f);

    private static readonly Vector2 ArrowRest = Target - SpriteAnchors.Arrow.Direction * RestDistance;
    private static readonly Vector2 BowOrigin = ArrowRest - SpriteAnchors.Bow.Shelf;
    private static readonly Vector2 NockedNock = BowOrigin + SpriteAnchors.Bow.Nock;

    /// <summary>Focal length chosen so the face sprite is the right size for a 122 cm face at <see cref="TargetDistance"/>.</summary>
    private static readonly PixelCamera Camera = new(
        new Vector2(PixelScreen.Width / 2f, Horizon),
        SpriteAnchors.TargetFace.Radius * TargetDistance / TargetFaceRadius);

    private static readonly Vector3 NockWorld = Camera.Unproject(NockedNock + PixelCentre, NockDepth);
    private static readonly Vector3 TargetWorld = Camera.Unproject(Target + PixelCentre, TargetDistance);
    private static readonly float ArrowLength = Vector3.Distance(NockWorld, AimLinePoint(SpriteAnchors.Arrow.Tip - SpriteAnchors.Arrow.Nock));

    /// <summary>
    /// Arrow steps the nock can be pulled back: at full draw the point sits one step in front of the arrow rest,
    /// as on a real bow.
    /// </summary>
    private static readonly int MaxDrawSteps =
        StepsAlongArrow(SpriteAnchors.Arrow.Tip - SpriteAnchors.Arrow.Nock) - StepsAlongArrow(ArrowRest - NockedNock) - 1;

    private static readonly Color StringColor = new(234, 212, 170, 255);
    private static readonly Color ServingColor = new(24, 20, 37, 255);
    private static readonly Color ShaftColor = new(228, 166, 114, 255);
    private static readonly Color FletchColor = new(228, 59, 68, 255);
    private static readonly Color NockColor = new(99, 199, 77, 255);
    private static readonly Color PointColor = new(192, 203, 220, 255);
    private static readonly Color StuckShaftColor = new(62, 39, 49, 255);

    private readonly AssetStore _assets;

    /// <summary>Arrows that have landed: tip position and pointing direction in world space.</summary>
    private readonly List<(Vector3 Tip, Vector3 Direction)> _stuckArrows = new();

    private ArrowFlight? _flight;
    private float _flightTime;

    /// <summary>Seconds the button has been held while drawing; null when not drawing.</summary>
    private float? _drawTime;

    public RangeScene(AssetStore assets)
    {
        _assets = assets;
    }

    public void Enter() { }

    public void Update(float dt)
    {
        if (_flight is not null)
        {
            _flightTime += dt;
            if (_flightTime >= _flight.Duration)
            {
                _stuckArrows.Add(_flight.At(_flight.Duration));
                _flight = null; // next arrow is nocked
            }
            return;
        }

        if (_drawTime is null)
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                _drawTime = 0f;
            }
            return;
        }

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
        {
            _drawTime += dt;
            return;
        }

        // Released. The sprite only slides in 2D, so a drawn-back nock has no matching 3D pose; the flight starts
        // from the braced position, which reads as the string snapping the arrow forward.
        _flight = new ArrowFlight(NockWorld, TargetWorld, ArrowLength, FlightSeconds);
        _flightTime = 0f;
        _drawTime = null;
    }

    public void Draw()
    {
        var hills = _assets.Texture(AssetIds.Hills);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Sky), 0, 0, Color.White);
        Raylib.DrawTexture(hills, 0, Horizon + 2 - hills.Height, Color.White);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Ground), 0, Horizon, Color.White);

        DrawAt(AssetIds.TargetStand, SpriteAnchors.TargetStand.FaceCenter, Target);
        DrawAt(AssetIds.TargetFace, SpriteAnchors.TargetFace.Center, Target);
        foreach (var (tip, direction) in _stuckArrows)
        {
            DrawArrow3D(tip, direction);
        }

        DrawAt(AssetIds.Bow, SpriteAnchors.Bow.Shelf, ArrowRest);
        if (_flight is null)
        {
            DrawAt(AssetIds.Arrow, SpriteAnchors.Arrow.Nock, DrawnNock());
            DrawString(BowOrigin, DrawnNock());
        }
        else
        {
            var (tip, direction) = _flight.At(_flightTime);
            DrawArrow3D(tip, direction);
            DrawString(BowOrigin, NockedNock);
        }

        var crosshair = _assets.Texture(AssetIds.Crosshair);
        DrawAt(AssetIds.Crosshair, new Vector2(crosshair.Width / 2, crosshair.Height / 2), Target);
    }

    public void Exit() { }

    /// <summary>Nock position on screen: the rest position, pulled back along the arrow while drawing.</summary>
    private Vector2 DrawnNock()
    {
        if (_drawTime is not { } held)
        {
            return NockedNock;
        }

        // Ease-out: quick at first, slowing toward full draw so it reads as heavy.
        float p = Math.Clamp(held / DrawSeconds, 0f, 1f);
        float eased = 1f - (1f - p) * (1f - p);
        int steps = (int)MathF.Round(MaxDrawSteps * eased);
        return NockedNock - SpriteAnchors.Arrow.Direction * steps;
    }

    /// <summary>
    /// World point on the aim line (rest nock → target centre) that projects to the screen point
    /// <paramref name="offsetFromNock"/> away from the rest nock along the arrow's screen line (negative = drawn back).
    /// With screen fraction s of the nock→target distance, the matching fraction along a 3D segment from depth z0 to
    /// z1 is s·z0 / (z1·(1−s) + s·z0).
    /// </summary>
    private static Vector3 AimLinePoint(Vector2 offsetFromNock)
    {
        float s = StepsAlongArrow(offsetFromNock) / (float)StepsAlongArrow(Target - NockedNock);
        float z0 = NockWorld.Z, z1 = TargetWorld.Z;
        float fraction = s * z0 / (z1 * (1f - s) + s * z0);
        return Vector3.Lerp(NockWorld, TargetWorld, fraction);
    }

    /// <summary>How many arrow steps <paramref name="offset"/> spans (it must lie on the arrow's line).</summary>
    private static int StepsAlongArrow(Vector2 offset) => (int)(offset.X / SpriteAnchors.Arrow.Direction.X);

    /// <summary>
    /// Draws an arrow from its world-space tip and direction. Near arrows are a shaft with red fletching; once the
    /// projection is only a few pixels long (seen nearly end-on, e.g. stuck in the target) it becomes the
    /// fletching seen from behind.
    /// </summary>
    private static void DrawArrow3D(Vector3 tip, Vector3 direction)
    {
        var nock = tip - direction * ArrowLength;
        var tipPx = Snap(Camera.Project(tip));
        var nockPx = Snap(Camera.Project(nock));

        if (Vector2.Distance(tipPx, nockPx) < 3f)
        {
            int x = (int)nockPx.X, y = (int)nockPx.Y;
            Raylib.DrawPixel(x, y, StuckShaftColor);
            Raylib.DrawPixel(x - 1, y, FletchColor);
            Raylib.DrawPixel(x + 1, y, FletchColor);
            Raylib.DrawPixel(x, y - 1, FletchColor);
            return;
        }

        float depth = (tip.Z + nock.Z) / 2f;
        float width = Math.Clamp(MathF.Round(Camera.Focal * ShaftThickness / depth), 1f, 3f);
        var fletchEnd = Snap(Vector2.Lerp(nockPx, tipPx, 0.22f));
        Raylib.DrawLineEx(nockPx, tipPx, width, ShaftColor);
        Raylib.DrawLineEx(nockPx, fletchEnd, width + 1f, FletchColor);
        Raylib.DrawPixel((int)tipPx.X, (int)tipPx.Y, PointColor);
        Raylib.DrawPixel((int)nockPx.X, (int)nockPx.Y, NockColor);
    }

    /// <summary>Centre of the pixel containing <paramref name="p"/>, so lines rasterize on the pixel grid.</summary>
    private static Vector2 Snap(Vector2 p) => new(MathF.Floor(p.X) + 0.5f, MathF.Floor(p.Y) + 0.5f);

    /// <summary>
    /// String: tips wrap the recurves; from the contact points it runs straight to the nock (a straight line at rest,
    /// a V when drawn), with the serving wrapped around the nock on both halves.
    /// </summary>
    private static void DrawString(Vector2 bowOrigin, Vector2 nock)
    {
        var topTip = bowOrigin + SpriteAnchors.Bow.TopTip;
        var topContact = bowOrigin + SpriteAnchors.Bow.TopContact;
        var bottomContact = bowOrigin + SpriteAnchors.Bow.BottomContact;
        var bottomTip = bowOrigin + SpriteAnchors.Bow.BottomTip;

        PixelLine(topTip, topContact, StringColor);
        PixelLine(topContact, nock, StringColor);
        PixelLine(nock, bottomContact, StringColor);
        PixelLine(bottomContact, bottomTip, StringColor);
        PixelLine(nock, nock + Vector2.Normalize(topContact - nock) * 4.5f, ServingColor);
        PixelLine(nock, nock + Vector2.Normalize(bottomContact - nock) * 4.5f, ServingColor);
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
