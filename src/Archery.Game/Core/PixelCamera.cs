using System.Numerics;

namespace Archery.Core;

/// <summary>
/// Pinhole camera for the first-person view. Eye at the origin looking along +Z, +X right, +Y up (metres).
/// <paramref name="Center"/> is the screen pixel straight ahead (on the horizon row of the backdrop);
/// <paramref name="Focal"/> is the focal length in screen pixels.
/// </summary>
public readonly record struct PixelCamera(Vector2 Center, float Focal)
{
    public Vector2 Project(Vector3 p) => new(Center.X + Focal * p.X / p.Z, Center.Y - Focal * p.Y / p.Z);

    /// <summary>The world point at <paramref name="depth"/> metres ahead that projects onto <paramref name="screen"/>.</summary>
    public Vector3 Unproject(Vector2 screen, float depth) =>
        new((screen.X - Center.X) * depth / Focal, (Center.Y - screen.Y) * depth / Focal, depth);
}
