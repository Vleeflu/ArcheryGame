using System.Numerics;
using Archery.Assets;
using Archery.Core;
using Archery.Gameplay;
using Raylib_cs;

namespace Archery.Scenes;

/// <summary>
/// First-person view of the shooting range with a target ~30 m away.
/// <list type="bullet">
/// <item>Ready: bow held low at the side, arrow nocked and pointing at the target.</item>
/// <item>Drawing (hold left mouse): the lowered bow drops away and the bow is raised upright in front of your face;
/// you look down the arrow while it is drawn back toward you, its point settling on the rest inside the crosshair.</item>
/// <item>Flying (release): the camera chases the arrow downrange in slow motion and stops short of the target.</item>
/// <item>Impact: the view holds on the target, then cuts back to the archer with a fresh arrow.</item>
/// </list>
/// The world is drawn through a <see cref="PixelCamera"/> (the eye, or the arrow cam). The bows are 2D sprites in
/// front of it; the arrow in the aim and in flight is a 3D object, so release continues seamlessly into the flight.
/// Drawn in native <see cref="PixelScreen"/> pixels.
/// </summary>
public sealed class RangeScene : IScene
{
    private enum Phase { Ready, Drawing, Flying, Impact }

    private const int Horizon = 81;

    /// <summary>Target face centre on screen; the crosshair sits on it.</summary>
    private static readonly Vector2 Target = new(160, 86);

    /// <summary>Where the arrow's point sits at full draw (on the raised bow's arrow rest), just inside the crosshair.</summary>
    private static readonly Vector2 AimPoint = new(160, 89);

    /// <summary>Lowered pose: the nocked arrow sprite lies on its slope through the target, this many steps back.</summary>
    private const int RestDistance = 40;

    private const float TargetDistance = 30f;       // metres to the target face
    private const float TargetFaceRadius = 0.61f;   // metres (122 cm face)
    private const float NockDepth = 0.75f;          // metres from the eye to the nock in the lowered pose
    private const float AimTipDepth = 0.78f;        // metres from the eye to the point at full draw
    private const float DrawLength = 0.45f;         // metres the nock travels from brace height to full draw
    private const float FlightSeconds = 0.6f;       // ~50 m/s
    private const float ShaftThickness = 0.008f;    // metres (8 mm shaft); drawn at true perspective size...
    private const float MaxShaftPixels = 8f;        // ...but capped so the end right at the eye stays an arrow, not a plank
    private const float FletchSpan = 0.025f;        // metres, size of the fletching seen end-on
    private const float StuckSagDegrees = 8f;       // a landed arrow's protruding shaft droops under its own weight

    private const float LowerSeconds = 0.12f;       // lowered bow leaves the view
    private const float RaiseDelay = 0.06f;         // raised bow starts coming up after this
    private const float RaiseSeconds = 0.22f;       // raised bow travels into place
    private const float DrawSeconds = 0.7f;         // then the arrow is drawn back to full draw
    private const float ArrowCamTimeScale = 0.6f;   // slow motion while following the arrow
    private const float ArrowCamStopShort = 5f;     // the chase camera stops this many metres before the target
    private const float ImpactHoldSeconds = 0.9f;   // how long to look at the hit before cutting back

    /// <summary>How far below its place the raised bow starts, and where it drops to after release.</summary>
    private static readonly Vector2 AimBowOffscreen = new(0, 200);

    /// <summary>Where the lowered bow drops to when the bow is raised.</summary>
    private static readonly Vector2 LoweredBowOffscreen = new(40, 150);

    private static readonly Vector2 PixelCentre = new(0.5f, 0.5f);

    private static readonly Vector2 ArrowRest = Target - SpriteAnchors.Arrow.Direction * RestDistance;
    private static readonly Vector2 BowOrigin = ArrowRest - SpriteAnchors.Bow.Shelf;
    private static readonly Vector2 NockedNock = BowOrigin + SpriteAnchors.Bow.Nock;
    private static readonly Vector2 AimBowOrigin = AimPoint - SpriteAnchors.BowAim.Rest;

