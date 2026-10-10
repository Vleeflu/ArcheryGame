using System.Numerics;

namespace Archery.Core;

/// <summary>
/// Pinhole camera at the archer's eye (the origin), looking along +Z (+X right, +Y up, metres).
/// <paramref name="Center"/> is the screen pixel straight ahead (on the horizon row of the backdrop);
/// <paramref name="Focal"/> is the focal length in screen pixels.
/// </summary>
public readonly record struct PixelCamera(Vector2 Center, float Focal)
{
    /// <summary>Points closer than this in front of the camera are not drawn.</summary>
    public const float NearPlane = 0.05f;

    public Vector2 Project(Vector3 p) => new(Center.X + Focal * p.X / p.Z, Center.Y - Focal * p.Y / p.Z);

    /// <summary>The world point <paramref name="depth"/> metres in front of the camera that projects onto <paramref name="screen"/>.</summary>
    public Vector3 Unproject(Vector2 screen, float depth) =>
        new((screen.X - Center.X) * depth / Focal, (Center.Y - screen.Y) * depth / Focal, depth);

    /// <summary>Distance in front of the camera (along its view direction) of world point <paramref name="p"/>.</summary>
    public float DepthOf(Vector3 p) => p.Z;

    /// <summary>
    /// The same eye through a <paramref name="zoom"/>× longer lens, shifted so screen point <paramref name="fixedPoint"/>
    /// stays put: the whole view scales about it, nothing moves closer.
    /// </summary>
    public PixelCamera ZoomedAbout(Vector2 fixedPoint, float zoom) =>
        this with { Focal = Focal * zoom, Center = fixedPoint + (Center - fixedPoint) * zoom };

    /// <summary>Where screen point <paramref name="screen"/> of this camera lands in <paramref name="zoomed"/>, a zoom of it.</summary>
    public Vector2 ToZoomed(PixelCamera zoomed, Vector2 screen) => zoomed.Center + (screen - Center) * (zoomed.Focal / Focal);
}