    /// <summary>The archer's eye. Focal length makes the face sprite the right size for a 122 cm face at 30 m.</summary>
    private static readonly PixelCamera Eye = new(
        new Vector2(PixelScreen.Width / 2f, Horizon),
        SpriteAnchors.TargetFace.Radius * TargetDistance / TargetFaceRadius,
        Vector3.Zero);

    private static readonly Vector3 TargetWorld = Eye.Unproject(Target + PixelCentre, TargetDistance);
    private static readonly float ArrowLength = ArrowLengthMatchingLoweredSprite();

    // Raised aim: the arrow points at the target; at full draw its point projects onto AimPoint.
    private static readonly Vector3 AimTip = Eye.Unproject(AimPoint + PixelCentre, AimTipDepth);
    private static readonly Vector3 AimDirection = Vector3.Normalize(TargetWorld - AimTip);
    private static readonly Vector3 NockAtFullDraw = AimTip - AimDirection * ArrowLength;
    private static readonly Vector3 NockAtBrace = NockAtFullDraw + AimDirection * DrawLength;

    private static readonly Color StringColor = new(234, 212, 170, 255);
    private static readonly Color NearStringColor = new(234, 212, 170, 80); // right in front of the eye: faint, out of focus
    private static readonly Color ServingColor = new(24, 20, 37, 255);
    private static readonly Color OutlineColor = new(24, 20, 37, 255);
    private static readonly Color ShaftColor = new(184, 111, 80, 255);
    private static readonly Color ShaftHighlightColor = new(234, 212, 170, 255);
    private static readonly Color FletchColor = new(228, 59, 68, 255);
    private static readonly Color FletchHighlightColor = new(246, 117, 122, 255);
    private static readonly Color NockColor = new(99, 199, 77, 255);
    private static readonly Color PointColor = new(192, 203, 220, 255);

    /// <summary>Screen direction the light comes from (upper left), for which side of the shaft gets the highlight.</summary>
    private static readonly Vector2 LightFrom = Vector2.Normalize(new Vector2(-1f, -1f));

    private readonly AssetStore _assets;

    /// <summary>Arrows that have landed: tip position and (sagging) shaft direction in world space.</summary>
    private readonly List<(Vector3 Tip, Vector3 Direction)> _stuckArrows = new();

    private Phase _phase = Phase.Ready;

    /// <summary>Seconds in the current phase (flight seconds while flying, real seconds otherwise).</summary>
    private float _phaseTime;

    private ArrowFlight? _flight;

    public RangeScene(AssetStore assets)
    {
        _assets = assets;
    }

    public void Enter() { }

    public void Update(float dt)
    {
        switch (_phase)
        {
            case Phase.Ready:
                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    SetPhase(Phase.Drawing);
                }
                break;

            case Phase.Drawing:
                if (Raylib.IsMouseButtonDown(MouseButton.Left))
                {
                    _phaseTime += dt;
                }
                else
                {
                    // The arrow leaves from exactly where it is drawn in the aim.
                    _flight = new ArrowFlight(AimNock(DrawProgress()), TargetWorld, ArrowLength, FlightSeconds);
                    SetPhase(Phase.Flying);
                }
                break;

            case Phase.Flying:
                _phaseTime += dt * ArrowCamTimeScale;
                if (_phaseTime >= _flight!.Duration)
                {
                    var (landedTip, heading) = _flight.At(_flight.Duration);
                    _stuckArrows.Add((landedTip, Sagged(heading)));
                    SetPhase(Phase.Impact);
                }
                break;

            case Phase.Impact:
                _phaseTime += dt;
                if (_phaseTime >= ImpactHoldSeconds)
                {
                    _flight = null;
                    SetPhase(Phase.Ready); // cut back to the archer with a fresh arrow
                }
                break;
        }
    }

    public void Draw()
    {
        var camera = CurrentCamera();

        var hills = _assets.Texture(AssetIds.Hills);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Sky), 0, 0, Color.White);
        Raylib.DrawTexture(hills, 0, Horizon + 2 - hills.Height, Color.White);
        Raylib.DrawTexture(_assets.Texture(AssetIds.Ground), 0, Horizon, Color.White);

        DrawTarget(camera);
        foreach (var (tip, direction) in _stuckArrows)
        {
            DrawArrow3D(camera, tip, direction, embedded: true);
        }

        switch (_phase)
        {
            case Phase.Ready:
                DrawLoweredBow(Vector2.Zero);
                DrawCrosshair();
                break;

            case Phase.Drawing:
                float lowered = EaseIn(_phaseTime / LowerSeconds);
                if (lowered < 1f)
                {
                    DrawLoweredBow(Vector2.Round(LoweredBowOffscreen * lowered));
                }
                float raised = EaseOut((_phaseTime - RaiseDelay) / RaiseSeconds);
                if (raised > 0f)
                {
                    DrawRaisedBow(Vector2.Round(AimBowOffscreen * (1f - raised)), DrawProgress());
                }
                DrawCrosshair();
                break;

            case Phase.Flying:
                float dropped = ArrowCam.Blend(_phaseTime);
                if (dropped < 1f)
                {
                    DrawRaisedBow(Vector2.Round(AimBowOffscreen * dropped), drawProgress: null);
                }
                var (flyingTip, flyingDirection) = _flight!.At(_phaseTime);
                DrawArrow3D(camera, flyingTip, flyingDirection);
                break;
        }
    }

    public void Exit() { }

    private void SetPhase(Phase phase)
    {
        _phase = phase;
        _phaseTime = 0f;
    }

    private PixelCamera CurrentCamera() => _phase switch
    {
        Phase.Flying => Eye with { Position = ArrowCam.Position(_flight!, _phaseTime, TargetWorld.Z - ArrowCamStopShort) },
        Phase.Impact => Eye with { Position = ArrowCam.Position(_flight!, _flight!.Duration, TargetWorld.Z - ArrowCamStopShort) },
        _ => Eye,
    };

    /// <summary>0 at brace height, 1 at full draw. Drawing starts once the bow is up; ease-out so it feels heavy at the end.</summary>
    private float DrawProgress() => EaseOut((_phaseTime - RaiseDelay - RaiseSeconds) / DrawSeconds);

    /// <summary>Nock position in the raised aim for draw progress <paramref name="progress"/>; the arrow points at the target.</summary>
    private static Vector3 AimNock(float progress) => Vector3.Lerp(NockAtBrace, NockAtFullDraw, progress);

    private static float EaseOut(float x)
    {
        float p = Math.Clamp(x, 0f, 1f);
        return 1f - (1f - p) * (1f - p);
    }

    private static float EaseIn(float x)
    {
        float p = Math.Clamp(x, 0f, 1f);
        return p * p;
    }

    /// <summary>Lowered pose: tilted bow at the side with the arrow nocked, string straight.</summary>
    private void DrawLoweredBow(Vector2 offset)
    {
        DrawAt(AssetIds.Bow, SpriteAnchors.Bow.Shelf, ArrowRest + offset);
        DrawAt(AssetIds.Arrow, SpriteAnchors.Arrow.Nock, NockedNock + offset);
        DrawString(BowOrigin + offset, NockedNock + offset);
    }

    /// <summary>
    /// Raised pose: upright bow in front of the face. With <paramref name="drawProgress"/> the arrow is on it, drawn
    /// back that far, and the string runs from the limbs to its nock; without (after release) the string is straight.
    /// </summary>
    private void DrawRaisedBow(Vector2 offset, float? drawProgress)
    {
        var origin = AimBowOrigin + offset;
        DrawAt(AssetIds.BowAim, SpriteAnchors.BowAim.Rest, AimPoint + offset);

        var top = origin + SpriteAnchors.BowAim.TopString;
        var bottom = origin + SpriteAnchors.BowAim.BottomString;
        if (drawProgress is not { } progress)
        {
            PixelLine(top, bottom, NearStringColor);
            return;
        }

        var nock = AimNock(progress);
        DrawArrow3D(Eye, nock + AimDirection * ArrowLength, AimDirection, offset);
        var nockPx = Eye.Project(nock) + offset;
        PixelLine(top, nockPx, NearStringColor);
        PixelLine(nockPx, bottom, NearStringColor);
    }

    private void DrawCrosshair()
    {
        var crosshair = _assets.Texture(AssetIds.Crosshair);
        DrawAt(AssetIds.Crosshair, new Vector2(crosshair.Width / 2, crosshair.Height / 2), Target);
    }

    /// <summary>Target stand and face, placed and scaled by distance from <paramref name="camera"/> (exactly 1:1 from the eye).</summary>
    private void DrawTarget(PixelCamera camera)
    {
        float scale = TargetDistance / camera.DepthOf(TargetWorld);
        var centre = camera.Project(TargetWorld);
        DrawScaled(AssetIds.TargetStand, SpriteAnchors.TargetStand.FaceCenter, centre, scale);
        DrawScaled(AssetIds.TargetFace, SpriteAnchors.TargetFace.Center, centre, scale);
    }

    /// <summary>
    /// World length of the arrow such that, seen from the eye in the lowered pose, it is as long as the nocked sprite:
    /// the sprite's tip sits a fraction s of the screen distance from its nock to the target, and along a 3D segment
    /// from depth z0 to z1 screen fraction s corresponds to segment fraction s·z0 / (z1·(1−s) + s·z0).
    /// </summary>
    private static float ArrowLengthMatchingLoweredSprite()
    {
        var nock = Eye.Unproject(NockedNock + PixelCentre, NockDepth);
        float s = StepsAlongArrow(SpriteAnchors.Arrow.Tip - SpriteAnchors.Arrow.Nock) / (float)StepsAlongArrow(Target - NockedNock);
        float z0 = nock.Z, z1 = TargetWorld.Z;
        float fraction = s * z0 / (z1 * (1f - s) + s * z0);
        return fraction * Vector3.Distance(nock, TargetWorld);
    }

    /// <summary>How many arrow steps <paramref name="offset"/> spans (it must lie on the lowered arrow's line).</summary>
    private static int StepsAlongArrow(Vector2 offset) => (int)(offset.X / SpriteAnchors.Arrow.Direction.X);

    /// <summary>
    /// Draws an arrow from its world-space tip and direction as seen by <paramref name="camera"/>, shifted on screen by
    /// <paramref name="screenOffset"/>. Seen side-on it is drawn like the sprites: a perspective-tapered wooden shaft
    /// with a highlight, red fletching near the nock, a steel point and a dark outline. Seen nearly end-on (projection
    /// only a few pixels long) it is the fletching from behind, sized by distance. An <paramref name="embedded"/> arrow
    /// (stuck in the target) has its point buried: the shaft ends in a dark entry hole, and it is never drawn thinner
    /// than 2 px so it keeps its outline against the target rings.
    /// </summary>
    private static void DrawArrow3D(PixelCamera camera, Vector3 tip, Vector3 direction, Vector2 screenOffset = default, bool embedded = false)
    {
        var nock = tip - direction * ArrowLength;
        float tipDepth = camera.DepthOf(tip), nockDepth = camera.DepthOf(nock);
        if (tipDepth <= PixelCamera.NearPlane)
        {
            return;
        }
        if (nockDepth < PixelCamera.NearPlane)
        {
            nock = Vector3.Lerp(nock, tip, (PixelCamera.NearPlane - nockDepth) / (tipDepth - nockDepth));
            nockDepth = PixelCamera.NearPlane;
        }

        var tipPx = Snap(camera.Project(tip) + screenOffset);
        var nockPx = Snap(camera.Project(nock) + screenOffset);

        if (Vector2.Distance(tipPx, nockPx) < 3f)
        {
            int arm = (int)Math.Clamp(MathF.Round(camera.Focal * FletchSpan / nockDepth), 1f, 3f);
            DrawFletchingEndOn((int)nockPx.X, (int)nockPx.Y, arm);
            return;
        }

        float length = Vector2.Distance(tipPx, nockPx);
        var along = (tipPx - nockPx) / length;
        var side = new Vector2(-along.Y, along.X);
        if (Vector2.Dot(side, LightFrom) < 0f)
        {
            side = -side; // highlight on the lit side
        }

        float minWidth = embedded ? 2f : 1f;
        float nockWidth = MathF.Max(ShaftPixels(camera, nockDepth), minWidth);
        float tipWidth = MathF.Max(ShaftPixels(camera, tipDepth), minWidth);
        var fletchFront = nockPx + along * length * 0.24f;
        float fletchBackWidth = nockWidth * 2.4f + 1f;
        float fletchFrontWidth = float.Lerp(nockWidth, tipWidth, 0.24f) * 1.2f;
        var pointBase = embedded ? tipPx : tipPx - along * MathF.Max(3f, length * 0.07f);
        float pointWidth = tipWidth * 1.8f + 1f;

        // Outline first (skipped when the arrow is only a pixel or two wide, where it would swallow it).
        if (nockWidth >= 2f)
        {
            FillTaper(nockPx - along, tipPx + along, nockWidth + 2f, tipWidth + 2f, OutlineColor);
            FillTaper(nockPx - along, fletchFront + along, fletchBackWidth + 2f, fletchFrontWidth + 2f, OutlineColor);
            if (!embedded)
            {
                FillTaper(pointBase - along, tipPx + along * 1.5f, pointWidth + 2f, 2f, OutlineColor);
            }
        }

        FillTaper(nockPx, fletchFront, fletchBackWidth, fletchFrontWidth, FletchColor);
        FillTaper(nockPx + side * fletchBackWidth * 0.25f, fletchFront + side * fletchFrontWidth * 0.25f,
            fletchBackWidth * 0.3f, fletchFrontWidth * 0.3f, FletchHighlightColor);
        FillTaper(nockPx, pointBase, nockWidth, tipWidth, ShaftColor);
        if (nockWidth >= 3f)
        {
            FillTaper(nockPx + side * nockWidth * 0.22f, pointBase + side * tipWidth * 0.22f,
                nockWidth * 0.35f, tipWidth * 0.35f, ShaftHighlightColor);
        }
        if (embedded)
        {
            // Entry hole: the point is inside the boss, the shaft disappears into a dark puncture.
            int x = (int)tipPx.X, y = (int)tipPx.Y;
            Raylib.DrawPixel(x, y, OutlineColor);
            Raylib.DrawPixel(x - 1, y, OutlineColor);
            Raylib.DrawPixel(x + 1, y, OutlineColor);
            Raylib.DrawPixel(x, y - 1, OutlineColor);
            Raylib.DrawPixel(x, y + 1, OutlineColor);
        }
        else
        {
            FillTaper(pointBase, tipPx, pointWidth, 1f, PointColor);
        }
        FillTaper(nockPx - along * 1.5f, nockPx + along * 0.5f, nockWidth, nockWidth, NockColor);
    }

    /// <summary>
    /// Fletching seen from behind: a pixel "Y" of red vanes <paramref name="arm"/> pixels long (cock vane up, lit) around
    /// the green nock at (<paramref name="x"/>, <paramref name="y"/>), with a dark outline so it reads on any ring.
    /// </summary>
    private static void DrawFletchingEndOn(int x, int y, int arm)
    {
        ReadOnlySpan<(int Dx, int Dy)> vanes = [(0, -1), (-1, 1), (1, 1)];
        foreach (var (dx, dy) in vanes)
        {
            for (int k = 0; k <= arm; k++)
            {
                int px = x + dx * k, py = y + dy * k;
                Raylib.DrawPixel(px - 1, py, OutlineColor);
                Raylib.DrawPixel(px + 1, py, OutlineColor);
                Raylib.DrawPixel(px, py - 1, OutlineColor);
                Raylib.DrawPixel(px, py + 1, OutlineColor);
            }
        }
        foreach (var (dx, dy) in vanes)
        {
            for (int k = 1; k <= arm; k++)
            {
                Raylib.DrawPixel(x + dx * k, y + dy * k, dy < 0 ? FletchHighlightColor : FletchColor);
            }
        }
        Raylib.DrawPixel(x, y, NockColor);
    }

    /// <summary>
    /// A landed arrow's shaft direction: its flight <paramref name="heading"/> tilted up by <see cref="StuckSagDegrees"/>,
    /// so the protruding nock end hangs lower than the point.
    /// </summary>
    private static Vector3 Sagged(Vector3 heading)
    {
        var (sin, cos) = MathF.SinCos(StuckSagDegrees * MathF.PI / 180f);
        return Vector3.Normalize(new Vector3(heading.X, heading.Y * cos + heading.Z * sin, heading.Z * cos - heading.Y * sin));
    }

    /// <summary>On-screen shaft width at <paramref name="depth"/> metres: true perspective size, capped near the eye.</summary>
    private static float ShaftPixels(PixelCamera camera, float depth) =>
        Math.Clamp(camera.Focal * ShaftThickness / depth, 1f, MaxShaftPixels);

    /// <summary>Filled quad from <paramref name="a"/> (width <paramref name="widthA"/>) to <paramref name="b"/> (width <paramref name="widthB"/>).</summary>
    private static void FillTaper(Vector2 a, Vector2 b, float widthA, float widthB, Color color)
    {
        var d = b - a;
        if (d.LengthSquared() < 1e-4f)
        {
            return;
        }
        var n = Vector2.Normalize(new Vector2(-d.Y, d.X));
        Vector2 a0 = a + n * widthA / 2f, a1 = a - n * widthA / 2f, b0 = b + n * widthB / 2f, b1 = b - n * widthB / 2f;
        FillTriangle(a0, a1, b1, color);
        FillTriangle(a0, b1, b0, color);
    }

    /// <summary>raylib only fills triangles of one winding; issuing both makes the fill independent of orientation.</summary>
    private static void FillTriangle(Vector2 p, Vector2 q, Vector2 r, Color color)
    {
        Raylib.DrawTriangle(p, q, r, color);
        Raylib.DrawTriangle(p, r, q, color);
    }

    /// <summary>Centre of the pixel containing <paramref name="p"/>, so lines rasterize on the pixel grid.</summary>
    private static Vector2 Snap(Vector2 p) => new(MathF.Floor(p.X) + 0.5f, MathF.Floor(p.Y) + 0.5f);

    /// <summary>Lowered bow's string: tips wrap the recurves, straight run through the nock, serving around the nock.</summary>
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

    /// <summary>
    /// Draws a sprite scaled by <paramref name="scale"/> so the centre of its pixel <paramref name="anchor"/> lands on
    /// <paramref name="screen"/>. At scale 1 with a pixel-centre <paramref name="screen"/> this matches <see cref="DrawAt"/>.
    /// </summary>
    private void DrawScaled(string id, Vector2 anchor, Vector2 screen, float scale)
    {
        var texture = _assets.Texture(id);
        var topLeft = screen - (anchor + PixelCentre) * scale;
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        var dest = new Rectangle(MathF.Floor(topLeft.X), MathF.Floor(topLeft.Y), texture.Width * scale, texture.Height * scale);
        Raylib.DrawTexturePro(texture, source, dest, Vector2.Zero, 0f, Color.White);
    }
}
